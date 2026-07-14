using Domain.DTOs.Chat;
using Domain.DTOs.Story;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Responses;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Сторис: ленты (подписки/юзер/мои), лайк-тумблер, уникальный просмотр, создание из поста
/// или файла, удаление автором. Сторис живёт 24 часа — во всех выборках применяется окно
/// <c>CreatedAt &gt; UtcNow - 24ч</c>. Id текущего юзера берётся из claims; чужую сторис удалять нельзя.
/// Проекция в DTO — общий <see cref="StoryProjections.ToDto"/>. §9: close-friends-аудитория
/// фильтруется по членству зрителя, ответы уходят в директ, репост поста ссылается на оригинал.
/// </summary>
public class StoryService : IStoryService
{
    private readonly DataContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileService _fileService;
    private readonly IChatNotifier _chatNotifier;
    private readonly INotificationService _notifications;
    private readonly IMentionService _mentions;

    public StoryService(
        DataContext context,
        ICurrentUserService currentUser,
        IFileService fileService,
        IChatNotifier chatNotifier,
        INotificationService notifications,
        IMentionService mentions)
    {
        _context = context;
        _currentUser = currentUser;
        _fileService = fileService;
        _chatNotifier = chatNotifier;
        _notifications = notifications;
        _mentions = mentions;
    }

    /// <summary>Граница активности сторис: моложе 24 часов.</summary>
    private static DateTime ActiveSince => DateTime.UtcNow.AddHours(-24);

    public async Task<Response<List<GetStoryDto>>> GetStoriesAsync()
    {
        var currentId = _currentUser.GetRequiredUserId();
        var since = ActiveSince;

        // Источник сторис ленты — авторы, на кого текущий юзер подписан ОДОБРЕННО.
        // Харденинг (§13): pending-запрос к приватному аккаунту не даёт доступ к его сторис
        // до принятия (блокировка снимает подписку в обе стороны — блок здесь уже исключён).
        var followingIds = _context.FollowingRelationShips.AsNoTracking()
            .Where(f => f.UserId == currentId && f.Status == FollowStatus.Accepted)
            .Select(f => f.FollowingUserId);

        // Группировка по авторам выражена сортировкой: сначала по автору, внутри — свежие выше.
        // §9: close-friends-сторис показываем только если текущий юзер в близких у автора.
        var stories = await _context.Stories.AsNoTracking()
            .Where(s => followingIds.Contains(s.UserId) && s.CreatedAt > since
                && (s.Audience == StoryAudience.All
                    || _context.CloseFriends.Any(cf => cf.UserId == s.UserId && cf.FriendUserId == currentId)))
            .OrderBy(s => s.UserId)
            .ThenByDescending(s => s.CreatedAt)
            .Select(StoryProjections.ToDto())
            .ToListAsync();

        return new Response<List<GetStoryDto>>(stories);
    }

    public async Task<Response<List<GetStoryDto>>> GetUserStoriesAsync(string userId)
    {
        var currentId = _currentUser.GetRequiredUserId();

        if (string.IsNullOrWhiteSpace(userId))
            throw new BadRequestException("Некорректный Id пользователя.");

        // Харденинг (§13): блокировка в любую сторону или приватный аккаунт без одобренной
        // подписки — сторис автора не отдаём (пустая лента, как в остальных выдачах контента).
        if (!await AccessGuard.CanViewContentAsync(_context, userId, currentId))
            return new Response<List<GetStoryDto>>(new List<GetStoryDto>());

        var since = ActiveSince;
        // §9: свои close-friends-сторис видны себе всегда; чужие — только если ты в близких у автора.
        var stories = await _context.Stories.AsNoTracking()
            .Where(s => s.UserId == userId && s.CreatedAt > since
                && (s.UserId == currentId
                    || s.Audience == StoryAudience.All
                    || _context.CloseFriends.Any(cf => cf.UserId == s.UserId && cf.FriendUserId == currentId)))
            .OrderByDescending(s => s.CreatedAt)
            .Select(StoryProjections.ToDto())
            .ToListAsync();

        return new Response<List<GetStoryDto>>(stories);
    }

