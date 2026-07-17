using System.Data;
using Domain.DTOs.Live;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Responses;
using FluentValidation;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.Options;
using Infrastructure.Services.Interfaces;
using Infrastructure.Services.Streaming;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Реализация бизнес-логики прямых эфиров. Видео идёт напрямую клиент↔провайдер (LiveKit), а бэкенд
/// раздаёт токены доступа (роль по бизнес-логике) и ведёт всё вокруг: эфиры, доступ (аудитория/приватность/
/// блокировки/баны), зрителей (денормализованные счётчики), гостей (лимит + очередь), комментарии/сердечки
/// (троттлинг), модерацию, статистику, вебхуки и автозавершение. Id текущего юзера — из claims.
/// </summary>
public class LiveStreamService : ILiveStreamService
{
    private readonly DataContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IStreamingProvider _provider;
    private readonly ILiveNotifier _notifier;
    private readonly INotificationService _notifications;
    private readonly ILiveRateLimiter _rateLimiter;
    private readonly ILiveWebhookValidator _webhookValidator;
    private readonly IValidator<StartLiveDto> _startValidator;
    private readonly IValidator<UpdateLiveTitleDto> _titleValidator;
    private readonly IValidator<AddLiveCommentDto> _commentValidator;
    private readonly StreamingOptions _options;
    private readonly ILogger<LiveStreamService> _logger;

    public LiveStreamService(
        DataContext context,
        ICurrentUserService currentUser,
        IStreamingProvider provider,
        ILiveNotifier notifier,
        INotificationService notifications,
        ILiveRateLimiter rateLimiter,
        ILiveWebhookValidator webhookValidator,
        IValidator<StartLiveDto> startValidator,
        IValidator<UpdateLiveTitleDto> titleValidator,
        IValidator<AddLiveCommentDto> commentValidator,
        IOptions<StreamingOptions> options,
        ILogger<LiveStreamService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _provider = provider;
        _notifier = notifier;
        _notifications = notifications;
        _rateLimiter = rateLimiter;
        _webhookValidator = webhookValidator;
        _startValidator = startValidator;
        _titleValidator = titleValidator;
        _commentValidator = commentValidator;
        _options = options.Value;
        _logger = logger;
    }

    // ── Управление эфиром (хост) ─────────────────────────────────────────────

    public async Task<Response<StartLiveResultDto>> StartAsync(StartLiveDto dto)
    {
        dto ??= new StartLiveDto();
        await _startValidator.ValidateAndThrowAsync(dto);

        var hostId = _currentUser.GetRequiredUserId();
        var hostName = _currentUser.UserName ?? string.Empty;

        // Запрет второго активного эфира у одного юзера.
        if (await _context.LiveStreams.AnyAsync(s => s.UserId == hostId && s.Status == LiveStreamStatus.Live))
            throw new BadRequestException("У вас уже идёт активный эфир.");

        var now = DateTime.UtcNow;
        var audience = dto.Audience ?? LiveStreamAudience.All;
        var roomName = $"live_{Guid.NewGuid():N}";

        await _provider.CreateRoomAsync(roomName);

        var stream = new LiveStream
        {
            UserId = hostId,
            Title = string.IsNullOrWhiteSpace(dto.Title) ? null : dto.Title.Trim(),
            RoomName = roomName,
            Status = LiveStreamStatus.Live,
            Audience = audience,
            StartedAt = now
        };
        _context.LiveStreams.Add(stream);
        await _context.SaveChangesAsync();

        // Токен хоста — с ролью Publisher. Генерирует только бэкенд.
        var token = await _provider.GenerateTokenAsync(roomName, hostId, hostName, ParticipantRole.Publisher);

        await NotifySubscribersOfStartAsync(stream, hostName);

        return new Response<StartLiveResultDto>(new StartLiveResultDto
        {
            StreamId = stream.Id,
            RoomName = roomName,
            Token = token,
            ServerUrl = ServerUrl()
        });
    }

    public async Task<Response<LiveStatsDto>> EndAsync(int? streamId)
    {
        var stream = await LoadTrackedStreamAsync(streamId);
        EnsureHost(stream);

        if (stream.Status == LiveStreamStatus.Ended)
            return new Response<LiveStatsDto>(await BuildStatsAsync(stream));

        var stats = await EndStreamInternalAsync(stream, notify: true);
        return new Response<LiveStatsDto>(stats);
    }

    public async Task<Response<string>> UpdateTitleAsync(int? streamId, UpdateLiveTitleDto dto)
    {
        dto ??= new UpdateLiveTitleDto();
        await _titleValidator.ValidateAndThrowAsync(dto);

        var stream = await LoadTrackedStreamAsync(streamId);
        EnsureHost(stream);

        stream.Title = string.IsNullOrWhiteSpace(dto.Title) ? null : dto.Title.Trim();
        await _context.SaveChangesAsync();

        return new Response<string>("Заголовок эфира обновлён.");
    }

    // ── Просмотр (зритель) ────────────────────────────────────────────────────

    public async Task<PagedResponse<List<LiveStreamDto>>> GetActiveAsync(int? pageNumber, int? pageSize)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var (page, size) = Pagination.Normalize(pageNumber, pageSize);

        // Хосты, на которых текущий подписан (одобренно) и с кем нет блокировки.
        var followingIds = await _context.FollowingRelationShips
            .Where(f => f.UserId == currentId && f.Status == FollowStatus.Accepted)
            .Select(f => f.FollowingUserId)
            .ToListAsync();

