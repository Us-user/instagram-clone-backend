using Domain.DTOs.GroupChat;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Responses;
using FluentValidation;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Групповые чаты (§7) — отдельная ветка от личных 1:1. Создание группы (создатель — Admin),
/// список групп с непрочитанными, открытие (пометка прочитанным), управление составом/инфо по
/// ролям, отправка сообщений с рассылкой через SignalR и удаление. Изменения состава/инфо
/// фиксируются служебными (System) сообщениями. Id текущего юзера — из claims.
/// </summary>
public class GroupChatService : IGroupChatService
{
    private static readonly string[] ImageExtensions =
        { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };

    /// <summary>Разрешённые расширения вложений группы (изображения + распространённые файлы).</summary>
    private static readonly string[] AttachmentExtensions =
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv",
        ".zip", ".rar", ".7z",
        ".mp3", ".wav", ".m4a", ".ogg", ".mp4", ".mov", ".webm"
    };

    private const long AttachmentMaxSize = 25 * 1024 * 1024; // 25 МБ

    private readonly DataContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileService _fileService;
    private readonly IGroupChatNotifier _notifier;
    private readonly IValidator<CreateGroupChatDto> _createValidator;
    private readonly IValidator<SendGroupMessageDto> _sendValidator;
    private readonly IValidator<UpdateGroupInfoDto> _updateValidator;

    public GroupChatService(
        DataContext context,
        ICurrentUserService currentUser,
        IFileService fileService,
        IGroupChatNotifier notifier,
        IValidator<CreateGroupChatDto> createValidator,
        IValidator<SendGroupMessageDto> sendValidator,
        IValidator<UpdateGroupInfoDto> updateValidator)
    {
        _context = context;
        _currentUser = currentUser;
        _fileService = fileService;
        _notifier = notifier;
        _createValidator = createValidator;
        _sendValidator = sendValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Response<GetGroupChatByIdDto>> CreateAsync(CreateGroupChatDto dto)
    {
        await _createValidator.ValidateAndThrowAsync(dto);

        var currentId = _currentUser.GetRequiredUserId();

        var name = dto.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new BadRequestException("Название группы обязательно.");

        // Кандидаты в участники: без пустых, без дублей, без самого создателя.
        var requested = (dto.MemberUserIds ?? new List<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id) && id != currentId)
            .Distinct()
            .ToList();

        // Оставляем только реально существующих и не заблокированных (в любую сторону) с создателем.
        var existing = await _context.Users.AsNoTracking()
            .Where(u => requested.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync();

        var blockedIds = await AccessGuard.BlockRelatedUserIds(_context, currentId).ToListAsync();
        var memberIds = existing.Where(id => !blockedIds.Contains(id)).ToList();

        var now = DateTime.UtcNow;
        var group = new GroupChat
        {
            Name = name,
            CreatorUserId = currentId,
            CreatedAt = now
        };

        group.Members.Add(new GroupChatMember
        {
            UserId = currentId,
            Role = GroupMemberRole.Admin,
            JoinedAt = now,
            LastReadAt = now
        });

        foreach (var memberId in memberIds)
        {
            group.Members.Add(new GroupChatMember
            {
                UserId = memberId,
                Role = GroupMemberRole.Member,
                JoinedAt = now
            });
        }

        _context.GroupChats.Add(group);
        await _context.SaveChangesAsync();

        // Служебные сообщения о создании и добавлении стартовых участников.
        var names = await UserNamesAsync(memberIds.Append(currentId).ToList());
        var creatorName = names.GetValueOrDefault(currentId, "Пользователь");

        AddSystemMessage(group.Id, $"{creatorName} создал группу", now);
        foreach (var memberId in memberIds)
            AddSystemMessage(group.Id, $"{creatorName} добавил {names.GetValueOrDefault(memberId, "участника")}", now);

        await _context.SaveChangesAsync();

        return new Response<GetGroupChatByIdDto>(await BuildGroupDtoAsync(group.Id, currentId));
    }

    public async Task<PagedResponse<List<GetGroupChatDto>>> GetMyGroupsAsync(int? pageNumber, int? pageSize)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var (page, size) = Pagination.Normalize(pageNumber, pageSize);

        var all = await _context.GroupChatMembers.AsNoTracking()
            .Where(m => m.UserId == currentId)
            .Select(GroupChatProjections.ToListDto(currentId))
            .ToListAsync();

        // Свежие переписки выше; группы без сообщений (LastMessageDate == null) — в конце.
        all = all.OrderByDescending(c => c.LastMessageDate).ToList();

        var pageItems = all
            .Skip((page - 1) * size)
            .Take(size)
            .ToList();

        return new PagedResponse<List<GetGroupChatDto>>(pageItems, all.Count, page, size);
    }

    public async Task<Response<GetGroupChatByIdDto>> GetGroupByIdAsync(int? groupId)
    {
        if (groupId is null or <= 0)
            throw new BadRequestException("Некорректный Id группы.");

        var currentId = _currentUser.GetRequiredUserId();

        var member = await RequireMembershipAsync(groupId.Value, currentId);

        // Пометить группу прочитанной для текущего участника.
        member.LastReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new Response<GetGroupChatByIdDto>(await BuildGroupDtoAsync(groupId.Value, currentId));
    }

    public async Task<Response<GetGroupChatByIdDto>> AddMemberAsync(int? groupId, string? userId)
    {
        if (groupId is null or <= 0)
            throw new BadRequestException("Некорректный Id группы.");
        if (string.IsNullOrWhiteSpace(userId))
            throw new BadRequestException("Не указан пользователь.");

        var currentId = _currentUser.GetRequiredUserId();

        await RequireAdminAsync(groupId.Value, currentId);

        if (!await _context.Users.AnyAsync(u => u.Id == userId))
            throw new NotFoundException("Пользователь не найден.");

        if (await _context.GroupChatMembers.AnyAsync(m => m.GroupChatId == groupId && m.UserId == userId))
            throw new BadRequestException("Пользователь уже в группе.");

        if (await AccessGuard.IsBlockBetweenAsync(_context, currentId, userId))
            throw new ForbiddenException("Нельзя добавить пользователя из-за блокировки.");

        var now = DateTime.UtcNow;
        _context.GroupChatMembers.Add(new GroupChatMember
        {
            GroupChatId = groupId.Value,
            UserId = userId,
            Role = GroupMemberRole.Member,
            JoinedAt = now
        });

        var names = await UserNamesAsync(new List<string> { currentId, userId });
        var systemMessage = AddSystemMessage(groupId.Value,
            $"{names.GetValueOrDefault(currentId, "Админ")} добавил {names.GetValueOrDefault(userId, "участника")}", now);

        await _context.SaveChangesAsync();
        await BroadcastAsync(groupId.Value, systemMessage.Id);

        return new Response<GetGroupChatByIdDto>(await BuildGroupDtoAsync(groupId.Value, currentId));
    }

    public async Task<Response<GetGroupChatByIdDto>> RemoveMemberAsync(int? groupId, string? userId)
    {
        if (groupId is null or <= 0)
            throw new BadRequestException("Некорректный Id группы.");
        if (string.IsNullOrWhiteSpace(userId))
            throw new BadRequestException("Не указан пользователь.");

        var currentId = _currentUser.GetRequiredUserId();

        await RequireAdminAsync(groupId.Value, currentId);

        if (userId == currentId)
            throw new BadRequestException("Чтобы покинуть группу, используйте leave.");

        var target = await _context.GroupChatMembers
            .FirstOrDefaultAsync(m => m.GroupChatId == groupId && m.UserId == userId)
            ?? throw new NotFoundException("Пользователь не состоит в группе.");

        _context.GroupChatMembers.Remove(target);

        var now = DateTime.UtcNow;
        var names = await UserNamesAsync(new List<string> { currentId, userId });
        var systemMessage = AddSystemMessage(groupId.Value,
            $"{names.GetValueOrDefault(currentId, "Админ")} удалил {names.GetValueOrDefault(userId, "участника")}", now);

        await _context.SaveChangesAsync();
        await BroadcastAsync(groupId.Value, systemMessage.Id);

        return new Response<GetGroupChatByIdDto>(await BuildGroupDtoAsync(groupId.Value, currentId));
    }

    public async Task<Response<GetGroupChatByIdDto>> PromoteAdminAsync(int? groupId, string? userId)
    {
        if (groupId is null or <= 0)
            throw new BadRequestException("Некорректный Id группы.");
        if (string.IsNullOrWhiteSpace(userId))
            throw new BadRequestException("Не указан пользователь.");

        var currentId = _currentUser.GetRequiredUserId();

        await RequireAdminAsync(groupId.Value, currentId);

        var target = await _context.GroupChatMembers
            .FirstOrDefaultAsync(m => m.GroupChatId == groupId && m.UserId == userId)
            ?? throw new NotFoundException("Пользователь не состоит в группе.");

        if (target.Role == GroupMemberRole.Admin)
            throw new BadRequestException("Пользователь уже админ.");

        target.Role = GroupMemberRole.Admin;

        var now = DateTime.UtcNow;
        var names = await UserNamesAsync(new List<string> { userId });
        var systemMessage = AddSystemMessage(groupId.Value,
            $"{names.GetValueOrDefault(userId, "Участник")} назначен админом", now);

        await _context.SaveChangesAsync();
        await BroadcastAsync(groupId.Value, systemMessage.Id);

        return new Response<GetGroupChatByIdDto>(await BuildGroupDtoAsync(groupId.Value, currentId));
    }

    public async Task<Response<bool>> LeaveAsync(int? groupId)
    {
        if (groupId is null or <= 0)
            throw new BadRequestException("Некорректный Id группы.");

        var currentId = _currentUser.GetRequiredUserId();

        var member = await RequireMembershipAsync(groupId.Value, currentId);

        var now = DateTime.UtcNow;
        var myName = (await UserNamesAsync(new List<string> { currentId })).GetValueOrDefault(currentId, "Пользователь");

        _context.GroupChatMembers.Remove(member);
        var leaveMessage = AddSystemMessage(groupId.Value, $"{myName} вышел", now);
        await _context.SaveChangesAsync();

        // Если после ухода в группе не осталось админов (но остались участники) — передаём
        // админство самому давнему участнику, чтобы группа не осталась без управления.
        var systemMessageIds = new List<int> { leaveMessage.Id };
        var remaining = await _context.GroupChatMembers
            .Where(m => m.GroupChatId == groupId)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync();

        if (remaining.Count > 0 && remaining.All(m => m.Role != GroupMemberRole.Admin))
        {
            var promoted = remaining[0];
            promoted.Role = GroupMemberRole.Admin;
            var promotedName = (await UserNamesAsync(new List<string> { promoted.UserId }))
                .GetValueOrDefault(promoted.UserId, "Участник");
            var promoteMessage = AddSystemMessage(groupId.Value, $"{promotedName} назначен админом", now);
            await _context.SaveChangesAsync();
            systemMessageIds.Add(promoteMessage.Id);
        }

        foreach (var messageId in systemMessageIds)
            await BroadcastAsync(groupId.Value, messageId);

        return new Response<bool>(true);
    }

    public async Task<Response<GetGroupChatByIdDto>> UpdateInfoAsync(int? groupId, UpdateGroupInfoDto dto)
    {
        await _updateValidator.ValidateAndThrowAsync(dto);

        if (groupId is null or <= 0)
            throw new BadRequestException("Некорректный Id группы.");

        var currentId = _currentUser.GetRequiredUserId();

        await RequireAdminAsync(groupId.Value, currentId);

        var group = await _context.GroupChats.FirstOrDefaultAsync(g => g.Id == groupId)
            ?? throw new NotFoundException("Группа не найдена.");

        var nameChanged = false;
        var newName = dto.Name?.Trim();
        if (!string.IsNullOrWhiteSpace(newName) && newName != group.Name)
        {
            group.Name = newName;
            nameChanged = true;
        }

        if (dto.Avatar is not null)
        {
            var savedName = await _fileService.SaveFileAsync(dto.Avatar);
            var oldAvatar = group.Avatar;
            group.Avatar = savedName;
            if (!string.IsNullOrWhiteSpace(oldAvatar))
                _fileService.DeleteFile(oldAvatar);
        }

        int? systemMessageId = null;
        if (nameChanged)
        {
            var actorName = (await UserNamesAsync(new List<string> { currentId })).GetValueOrDefault(currentId, "Админ");
            var systemMessage = AddSystemMessage(groupId.Value,
                $"{actorName} сменил название на «{group.Name}»", DateTime.UtcNow);
            await _context.SaveChangesAsync();
            systemMessageId = systemMessage.Id;
        }
        else
        {
            await _context.SaveChangesAsync();
        }

        if (systemMessageId.HasValue)
            await BroadcastAsync(groupId.Value, systemMessageId.Value);

        return new Response<GetGroupChatByIdDto>(await BuildGroupDtoAsync(groupId.Value, currentId));
    }

    public async Task<Response<GetGroupMessageDto>> SendMessageAsync(int? groupId, SendGroupMessageDto dto)
    {
        await _sendValidator.ValidateAndThrowAsync(dto);

        if (groupId is null or <= 0)
            throw new BadRequestException("Некорректный Id группы.");
        if (string.IsNullOrWhiteSpace(dto.MessageText) && dto.File is null)
            throw new BadRequestException("Сообщение должно содержать текст или файл.");

        var currentId = _currentUser.GetRequiredUserId();

        var member = await RequireMembershipAsync(groupId.Value, currentId);

        // Reply допустим только на сообщение этой же группы.
        if (dto.ReplyToMessageId is > 0)
        {
            var replyExists = await _context.GroupMessages
                .AnyAsync(m => m.Id == dto.ReplyToMessageId && m.GroupChatId == groupId);
            if (!replyExists)
                throw new BadRequestException("Сообщение для ответа не найдено в этой группе.");
        }

        string? fileName = null;
        var messageType = MessageType.Text;
        if (dto.File is not null)
        {
            fileName = await _fileService.SaveFileAsync(dto.File, AttachmentExtensions, AttachmentMaxSize);
            messageType = ImageExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant())
                ? MessageType.Image
                : MessageType.File;
        }

        var message = new GroupMessage
        {
            GroupChatId = groupId.Value,
            SenderUserId = currentId,
            MessageText = string.IsNullOrWhiteSpace(dto.MessageText) ? null : dto.MessageText,
            FileName = fileName,
            MessageType = messageType,
            ReplyToMessageId = dto.ReplyToMessageId is > 0 ? dto.ReplyToMessageId : null,
            IsForwarded = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.GroupMessages.Add(message);

        // Отправитель, очевидно, всё прочитал.
        member.LastReadAt = message.CreatedAt;

        await _context.SaveChangesAsync();

        var result = await _context.GroupMessages.AsNoTracking()
            .Where(m => m.Id == message.Id)
            .Select(GroupChatProjections.MessageToDto)
            .FirstAsync();

        // Реал-тайм рассылка всем участникам группы.
        var memberIds = await GroupMemberIdsAsync(groupId.Value);
        if (memberIds.Count > 0)
            await _notifier.NotifyGroupMessageAsync(memberIds, result);

        return new Response<GetGroupMessageDto>(result);
    }

    public async Task<Response<bool>> DeleteMessageAsync(int? messageId)
    {
        if (messageId is null or <= 0)
            throw new BadRequestException("Некорректный Id сообщения.");

        var currentId = _currentUser.GetRequiredUserId();

        var message = await _context.GroupMessages
            .FirstOrDefaultAsync(m => m.Id == messageId)
            ?? throw new NotFoundException("Сообщение не найдено.");

        // Удалить может автор сообщения либо админ группы.
        if (message.SenderUserId != currentId)
        {
            var isAdmin = await _context.GroupChatMembers.AnyAsync(m =>
                m.GroupChatId == message.GroupChatId &&
                m.UserId == currentId &&
                m.Role == GroupMemberRole.Admin);
            if (!isAdmin)
                throw new ForbiddenException("Удалить сообщение может только его автор или админ группы.");
        }

        var fileName = message.FileName;
        var messageType = message.MessageType;

        // Реакции полиморфны (без FK на сообщение) — чистим вручную.
        await _context.MessageReactions
            .Where(r => r.MessageContext == MessageContext.Group && r.MessageId == message.Id)
            .ExecuteDeleteAsync();

        _context.GroupMessages.Remove(message);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(fileName))
            _fileService.DeleteFile(fileName, messageType == MessageType.Voice ? "voice" : "images");

        return new Response<bool>(true);
    }

    // ── Вспомогательные ───────────────────────────────────────────────────────

    /// <summary>Возвращает членство пользователя (tracked) или бросает 404/403 (нет группы/не участник).</summary>
    private async Task<GroupChatMember> RequireMembershipAsync(int groupId, string userId)
    {
        var member = await _context.GroupChatMembers
            .FirstOrDefaultAsync(m => m.GroupChatId == groupId && m.UserId == userId);
        if (member is not null)
            return member;

        var groupExists = await _context.GroupChats.AnyAsync(g => g.Id == groupId);
        if (!groupExists)
            throw new NotFoundException("Группа не найдена.");
        throw new ForbiddenException("Вы не участник этой группы.");
    }

    /// <summary>Требует роль Admin у пользователя в группе (иначе 403).</summary>
    private async Task<GroupChatMember> RequireAdminAsync(int groupId, string userId)
    {
        var member = await RequireMembershipAsync(groupId, userId);
        if (member.Role != GroupMemberRole.Admin)
            throw new ForbiddenException("Требуются права администратора группы.");
        return member;
    }

    /// <summary>Добавляет служебное (System) сообщение в контекст (без сохранения) и возвращает его.</summary>
    private GroupMessage AddSystemMessage(int groupId, string text, DateTime createdAt)
    {
        var message = new GroupMessage
        {
            GroupChatId = groupId,
            SenderUserId = null,
            MessageText = text,
            MessageType = MessageType.System,
            CreatedAt = createdAt
        };
        _context.GroupMessages.Add(message);
        return message;
    }

    /// <summary>Проецирует уже сохранённое сообщение и рассылает его всем участникам группы.</summary>
    private async Task BroadcastAsync(int groupId, int messageId)
    {
        var dto = await _context.GroupMessages.AsNoTracking()
            .Where(m => m.Id == messageId)
            .Select(GroupChatProjections.MessageToDto)
            .FirstAsync();

        var memberIds = await GroupMemberIdsAsync(groupId);
        if (memberIds.Count > 0)
            await _notifier.NotifyGroupMessageAsync(memberIds, dto);
    }

    /// <summary>Полная карточка группы: инфо + участники + сообщения + роль текущего юзера.</summary>
    private async Task<GetGroupChatByIdDto> BuildGroupDtoAsync(int groupId, string currentId)
    {
        var group = await _context.GroupChats.AsNoTracking()
            .Where(g => g.Id == groupId)
            .Select(g => new { g.Id, g.Name, g.Avatar, g.CreatorUserId, g.CreatedAt })
            .FirstAsync();

        var members = await _context.GroupChatMembers.AsNoTracking()
            .Where(m => m.GroupChatId == groupId)
            .OrderBy(m => m.JoinedAt)
            .Select(GroupChatProjections.MemberToDto)
            .ToListAsync();

        var messages = await _context.GroupMessages.AsNoTracking()
            .Where(m => m.GroupChatId == groupId)
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .Select(GroupChatProjections.MessageToDto)
            .ToListAsync();

        // Реакции догружаем отдельно (полиморфны, без навигации) и раскладываем по сообщениям.
        var reactions = await ReactionEnrichment.LoadAsync(
            _context, MessageContext.Group, messages.Select(m => m.Id).ToList());
        foreach (var m in messages)
            m.Reactions = reactions.GetValueOrDefault(m.Id) ?? new();

        return new GetGroupChatByIdDto
        {
            Id = group.Id,
            Name = group.Name,
            Avatar = group.Avatar,
            CreatorUserId = group.CreatorUserId,
            CreatedAt = group.CreatedAt,
            MyRole = members.FirstOrDefault(m => m.UserId == currentId)?.Role ?? GroupMemberRole.Member,
            Members = members,
            Messages = messages
        };
    }

    private Task<List<string>> GroupMemberIdsAsync(int groupId) =>
        _context.GroupChatMembers.AsNoTracking()
            .Where(m => m.GroupChatId == groupId)
            .Select(m => m.UserId)
            .ToListAsync();

    /// <summary>Словарь Id → UserName для набора пользователей (для текста служебных сообщений).</summary>
    private async Task<Dictionary<string, string>> UserNamesAsync(List<string> userIds) =>
        await _context.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName ?? string.Empty);
}