    public async Task<Response<List<GetStoryDto>>> GetMyStoriesAsync()
    {
        var currentId = _currentUser.GetRequiredUserId();

        var since = ActiveSince;
        var stories = await _context.Stories.AsNoTracking()
            .Where(s => s.UserId == currentId && s.CreatedAt > since)
            .OrderByDescending(s => s.CreatedAt)
            .Select(StoryProjections.ToDto())
            .ToListAsync();

        return new Response<List<GetStoryDto>>(stories);
    }

    public async Task<Response<string>> LikeStoryAsync(int? storyId)
    {
        if (storyId is null or <= 0)
            throw new BadRequestException("Некорректный Id сторис.");

        var currentId = _currentUser.GetRequiredUserId();
        await EnsureStoryExistsAsync(storyId.Value);

        var existing = await _context.StoryLikes
            .FirstOrDefaultAsync(l => l.StoryId == storyId && l.UserId == currentId);

        string message;
        if (existing is not null)
        {
            _context.StoryLikes.Remove(existing);
            message = "Лайк убран.";
        }
        else
        {
            _context.StoryLikes.Add(new StoryLike { StoryId = storyId.Value, UserId = currentId });
            message = "Лайк добавлен.";
        }

        await _context.SaveChangesAsync();
        return new Response<string>(message);
    }

    public async Task<Response<GetStoryDto>> GetStoryByIdAsync(int? id)
    {
        if (id is null or <= 0)
            throw new BadRequestException("Некорректный Id сторис.");

        var currentId = _currentUser.GetRequiredUserId();

        var story = await _context.Stories.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(StoryProjections.ToDto())
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Сторис не найдена.");

        // Харденинг (§13): блокировка в любую сторону или приватный аккаунт без одобренной
        // подписки — сторис не показываем (404, чтобы не раскрывать факт существования).
        if (!await AccessGuard.CanViewContentAsync(_context, story.UserId, currentId))
            throw new NotFoundException("Сторис не найдена.");

        // §9: close-friends-сторис доступна только автору и тем, кто у него в близких.
        if (story.Audience == StoryAudience.CloseFriends
            && story.UserId != currentId
            && !await IsCloseFriendOfAsync(story.UserId, currentId))
            throw new NotFoundException("Сторис не найдена.");

        return new Response<GetStoryDto>(story);
    }

    public async Task<Response<GetStoryDto>> AddStoriesAsync(int? postId, AddStoryDto dto)
    {
        var currentId = _currentUser.GetRequiredUserId();

        string? fileName = null;
        int? sourcePostId = null;

        if (postId is > 0)
        {
            // Сторис из поста: сохраняем ссылку на пост, собственного файла нет.
            var postExists = await _context.Posts.AnyAsync(p => p.Id == postId);
            if (!postExists)
                throw new NotFoundException("Пост-источник не найден.");
            sourcePostId = postId;
        }
        else if (dto.Image is not null)
        {
            // Сторис из файла: сохраняем файл через общий IFileService.
            fileName = await _fileService.SaveFileAsync(dto.Image);
        }
        else
        {
            throw new BadRequestException("Нужно указать PostId или прикрепить файл сторис.");
        }

        var story = new Story
        {
            UserId = currentId,
            FileName = fileName,
            PostId = sourcePostId,
            Audience = dto.Audience ?? StoryAudience.All,
            CreatedAt = DateTime.UtcNow
        };

        _context.Stories.Add(story);
        await _context.SaveChangesAsync();

        var result = await _context.Stories.AsNoTracking()
            .Where(s => s.Id == story.Id)
            .Select(StoryProjections.ToDto())
            .FirstAsync();

        return new Response<GetStoryDto>(result);
    }