        var blocked = await AccessGuard.BlockRelatedUserIds(_context, currentId).ToListAsync();
        var hostIds = followingIds.Except(blocked).ToList();

        if (hostIds.Count == 0)
            return new PagedResponse<List<LiveStreamDto>>(new List<LiveStreamDto>(), 0, page, size);

        // Хосты, для которых текущий — близкий друг (нужно для эфиров с аудиторией CloseFriends).
        var closeFriendHosts = await _context.CloseFriends
            .Where(cf => cf.FriendUserId == currentId && hostIds.Contains(cf.UserId))
            .Select(cf => cf.UserId)
            .ToListAsync();

        var query = _context.LiveStreams.AsNoTracking()
            .Include(s => s.User)
            .Where(s => s.Status == LiveStreamStatus.Live
                        && hostIds.Contains(s.UserId)
                        && (s.Audience == LiveStreamAudience.All || closeFriendHosts.Contains(s.UserId)))
            .OrderByDescending(s => s.StartedAt);

        var total = await query.CountAsync();
        var streams = await query.Skip((page - 1) * size).Take(size).ToListAsync();

        var dtos = await MapStreamListAsync(streams);
        return new PagedResponse<List<LiveStreamDto>>(dtos, total, page, size);
    }

    public async Task<Response<LiveStreamDto>> GetByIdAsync(int? streamId)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var stream = await LoadStreamWithHostAsync(streamId);

        if (!await CanViewAsync(stream, currentId))
            throw new ForbiddenException("Эфир недоступен.");

        var currentViewers = await CountActiveViewersAsync(stream.Id);
        var guests = await LoadActiveGuestsAsync(stream.Id);

        var dto = MapStreamDto(stream, currentViewers, guests.Count, guests);
        return new Response<LiveStreamDto>(dto);
    }

    public async Task<Response<JoinLiveResultDto>> JoinAsync(int? streamId)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var userName = _currentUser.UserName ?? string.Empty;
        var stream = await LoadTrackedStreamAsync(streamId);

        if (stream.Status != LiveStreamStatus.Live)
            throw new BadRequestException("Эфир не активен.");

        // Хост своего эфира — не «зритель»: отдаём Publisher-токен без учёта в статистике зрителей.
        if (stream.UserId == currentId)
        {
            var hostToken = await _provider.GenerateTokenAsync(
                stream.RoomName, currentId, userName, ParticipantRole.Publisher);
            return new Response<JoinLiveResultDto>(BuildJoinResult(stream, hostToken));
        }

        if (await IsBannedAsync(stream.Id, currentId))
            throw new ForbiddenException("Вы забанены в этом эфире.");

        if (!await CanViewAsync(stream, currentId))
            throw new ForbiddenException("Эфир недоступен.");

        var now = DateTime.UtcNow;

        // Закрываем прежние незавершённые заходы этого зрителя (одна активная запись на юзера).
        await CloseActiveViewerRecordsAsync(stream.Id, currentId, now);

        _context.LiveViewers.Add(new LiveViewer
        {
            LiveStreamId = stream.Id,
            UserId = currentId,
            JoinedAt = now
        });
        stream.ViewersTotal += 1;
        await _context.SaveChangesAsync();

        var currentViewers = await CountActiveViewersAsync(stream.Id);
        if (currentViewers > stream.ViewersPeak)
        {
            stream.ViewersPeak = currentViewers;
            await _context.SaveChangesAsync();
        }

        var token = await _provider.GenerateTokenAsync(
            stream.RoomName, currentId, userName, ParticipantRole.Subscriber);

        await _notifier.ViewerJoinedAsync(stream.Id, await MapUserAsync(currentId));
        await _notifier.ViewerCountAsync(stream.Id, currentViewers);

        return new Response<JoinLiveResultDto>(BuildJoinResult(stream, token));
    }

    public async Task<Response<bool>> LeaveAsync(int? streamId)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var id = RequireId(streamId);

        await MarkViewerLeftAsync(id, currentId, notify: true);
        return new Response<bool>(true);
    }

    // ── Гости ──────────────────────────────────────────────────────────────────

    public async Task<Response<LiveGuestRequestDto>> RequestGuestAsync(int? streamId)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var stream = await LoadStreamWithHostAsync(streamId);

        if (stream.Status != LiveStreamStatus.Live)
            throw new BadRequestException("Эфир не активен.");
        if (stream.UserId == currentId)
            throw new BadRequestException("Вы хост этого эфира.");
        if (await IsBannedAsync(stream.Id, currentId))
            throw new ForbiddenException("Вы забанены в этом эфире.");
        if (!await CanViewAsync(stream, currentId))
            throw new ForbiddenException("Эфир недоступен.");

        var existing = await _context.LiveGuestRequests
            .Where(r => r.LiveStreamId == stream.Id && r.UserId == currentId)
            .ToListAsync();

        if (existing.Any(r => r.Status == LiveGuestRequestStatus.Approved))
            throw new BadRequestException("Вы уже гость этого эфира.");
        if (existing.Any(r => r.Status == LiveGuestRequestStatus.Pending))
            throw new BadRequestException("Ваша заявка уже на рассмотрении.");

        var now = DateTime.UtcNow;
        var request = new LiveGuestRequest
        {
            LiveStreamId = stream.Id,
            UserId = currentId,
            Status = LiveGuestRequestStatus.Pending,
            RequestedAt = now
        };
        _context.LiveGuestRequests.Add(request);
        await _context.SaveChangesAsync();

        var user = await MapUserAsync(currentId);
        var dto = new LiveGuestRequestDto
        {
            RequestId = request.Id,
            User = user,
            RequestedAt = now,
            Status = request.Status.ToString()
        };

        await _notifier.GuestRequestReceivedAsync(stream.UserId, dto);
        await _notifications.CreateAsync(
            stream.UserId, currentId, NotificationType.LiveGuestRequest,
            NotificationEntityType.LiveStream, stream.Id);

        return new Response<LiveGuestRequestDto>(dto);
    }

    public async Task<Response<bool>> CancelGuestRequestAsync(int? streamId)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var id = RequireId(streamId);

        var pending = await _context.LiveGuestRequests
            .Where(r => r.LiveStreamId == id && r.UserId == currentId
                        && r.Status == LiveGuestRequestStatus.Pending)
            .ToListAsync();

        if (pending.Count == 0)
            return new Response<bool>(true); // идемпотентно

        var now = DateTime.UtcNow;
        foreach (var r in pending)
        {
            r.Status = LiveGuestRequestStatus.Cancelled;
            r.RespondedAt = now;
        }
        await _context.SaveChangesAsync();

        return new Response<bool>(true);
    }

    public async Task<Response<List<LiveGuestRequestDto>>> GetGuestRequestsAsync(int? streamId)
    {
        var stream = await LoadStreamWithHostAsync(streamId);
        EnsureHost(stream);

        var requests = await _context.LiveGuestRequests.AsNoTracking()
            .Where(r => r.LiveStreamId == stream.Id && r.Status == LiveGuestRequestStatus.Pending)
            .OrderBy(r => r.RequestedAt)
            .Select(r => new LiveGuestRequestDto
            {
                RequestId = r.Id,
                RequestedAt = r.RequestedAt,
                Status = r.Status.ToString(),
                User = new LiveUserDto
                {
                    UserId = r.User!.Id,
                    UserName = r.User.UserName!,
                    FullName = r.User.FullName,
                    Avatar = r.User.Avatar,
                    IsVerified = r.User.IsVerified
                }
            })
            .ToListAsync();

        return new Response<List<LiveGuestRequestDto>>(requests);
    }

    public async Task<Response<bool>> ApproveGuestAsync(int? requestId)
    {
        var id = RequireId(requestId);

        var request = await _context.LiveGuestRequests
            .Include(r => r.LiveStream)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException("Заявка не найдена.");

        var stream = request.LiveStream!;
        EnsureHost(stream);

        if (stream.Status != LiveStreamStatus.Live)
            throw new BadRequestException("Эфир не активен.");
        if (request.Status != LiveGuestRequestStatus.Pending)
            throw new BadRequestException("Заявка уже обработана.");

        // Гонка одновременного одобрения при лимите: считаем и проставляем в сериализуемой транзакции.
        var max = Math.Max(0, _options.MaxGuests);
        var now = DateTime.UtcNow;
        await using (var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            var approved = await _context.LiveGuestRequests
                .CountAsync(r => r.LiveStreamId == stream.Id && r.Status == LiveGuestRequestStatus.Approved);
            if (approved >= max)
                throw new BadRequestException($"Достигнут лимит гостей ({max}). Заявка осталась в очереди.");

            request.Status = LiveGuestRequestStatus.Approved;
            request.RespondedAt = now;
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }

        // Повышаем роль у провайдера и оповещаем — после коммита.
        await _provider.UpdateParticipantRoleAsync(stream.RoomName, request.UserId, ParticipantRole.Publisher);
        await _notifier.GuestJoinedAsync(stream.Id, await MapUserAsync(request.UserId));

        return new Response<bool>(true);
    }

    public async Task<Response<bool>> DeclineGuestAsync(int? requestId)
    {
        var id = RequireId(requestId);

        var request = await _context.LiveGuestRequests
            .Include(r => r.LiveStream)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException("Заявка не найдена.");

        EnsureHost(request.LiveStream!);

        if (request.Status != LiveGuestRequestStatus.Pending)
            throw new BadRequestException("Заявка уже обработана.");

        request.Status = LiveGuestRequestStatus.Declined;
        request.RespondedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _notifier.GuestRequestDeclinedAsync(request.UserId, request.Id);
        return new Response<bool>(true);
    }

    public async Task<Response<bool>> RemoveGuestAsync(int? streamId, string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new BadRequestException("Не указан пользователь.");

        var stream = await LoadTrackedStreamAsync(streamId);
        EnsureHost(stream);

        var approved = await _context.LiveGuestRequests
            .Where(r => r.LiveStreamId == stream.Id && r.UserId == userId
                        && r.Status == LiveGuestRequestStatus.Approved)
            .ToListAsync();

        if (approved.Count == 0)
            throw new NotFoundException("Активный гость не найден.");

        var now = DateTime.UtcNow;
        foreach (var r in approved)
        {
            r.Status = LiveGuestRequestStatus.Removed;
            r.RespondedAt = now;
        }
        await _context.SaveChangesAsync();

        await _provider.UpdateParticipantRoleAsync(stream.RoomName, userId, ParticipantRole.Subscriber);
        await _notifier.GuestLeftAsync(stream.Id, userId);

        return new Response<bool>(true);
    }

    public async Task<Response<List<LiveUserDto>>> GetActiveGuestsAsync(int? streamId)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var stream = await LoadStreamWithHostAsync(streamId);

        if (!await CanViewAsync(stream, currentId))
            throw new ForbiddenException("Эфир недоступен.");

        var guests = await LoadActiveGuestsAsync(stream.Id);
        return new Response<List<LiveUserDto>>(guests);
    }

    // ── Комментарии и реакции ───────────────────────────────────────────────────

    public async Task<Response<LiveCommentDto>> AddCommentAsync(int? streamId, AddLiveCommentDto dto)
    {
        dto ??= new AddLiveCommentDto();
        await _commentValidator.ValidateAndThrowAsync(dto);

        var currentId = _currentUser.GetRequiredUserId();
        var stream = await LoadTrackedStreamAsync(streamId);

        if (stream.Status != LiveStreamStatus.Live)
            throw new BadRequestException("Эфир не активен.");
        if (stream.UserId != currentId)
        {
            if (await IsBannedAsync(stream.Id, currentId))
                throw new ForbiddenException("Вы забанены в этом эфире.");
            if (!await CanViewAsync(stream, currentId))
                throw new ForbiddenException("Эфир недоступен.");
        }

        var text = dto.Text.Trim();
        if (text.Length > _options.MaxCommentLength)
            throw new BadRequestException($"Слишком длинный комментарий (максимум {_options.MaxCommentLength}).");

        if (!_rateLimiter.AllowComment(currentId, stream.Id))
            throw new BadRequestException("Вы комментируете слишком часто. Подождите немного.");

        var now = DateTime.UtcNow;
        var comment = new LiveComment
        {
            LiveStreamId = stream.Id,
            UserId = currentId,
            Text = text,
            CreatedAt = now
        };
        _context.LiveComments.Add(comment);
        stream.CommentsCount += 1;
        await _context.SaveChangesAsync();

        var commentDto = new LiveCommentDto
        {
            Id = comment.Id,
            User = await MapUserAsync(currentId),
            Text = text,
            CreatedAt = now,
            IsPinned = false
        };

        await _notifier.NewCommentAsync(stream.Id, commentDto);
        return new Response<LiveCommentDto>(commentDto);
    }

    public async Task<Response<bool>> DeleteCommentAsync(int? commentId)
    {
        var id = RequireId(commentId);
        var currentId = _currentUser.GetRequiredUserId();

        var comment = await _context.LiveComments
            .Include(c => c.LiveStream)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException("Комментарий не найден.");

        // Удалить может автор коммента или хост эфира.
        if (comment.UserId != currentId && comment.LiveStream!.UserId != currentId)
            throw new ForbiddenException("Нет доступа к этому комментарию.");

        if (!comment.IsDeleted)
        {
            comment.IsDeleted = true;
            if (comment.LiveStream!.CommentsCount > 0)
                comment.LiveStream.CommentsCount -= 1;
            await _context.SaveChangesAsync();
            await _notifier.CommentDeletedAsync(comment.LiveStreamId, comment.Id);
        }

        return new Response<bool>(true);
    }

    public async Task<Response<bool>> PinCommentAsync(int? commentId)
    {
        var id = RequireId(commentId);

        var comment = await _context.LiveComments
            .Include(c => c.LiveStream)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException("Комментарий не найден.");

        EnsureHost(comment.LiveStream!);

        if (comment.IsDeleted)
            throw new BadRequestException("Нельзя закрепить удалённый комментарий.");

        // Одновременно закреплён только один — снимаем закрепление с остальных.
        var pinned = await _context.LiveComments
            .Where(c => c.LiveStreamId == comment.LiveStreamId && c.IsPinned && c.Id != comment.Id)
            .ToListAsync();
        foreach (var p in pinned)
            p.IsPinned = false;

        comment.IsPinned = true;
        await _context.SaveChangesAsync();

        await _notifier.CommentPinnedAsync(comment.LiveStreamId, comment.Id);
        return new Response<bool>(true);
    }

    public async Task<PagedResponse<List<LiveCommentDto>>> GetCommentsAsync(
        int? streamId, int? pageNumber, int? pageSize)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var stream = await LoadStreamWithHostAsync(streamId);

        if (!await CanViewAsync(stream, currentId))
            throw new ForbiddenException("Эфир недоступен.");

        var (page, size) = Pagination.Normalize(pageNumber, pageSize);

        var query = _context.LiveComments.AsNoTracking()
            .Where(c => c.LiveStreamId == stream.Id && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(c => new LiveCommentDto
            {
                Id = c.Id,
                Text = c.Text,
                CreatedAt = c.CreatedAt,
                IsPinned = c.IsPinned,
                User = new LiveUserDto
                {
                    UserId = c.User!.Id,
                    UserName = c.User.UserName!,
                    FullName = c.User.FullName,
                    Avatar = c.User.Avatar,
                    IsVerified = c.User.IsVerified
                }
            })
            .ToListAsync();

        return new PagedResponse<List<LiveCommentDto>>(items, total, page, size);
    }

    public async Task<Response<bool>> SendLikeAsync(int? streamId)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var stream = await LoadTrackedStreamAsync(streamId);

        if (stream.Status != LiveStreamStatus.Live)
            throw new BadRequestException("Эфир не активен.");
        if (stream.UserId != currentId)
        {
            if (await IsBannedAsync(stream.Id, currentId))
                throw new ForbiddenException("Вы забанены в этом эфире.");
            if (!await CanViewAsync(stream, currentId))
                throw new ForbiddenException("Эфир недоступен.");
        }

        // Троттлинг: сердечки шлют пачками — молча игнорируем сверх лимита (не ошибка), data=false.
        if (!_rateLimiter.AllowLike(currentId, stream.Id))
            return new Response<bool>(false);

        _context.LiveLikes.Add(new LiveLike
        {
            LiveStreamId = stream.Id,
            UserId = currentId,
            CreatedAt = DateTime.UtcNow
        });
        stream.LikesCount += 1;
        await _context.SaveChangesAsync();

        await _notifier.NewLikeAsync(stream.Id, currentId);
        return new Response<bool>(true);
    }

    // ── Модерация ────────────────────────────────────────────────────────────────

    public async Task<Response<bool>> BanViewerAsync(int? streamId, string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new BadRequestException("Не указан пользователь.");

        var stream = await LoadTrackedStreamAsync(streamId);
        EnsureHost(stream);

        if (userId == stream.UserId)
            throw new BadRequestException("Нельзя забанить самого себя.");

        var now = DateTime.UtcNow;

        var alreadyBanned = await _context.LiveBans
            .AnyAsync(b => b.LiveStreamId == stream.Id && b.UserId == userId);
        if (!alreadyBanned)
        {
            _context.LiveBans.Add(new LiveBan
            {
                LiveStreamId = stream.Id,
                UserId = userId,
                BannedAt = now
            });
        }

        // Понижаем возможного гостя и закрываем его активные заходы.
        var guest = await _context.LiveGuestRequests
            .Where(r => r.LiveStreamId == stream.Id && r.UserId == userId
                        && r.Status == LiveGuestRequestStatus.Approved)
            .ToListAsync();
        foreach (var g in guest)
        {
            g.Status = LiveGuestRequestStatus.Removed;
            g.RespondedAt = now;
        }

        await CloseActiveViewerRecordsAsync(stream.Id, userId, now);
        await _context.SaveChangesAsync();

        await _provider.RemoveParticipantAsync(stream.RoomName, userId);

        await _notifier.ViewerBannedAsync(stream.Id, userId);
        await _notifier.ViewerCountAsync(stream.Id, await CountActiveViewersAsync(stream.Id));

        return new Response<bool>(true);
    }

    public async Task<Response<bool>> UnbanViewerAsync(int? streamId, string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new BadRequestException("Не указан пользователь.");

        var stream = await LoadTrackedStreamAsync(streamId);
        EnsureHost(stream);

        var ban = await _context.LiveBans
            .FirstOrDefaultAsync(b => b.LiveStreamId == stream.Id && b.UserId == userId);
        if (ban is not null)
        {
            _context.LiveBans.Remove(ban);
            await _context.SaveChangesAsync();
        }

        return new Response<bool>(true);
    }

    public async Task<PagedResponse<List<LiveViewerDto>>> GetViewersAsync(
        int? streamId, int? pageNumber, int? pageSize)
    {
        var stream = await LoadStreamWithHostAsync(streamId);
        EnsureHost(stream);

        var (page, size) = Pagination.Normalize(pageNumber, pageSize);

        // Текущие зрители: активные заходы (без LeftAt), по одной строке на пользователя (последний заход).
        var activeQuery = _context.LiveViewers.AsNoTracking()
            .Where(v => v.LiveStreamId == stream.Id && v.LeftAt == null);

        var grouped = activeQuery
            .GroupBy(v => v.UserId)
            .Select(g => new { UserId = g.Key, JoinedAt = g.Max(x => x.JoinedAt) });

        var total = await grouped.CountAsync();

        var pageRows = await grouped
            .OrderByDescending(x => x.JoinedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        var userIds = pageRows.Select(r => r.UserId).ToList();
        var users = await _context.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var items = pageRows
            .Where(r => users.ContainsKey(r.UserId))
            .Select(r => new LiveViewerDto
            {
                User = MapUser(users[r.UserId]),
                JoinedAt = r.JoinedAt
            })
            .ToList();

        return new PagedResponse<List<LiveViewerDto>>(items, total, page, size);
    }

    // ── Статистика / после эфира ───────────────────────────────────────────────────

    public async Task<Response<LiveStatsDto>> GetStatsAsync(int? streamId)
    {
        var stream = await LoadStreamWithHostAsync(streamId);
        EnsureHost(stream);

        return new Response<LiveStatsDto>(await BuildStatsAsync(stream));
    }

    public async Task<PagedResponse<List<LiveStreamDto>>> GetMyStreamsAsync(int? pageNumber, int? pageSize)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var (page, size) = Pagination.Normalize(pageNumber, pageSize);

        var query = _context.LiveStreams.AsNoTracking()
            .Include(s => s.User)
            .Where(s => s.UserId == currentId)
            .OrderByDescending(s => s.StartedAt);

        var total = await query.CountAsync();
        var streams = await query.Skip((page - 1) * size).Take(size).ToListAsync();

        var dtos = await MapStreamListAsync(streams);
        return new PagedResponse<List<LiveStreamDto>>(dtos, total, page, size);
    }

    public async Task<Response<string>> SaveToStoryAsync(int? streamId)
    {
        var stream = await LoadTrackedStreamAsync(streamId);
        EnsureHost(stream);

        if (stream.Status != LiveStreamStatus.Ended)
            throw new BadRequestException("Сохранить в сторис можно только завершённый эфир.");
        if (stream.SavedToStory)
            return new Response<string>("Эфир уже сохранён в сторис.");
        if (string.IsNullOrWhiteSpace(stream.RecordingUrl))
            throw new BadRequestException("Нет записи эфира для сохранения в сторис.");

        // Запись эфира — внешний URL (egress провайдера), поэтому в FileName сторис кладём ссылку на неё
        // (для локальных файлов там имя в wwwroot). Аудитория сторис наследуется от аудитории эфира.
        _context.Stories.Add(new Story
        {
            UserId = stream.UserId,
            FileName = stream.RecordingUrl,
            Audience = stream.Audience == LiveStreamAudience.CloseFriends
                ? StoryAudience.CloseFriends
                : StoryAudience.All,
            CreatedAt = DateTime.UtcNow
        });
        stream.SavedToStory = true;
        await _context.SaveChangesAsync();

        return new Response<string>("Эфир сохранён в сторис.");
    }

    // ── Вебхуки провайдера / real-time инфраструктура ────────────────────────────────

    public async Task<Response<string>> HandleWebhookAsync(string rawBody, string? authHeader)
    {
        var evt = _webhookValidator.Validate(rawBody ?? string.Empty, authHeader);
        if (evt is null)
            return new Response<string>(401, "Недействительная подпись вебхука.");

        if (string.IsNullOrWhiteSpace(evt.Room))
            return new Response<string>("ignored");

        var stream = await _context.LiveStreams
            .FirstOrDefaultAsync(s => s.RoomName == evt.Room);
        if (stream is null)
            return new Response<string>("ignored");

        switch (evt.Event)
        {
            case "room_finished":
                // Хост потерял связь навсегда / комната закрылась → завершаем эфир (идемпотентно).
                if (stream.Status == LiveStreamStatus.Live)
                    await EndStreamInternalAsync(stream, notify: true);
                break;

            case "participant_left":
                // Уход зрителя/гостя из комнаты (не хоста) → фиксируем выход (идемпотентно).
                if (stream.Status == LiveStreamStatus.Live
                    && !string.IsNullOrWhiteSpace(evt.ParticipantIdentity)
                    && evt.ParticipantIdentity != stream.UserId)
                {
                    await MarkViewerLeftAsync(stream.Id, evt.ParticipantIdentity!, notify: true);
                }
                break;

            // participant_joined / track_published — учёт ведём через REST; здесь no-op.
            default:
                _logger.LogDebug("Вебхук {Event} для комнаты {Room} — без действия.", evt.Event, evt.Room);
                break;
        }

        return new Response<string>("ok");
    }

    public async Task HandleViewerDisconnectAsync(int streamId, string userId)
    {
        try
        {
            await MarkViewerLeftAsync(streamId, userId, notify: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось обработать отключение зрителя {User} эфира {Stream}.", userId, streamId);
        }
    }

    public async Task AutoEndInactiveStreamsAsync(TimeSpan inactivity, TimeSpan maxDuration)
    {
        var now = DateTime.UtcNow;

        var liveStreams = await _context.LiveStreams
            .Where(s => s.Status == LiveStreamStatus.Live)
            .ToListAsync();

        foreach (var stream in liveStreams)
        {
            // Последняя активность = максимум из старта, последнего коммента/лайка/захода зрителя.
            var lastComment = await _context.LiveComments
                .Where(c => c.LiveStreamId == stream.Id)
                .MaxAsync(c => (DateTime?)c.CreatedAt);
            var lastLike = await _context.LiveLikes
                .Where(l => l.LiveStreamId == stream.Id)
                .MaxAsync(l => (DateTime?)l.CreatedAt);
            var lastJoin = await _context.LiveViewers
                .Where(v => v.LiveStreamId == stream.Id)
                .MaxAsync(v => (DateTime?)v.JoinedAt);

            var lastActivity = new[] { stream.StartedAt, lastComment ?? stream.StartedAt,
                    lastLike ?? stream.StartedAt, lastJoin ?? stream.StartedAt }
                .Max();

            var stale = now - lastActivity > inactivity;
            var tooLong = now - stream.StartedAt > maxDuration;

            if (stale || tooLong)
            {
                _logger.LogInformation(
                    "Автозавершение висящего эфира {Stream} (stale={Stale}, tooLong={TooLong}).",
                    stream.Id, stale, tooLong);
                await EndStreamInternalAsync(stream, notify: true);
            }
        }
    }

    // ── Внутренние помощники ─────────────────────────────────────────────────────────

    private async Task<LiveStatsDto> EndStreamInternalAsync(LiveStream stream, bool notify)
    {
        var now = DateTime.UtcNow;
        stream.Status = LiveStreamStatus.Ended;
        stream.EndedAt = now;

        // Фиксируем длительность просмотра всем активным зрителям.
        var active = await _context.LiveViewers
            .Where(v => v.LiveStreamId == stream.Id && v.LeftAt == null)
            .ToListAsync();
        foreach (var v in active)
        {
            v.LeftAt = now;
            v.WatchDurationSeconds = Math.Max(0, (int)(now - v.JoinedAt).TotalSeconds);
        }
        await _context.SaveChangesAsync();

        await _provider.CloseRoomAsync(stream.RoomName);

        var stats = await BuildStatsAsync(stream);
        if (notify)
            await _notifier.StreamEndedAsync(stream.Id,
                new LiveStreamEndedDto { StreamId = stream.Id, Stats = stats });

        return stats;
    }

    private async Task MarkViewerLeftAsync(int streamId, string userId, bool notify)
    {
        var now = DateTime.UtcNow;
        var active = await _context.LiveViewers
            .Where(v => v.LiveStreamId == streamId && v.UserId == userId && v.LeftAt == null)
            .ToListAsync();

        if (active.Count == 0)
            return; // идемпотентно

        foreach (var v in active)
        {
            v.LeftAt = now;
            v.WatchDurationSeconds = Math.Max(0, (int)(now - v.JoinedAt).TotalSeconds);
        }
        await _context.SaveChangesAsync();

        if (notify)
        {
            await _notifier.ViewerLeftAsync(streamId, userId);
            await _notifier.ViewerCountAsync(streamId, await CountActiveViewersAsync(streamId));
        }
    }

    private async Task CloseActiveViewerRecordsAsync(int streamId, string userId, DateTime now)
    {
        var active = await _context.LiveViewers
            .Where(v => v.LiveStreamId == streamId && v.UserId == userId && v.LeftAt == null)
            .ToListAsync();
        foreach (var v in active)
        {
            v.LeftAt = now;
            v.WatchDurationSeconds = Math.Max(0, (int)(now - v.JoinedAt).TotalSeconds);
        }
    }

    private async Task NotifySubscribersOfStartAsync(LiveStream stream, string hostName)
    {
        var hostId = stream.UserId;

        var followerIds = await _context.FollowingRelationShips
            .Where(f => f.FollowingUserId == hostId && f.Status == FollowStatus.Accepted)
            .Select(f => f.UserId)
            .ToListAsync();

        if (followerIds.Count == 0)
            return;

        var blocked = await AccessGuard.BlockRelatedUserIds(_context, hostId).ToListAsync();
        var eligible = followerIds.Except(blocked).ToList();

        // Аудитория CloseFriends → только близкие друзья хоста среди подписчиков.
        if (stream.Audience == LiveStreamAudience.CloseFriends)
        {
            var closeFriends = await _context.CloseFriends
                .Where(cf => cf.UserId == hostId)
                .Select(cf => cf.FriendUserId)
                .ToListAsync();
            eligible = eligible.Intersect(closeFriends).ToList();
        }

        if (eligible.Count == 0)
            return;

        await _notifier.StreamStartedAsync(eligible, new LiveStreamStartedDto
        {
            StreamId = stream.Id,
            HostUserId = hostId,
            HostUserName = hostName,
            Title = stream.Title
        });

        foreach (var recipientId in eligible)
        {
            await _notifications.CreateAsync(
                recipientId, hostId, NotificationType.LiveStarted,
                NotificationEntityType.LiveStream, stream.Id);
        }
    }

    private async Task<LiveStatsDto> BuildStatsAsync(LiveStream stream)
    {
        var now = DateTime.UtcNow;
        var id = stream.Id;
        var isLive = stream.Status == LiveStreamStatus.Live;

        var uniqueViewers = await _context.LiveViewers
            .Where(v => v.LiveStreamId == id)
            .Select(v => v.UserId)
            .Distinct()
            .CountAsync();

        var currentViewers = isLive ? await CountActiveViewersAsync(id) : 0;
        var activeGuests = await _context.LiveGuestRequests
            .CountAsync(r => r.LiveStreamId == id && r.Status == LiveGuestRequestStatus.Approved);
        var guestRequests = await _context.LiveGuestRequests.CountAsync(r => r.LiveStreamId == id);

        var totalWatch = await _context.LiveViewers
            .Where(v => v.LiveStreamId == id)
            .SumAsync(v => v.WatchDurationSeconds);
        var avgWatch = uniqueViewers > 0 ? totalWatch / uniqueViewers : 0;

        var durationEnd = stream.EndedAt ?? now;
        var duration = Math.Max(0, (int)(durationEnd - stream.StartedAt).TotalSeconds);

        // Топ-комментаторы (по неудалённым комментам).
        var topRows = await _context.LiveComments
            .Where(c => c.LiveStreamId == id && !c.IsDeleted)
            .GroupBy(c => c.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();

        var topUserIds = topRows.Select(r => r.UserId).ToList();
        var topUsers = await _context.Users.AsNoTracking()
            .Where(u => topUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var topCommenters = topRows
            .Where(r => topUsers.ContainsKey(r.UserId))
            .Select(r => new LiveTopCommenterDto
            {
                User = MapUser(topUsers[r.UserId]),
                CommentsCount = r.Count
            })
            .ToList();

        return new LiveStatsDto
        {
            StreamId = id,
            Status = stream.Status.ToString(),
            CurrentViewers = currentViewers,
            ViewersPeak = stream.ViewersPeak,
            UniqueViewers = uniqueViewers,
            CommentsCount = stream.CommentsCount,
            LikesCount = stream.LikesCount,
            ActiveGuests = activeGuests,
            GuestRequestsCount = guestRequests,
            DurationSeconds = duration,
            AverageWatchSeconds = avgWatch,
            TotalWatchSeconds = totalWatch,
            TopCommenters = topCommenters
        };
    }

    private async Task<List<LiveStreamDto>> MapStreamListAsync(List<LiveStream> streams)
    {
        if (streams.Count == 0)
            return new List<LiveStreamDto>();

        var ids = streams.Select(s => s.Id).ToList();

        var activeCounts = await _context.LiveViewers.AsNoTracking()
            .Where(v => ids.Contains(v.LiveStreamId) && v.LeftAt == null)
            .GroupBy(v => v.LiveStreamId)
            .Select(g => new { g.Key, Count = g.Select(x => x.UserId).Distinct().Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var guestCounts = await _context.LiveGuestRequests.AsNoTracking()
            .Where(r => ids.Contains(r.LiveStreamId) && r.Status == LiveGuestRequestStatus.Approved)
            .GroupBy(r => r.LiveStreamId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        return streams.Select(s => MapStreamDto(
            s,
            activeCounts.TryGetValue(s.Id, out var vc) ? vc : 0,
            guestCounts.TryGetValue(s.Id, out var gc) ? gc : 0)).ToList();
    }

    private static LiveStreamDto MapStreamDto(
        LiveStream s, int currentViewers, int guestsCount, List<LiveUserDto>? guests = null) => new()
    {
        StreamId = s.Id,
        RoomName = s.RoomName,
        Title = s.Title,
        Status = s.Status.ToString(),
        Audience = s.Audience.ToString(),
        Host = MapUser(s.User!),
        StartedAt = s.StartedAt,
        EndedAt = s.EndedAt,
        CurrentViewers = currentViewers,
        ViewersPeak = s.ViewersPeak,
        ViewersTotal = s.ViewersTotal,
        CommentsCount = s.CommentsCount,
        LikesCount = s.LikesCount,
        GuestsCount = guestsCount,
        SavedToStory = s.SavedToStory,
        RecordingUrl = s.RecordingUrl,
        Guests = guests
    };

    private async Task<List<LiveUserDto>> LoadActiveGuestsAsync(int streamId) =>
        await _context.LiveGuestRequests.AsNoTracking()
            .Where(r => r.LiveStreamId == streamId && r.Status == LiveGuestRequestStatus.Approved)
            .OrderBy(r => r.RespondedAt)
            .Select(r => new LiveUserDto
            {
                UserId = r.User!.Id,
                UserName = r.User.UserName!,
                FullName = r.User.FullName,
                Avatar = r.User.Avatar,
                IsVerified = r.User.IsVerified
            })
            .ToListAsync();

    private Task<int> CountActiveViewersAsync(int streamId) =>
        _context.LiveViewers
            .Where(v => v.LiveStreamId == streamId && v.LeftAt == null)
            .Select(v => v.UserId)
            .Distinct()
            .CountAsync();

    private Task<bool> IsBannedAsync(int streamId, string userId) =>
        _context.LiveBans.AnyAsync(b => b.LiveStreamId == streamId && b.UserId == userId);

    /// <summary>Доступ к эфиру: свой; иначе блок/приватность (AccessGuard) + аудитория (close friends).</summary>
    private async Task<bool> CanViewAsync(LiveStream stream, string currentId)
    {
        if (stream.UserId == currentId)
            return true;

        if (!await AccessGuard.CanViewContentAsync(_context, stream.UserId, currentId))
            return false;

        if (stream.Audience == LiveStreamAudience.CloseFriends)
            return await _context.CloseFriends
                .AnyAsync(cf => cf.UserId == stream.UserId && cf.FriendUserId == currentId);

        return true;
    }

    private async Task<LiveUserDto> MapUserAsync(string userId)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        return user is null
            ? new LiveUserDto { UserId = userId }
            : MapUser(user);
    }

    private static LiveUserDto MapUser(User u) => new()
    {
        UserId = u.Id,
        UserName = u.UserName ?? string.Empty,
        FullName = u.FullName,
        Avatar = u.Avatar,
        IsVerified = u.IsVerified
    };

    private JoinLiveResultDto BuildJoinResult(LiveStream stream, string token) => new()
    {
        StreamId = stream.Id,
        RoomName = stream.RoomName,
        Token = token,
        ServerUrl = ServerUrl()
    };

    private string ServerUrl() =>
        string.IsNullOrWhiteSpace(_options.LiveKit.Url) ? "wss://fake.local" : _options.LiveKit.Url;

    private void EnsureHost(LiveStream stream)
    {
        var currentId = _currentUser.GetRequiredUserId();
        if (stream.UserId != currentId)
            throw new ForbiddenException("Действие доступно только хосту эфира.");
    }

    private async Task<LiveStream> LoadTrackedStreamAsync(int? streamId)
    {
        var id = RequireId(streamId);
        return await _context.LiveStreams.FirstOrDefaultAsync(s => s.Id == id)
               ?? throw new NotFoundException("Эфир не найден.");
    }

    private async Task<LiveStream> LoadStreamWithHostAsync(int? streamId)
    {
        var id = RequireId(streamId);
        return await _context.LiveStreams.AsNoTracking()
                   .Include(s => s.User)
                   .FirstOrDefaultAsync(s => s.Id == id)
               ?? throw new NotFoundException("Эфир не найден.");
    }

    private static int RequireId(int? id)
    {
        if (id is null or <= 0)
            throw new BadRequestException("Некорректный идентификатор.");
        return id.Value;
    }
}