    public async Task<Response<bool>> DeleteStoryAsync(int? id)
    {
        if (id is null or <= 0)
            throw new BadRequestException("Некорректный Id сторис.");

        var currentId = _currentUser.GetRequiredUserId();

        var story = await _context.Stories
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new NotFoundException("Сторис не найдена.");

        if (story.UserId != currentId)
            throw new ForbiddenException("Нельзя удалить чужую сторис.");

        // Собственный файл сторис удаляем с диска; сторис из поста своего файла не имеет.
        var fileName = story.FileName;

        _context.Stories.Remove(story);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(fileName))
            _fileService.DeleteFile(fileName);

        return new Response<bool>(true);
    }

    public async Task<Response<GetStoryViewDto>> AddStoryViewAsync(int? storyId)
    {
        if (storyId is null or <= 0)
            throw new BadRequestException("Некорректный Id сторис.");

        var currentId = _currentUser.GetRequiredUserId();
        await EnsureStoryExistsAsync(storyId.Value);

        // Просмотр уникален на юзера — повторный вызов возвращает существующую запись.
        var view = await _context.StoryViews
            .FirstOrDefaultAsync(v => v.StoryId == storyId && v.ViewUserId == currentId);

        if (view is null)
        {
            view = new StoryView { StoryId = storyId.Value, ViewUserId = currentId };
            _context.StoryViews.Add(view);
            await _context.SaveChangesAsync();
        }

        return new Response<GetStoryViewDto>(new GetStoryViewDto
        {
            Id = view.Id,
            ViewUserId = view.ViewUserId,
            StoryId = view.StoryId
        });
    }

    public async Task<Response<GetMessageDto>> ReplyAsync(int? storyId, StoryReplyRequestDto dto)
    {
        if (storyId is null or <= 0)
            throw new BadRequestException("Некорректный Id сторис.");
        if (dto is null || string.IsNullOrWhiteSpace(dto.Text))
            throw new BadRequestException("Текст ответа не может быть пустым.");

        var currentId = _currentUser.GetRequiredUserId();

        var story = await _context.Stories.AsNoTracking()
            .Where(s => s.Id == storyId)
            .Select(s => new { s.Id, s.UserId, s.Audience, s.CreatedAt })
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Сторис не найдена.");

        // Отвечать можно только на активную (< 24ч) чужую сторис.
        if (story.CreatedAt <= ActiveSince)
            throw new BadRequestException("Сторис больше не активна.");
        if (story.UserId == currentId)
            throw new BadRequestException("Нельзя ответить на собственную сторис.");

        var authorId = story.UserId;

        // Блокировка в любую сторону — ответ недоступен.
        if (await AccessGuard.IsBlockBetweenAsync(_context, currentId, authorId))
            throw new ForbiddenException("Ответ недоступен из-за блокировки.");

        // Close-friends-сторис: ответить может только тот, кто её вообще видит (в близких у автора).
        if (story.Audience == StoryAudience.CloseFriends
            && !await IsCloseFriendOfAsync(authorId, currentId))
            throw new NotFoundException("Сторис не найдена.");

        // Настройка «кто может отвечать на сторис» автора (§6).
        var whoCanReply = await _context.PrivacySettings.AsNoTracking()
            .Where(s => s.UserId == authorId)
            .Select(s => (WhoCanReplyStory?)s.WhoCanReplyStory)
            .FirstOrDefaultAsync() ?? WhoCanReplyStory.Everyone;

        var allowed = whoCanReply switch
        {
            WhoCanReplyStory.Everyone => true,
            WhoCanReplyStory.Followers => await AccessGuard.IsAcceptedFollowerAsync(_context, currentId, authorId),
            WhoCanReplyStory.CloseFriends => await IsCloseFriendOfAsync(authorId, currentId),
            _ => false // Nobody
        };
        if (!allowed)
            throw new ForbiddenException("Автор ограничил ответы на свои сторис.");

        // Ответ уходит в директ: находим/создаём чат 1:1, создаём сообщение + запись-связку.
        var chat = await GetOrCreateChatAsync(currentId, authorId);

        var message = new Message
        {
            Chat = chat,
            SenderUserId = currentId,
            MessageText = dto.Text,
            MessageType = MessageType.Text,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };
        _context.Messages.Add(message);

        var storyReply = new StoryReply
        {
            StoryId = story.Id,
            FromUserId = currentId,
            Message = message,
            CreatedAt = DateTime.UtcNow
        };
        _context.StoryReplies.Add(storyReply);

        await _context.SaveChangesAsync();

        // Уведомление автору сторис (entity — Story, id — самой сторис).
        await _notifications.CreateAsync(
            authorId, currentId, NotificationType.StoryReply, NotificationEntityType.Story, story.Id);

        // Упоминания (@username) в тексте ответа (задел StoryReply из Phase 13).
        await _mentions.ProcessMentionsAsync(
            dto.Text, currentId, MentionEntityType.StoryReply, storyReply.Id);

        var result = await _context.Messages.AsNoTracking()
            .Where(m => m.Id == message.Id)
            .Select(ChatProjections.MessageToDto)
            .FirstAsync();

        // Реал-тайм доставка сообщения обоим участникам чата.
        await _chatNotifier.NotifyMessageAsync(chat.User1Id, chat.User2Id, result);

        return new Response<GetMessageDto>(result);
    }

    public async Task<Response<GetStoryDto>> SharePostAsync(int? postId)
    {
        if (postId is null or <= 0)
            throw new BadRequestException("Некорректный Id поста.");

        var currentId = _currentUser.GetRequiredUserId();

        var post = await _context.Posts.AsNoTracking()
            .Where(p => p.Id == postId)
            .Select(p => new { p.Id, p.UserId, IsPrivate = p.User!.IsPrivate })
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Пост не найден.");

        // Репост чужого поста: только публичный автор и без блокировки в любую сторону.
        if (post.UserId != currentId)
        {
            if (await AccessGuard.IsBlockBetweenAsync(_context, currentId, post.UserId))
                throw new ForbiddenException("Репост недоступен из-за блокировки.");
            if (post.IsPrivate)
                throw new ForbiddenException("Нельзя репостить пост из приватного аккаунта.");
        }

        var story = new Story
        {
            UserId = currentId,
            SharedPostId = post.Id,
            Audience = StoryAudience.All,
            CreatedAt = DateTime.UtcNow
        };

        _context.Stories.Add(story);
        await _context.SaveChangesAsync();

        // Уведомление автору оригинала (правило «не себе» — внутри CreateAsync).
        await _notifications.CreateAsync(
            post.UserId, currentId, NotificationType.PostShared, NotificationEntityType.Post, post.Id);

        var result = await _context.Stories.AsNoTracking()
            .Where(s => s.Id == story.Id)
            .Select(StoryProjections.ToDto())
            .FirstAsync();

        return new Response<GetStoryDto>(result);
    }

    /// <summary>Находит существующий чат 1:1 или создаёт новый (нормализованный порядок участников).</summary>
    private async Task<Chat> GetOrCreateChatAsync(string currentId, string otherId)
    {
        var (user1, user2) = string.CompareOrdinal(currentId, otherId) <= 0
            ? (currentId, otherId)
            : (otherId, currentId);

        var chat = await _context.Chats
            .FirstOrDefaultAsync(c => c.User1Id == user1 && c.User2Id == user2);

        if (chat is null)
        {
            chat = new Chat { User1Id = user1, User2Id = user2, CreatedAt = DateTime.UtcNow };
            _context.Chats.Add(chat);
        }

        return chat;
    }

    /// <summary>В близких ли <paramref name="viewerId"/> у пользователя <paramref name="ownerId"/>.</summary>
    private Task<bool> IsCloseFriendOfAsync(string ownerId, string viewerId) =>
        _context.CloseFriends.AnyAsync(cf => cf.UserId == ownerId && cf.FriendUserId == viewerId);

    /// <summary>Гарантирует существование сторис; иначе <see cref="NotFoundException"/> (404).</summary>
    private async Task EnsureStoryExistsAsync(int storyId)
    {
        var exists = await _context.Stories.AnyAsync(s => s.Id == storyId);
        if (!exists)
            throw new NotFoundException("Сторис не найдена.");
    }
}
