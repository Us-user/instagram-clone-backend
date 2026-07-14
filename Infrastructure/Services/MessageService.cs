using Domain.DTOs.Message;
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
/// Кросс-контекстные операции над сообщениями (§8): реакции (тумблер/замена), пересылка (копия
/// содержимого с <c>IsForwarded=true</c>) и голосовые (аудио в <c>wwwroot/voice</c> + длительность
/// и волна). Работает одинаково для личных (Direct → <see cref="Message"/>) и групповых
/// (Group → <see cref="GroupMessage"/>) чатов; результат рассылается в реальном времени через
/// соответствующий SignalR-хаб. Id текущего юзера — из claims.
/// </summary>
public class MessageService : IMessageService
{
    /// <summary>Разрешённые расширения аудио для голосовых.</summary>
    private static readonly string[] VoiceExtensions =
        { ".mp3", ".wav", ".m4a", ".aac", ".ogg", ".oga", ".webm" };

    private const long VoiceMaxSize = 15 * 1024 * 1024; // 15 МБ
    private const string VoiceFolder = "voice";
    private const string ImagesFolder = "images";
    private const int MaxEmojiLength = 16;

    private readonly DataContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileService _fileService;
    private readonly IChatNotifier _chatNotifier;
    private readonly IGroupChatNotifier _groupNotifier;
    private readonly IValidator<SendVoiceDto> _voiceValidator;

    public MessageService(
        DataContext context,
        ICurrentUserService currentUser,
        IFileService fileService,
        IChatNotifier chatNotifier,
        IGroupChatNotifier groupNotifier,
        IValidator<SendVoiceDto> voiceValidator)
    {
        _context = context;
        _currentUser = currentUser;
        _fileService = fileService;
        _chatNotifier = chatNotifier;
        _groupNotifier = groupNotifier;
        _voiceValidator = voiceValidator;
    }

    public async Task<Response<MessageReactionsDto>> ReactAsync(int? messageId, MessageContext? context, string? emoji)
    {
        if (messageId is null or <= 0)
            throw new BadRequestException("Некорректный Id сообщения.");
        if (context is null)
            throw new BadRequestException("Не указан контекст сообщения (Direct/Group).");
        if (string.IsNullOrWhiteSpace(emoji))
            throw new BadRequestException("Не указан эмодзи реакции.");

        emoji = emoji.Trim();
        if (emoji.Length > MaxEmojiLength)
            throw new BadRequestException("Слишком длинная реакция.");

        var currentId = _currentUser.GetRequiredUserId();
        var ctx = context.Value;

        // Проверяем доступ к сообщению и собираем получателей real-time пуша.
        string directUser1 = string.Empty, directUser2 = string.Empty;
        var memberIds = new List<string>();

        if (ctx == MessageContext.Direct)
        {
            var chatId = await _context.Messages.AsNoTracking()
                .Where(m => m.Id == messageId)
                .Select(m => (int?)m.ChatId)
                .FirstOrDefaultAsync()
                ?? throw new NotFoundException("Сообщение не найдено.");

            var chat = await RequireDirectAccessAsync(chatId, currentId);
            var interlocutorId = chat.User1Id == currentId ? chat.User2Id : chat.User1Id;
            if (await AccessGuard.IsBlockBetweenAsync(_context, currentId, interlocutorId))
                throw new ForbiddenException("Реакция недоступна из-за блокировки.");

            directUser1 = chat.User1Id;
            directUser2 = chat.User2Id;
        }
        else
        {
            var source = await _context.GroupMessages.AsNoTracking()
                .Where(m => m.Id == messageId)
                .Select(m => new { m.GroupChatId, m.MessageType })
                .FirstOrDefaultAsync()
                ?? throw new NotFoundException("Сообщение не найдено.");

            if (source.MessageType == MessageType.System)
                throw new BadRequestException("На служебное сообщение нельзя реагировать.");

            await RequireGroupMemberAsync(source.GroupChatId, currentId);
            memberIds = await GroupMemberIdsAsync(source.GroupChatId);
        }

        // Тумблер/замена: нет реакции → добавить; та же → снять; другая → заменить.
        var existing = await _context.MessageReactions.FirstOrDefaultAsync(r =>
            r.MessageId == messageId && r.MessageContext == ctx && r.UserId == currentId);

        if (existing is null)
        {
            _context.MessageReactions.Add(new MessageReaction
            {
                MessageId = messageId.Value,
                MessageContext = ctx,
                UserId = currentId,
                Emoji = emoji,
                CreatedAt = DateTime.UtcNow
            });
        }
        else if (existing.Emoji == emoji)
        {
            _context.MessageReactions.Remove(existing);
        }
        else
        {
            existing.Emoji = emoji;
        }

        await _context.SaveChangesAsync();

        var reactions = (await ReactionEnrichment.LoadAsync(_context, ctx, new[] { messageId.Value }))
            .GetValueOrDefault(messageId.Value) ?? new List<MessageReactionDto>();
        var dto = new MessageReactionsDto { MessageId = messageId.Value, Context = ctx, Reactions = reactions };

        if (ctx == MessageContext.Direct)
            await _chatNotifier.NotifyReactionAsync(directUser1, directUser2, dto);
        else if (memberIds.Count > 0)
            await _groupNotifier.NotifyReactionAsync(memberIds, dto);

        return new Response<MessageReactionsDto>(dto);
    }

    public async Task<Response<object>> ForwardAsync(
        int? messageId, MessageContext? context, int? targetChatId, MessageContext? targetContext)
    {
        if (messageId is null or <= 0)
            throw new BadRequestException("Некорректный Id сообщения.");
        if (context is null)
            throw new BadRequestException("Не указан контекст исходного сообщения (Direct/Group).");
        if (targetChatId is null or <= 0)
            throw new BadRequestException("Некорректный Id целевого чата.");
        if (targetContext is null)
            throw new BadRequestException("Не указан контекст целевого чата (Direct/Group).");

        var currentId = _currentUser.GetRequiredUserId();

        // 1. Читаем содержимое источника с проверкой доступа.
        var source = await ReadForwardSourceAsync(messageId.Value, context.Value, currentId);

        // 2. Копируем файл в отдельный экземпляр (оригинал не связываем).
        var folder = FolderFor(source.MessageType);
        var copiedFile = source.FileName is null ? null : _fileService.CopyFile(source.FileName, folder);

        var now = DateTime.UtcNow;

        // 3. Создаём копию в целевом чате/группе.
        if (targetContext.Value == MessageContext.Direct)
        {
            var chat = await RequireDirectAccessAsync(targetChatId.Value, currentId);
            var interlocutorId = chat.User1Id == currentId ? chat.User2Id : chat.User1Id;
            if (await AccessGuard.IsBlockBetweenAsync(_context, currentId, interlocutorId))
                throw new ForbiddenException("Пересылка недоступна из-за блокировки.");

            var message = new Message
            {
                ChatId = chat.Id,
                SenderUserId = currentId,
                MessageText = source.MessageText,
                FileName = copiedFile,
                MessageType = source.MessageType,
                Duration = source.Duration,
                Waveform = source.Waveform,
                IsForwarded = true,
                CreatedAt = now,
                IsRead = false
            };
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            var result = await _context.Messages.AsNoTracking()
                .Where(m => m.Id == message.Id)
                .Select(ChatProjections.MessageToDto)
                .FirstAsync();

            await _chatNotifier.NotifyMessageAsync(chat.User1Id, chat.User2Id, result);
            return new Response<object>(result);
        }
        else
        {
            var member = await RequireGroupMemberAsync(targetChatId.Value, currentId);

            var message = new GroupMessage
            {
                GroupChatId = targetChatId.Value,
                SenderUserId = currentId,
                MessageText = source.MessageText,
                FileName = copiedFile,
                MessageType = source.MessageType,
                Duration = source.Duration,
                Waveform = source.Waveform,
                IsForwarded = true,
                CreatedAt = now
            };
            _context.GroupMessages.Add(message);
            member.LastReadAt = now;
            await _context.SaveChangesAsync();

            var result = await _context.GroupMessages.AsNoTracking()
                .Where(m => m.Id == message.Id)
                .Select(GroupChatProjections.MessageToDto)
                .FirstAsync();

            var memberIds = await GroupMemberIdsAsync(targetChatId.Value);
            if (memberIds.Count > 0)
                await _groupNotifier.NotifyGroupMessageAsync(memberIds, result);
            return new Response<object>(result);
        }
    }

    public async Task<Response<object>> SendVoiceAsync(SendVoiceDto dto)
    {
        await _voiceValidator.ValidateAndThrowAsync(dto);

        if (dto.File is null)
            throw new BadRequestException("Не передан аудиофайл.");

        var currentId = _currentUser.GetRequiredUserId();
        var now = DateTime.UtcNow;
        var waveform = WaveformGenerator.Placeholder(dto.Duration);

        if (dto.Context == MessageContext.Direct)
        {
            var chat = await RequireDirectAccessAsync(dto.ChatId, currentId);
            var interlocutorId = chat.User1Id == currentId ? chat.User2Id : chat.User1Id;
            if (await AccessGuard.IsBlockBetweenAsync(_context, currentId, interlocutorId))
                throw new ForbiddenException("Отправка голосового недоступна из-за блокировки.");

            if (dto.ReplyToMessageId is > 0 &&
                !await _context.Messages.AnyAsync(m => m.Id == dto.ReplyToMessageId && m.ChatId == dto.ChatId))
                throw new BadRequestException("Сообщение для ответа не найдено в этом чате.");

            var fileName = await _fileService.SaveFileAsync(dto.File, VoiceExtensions, VoiceMaxSize, VoiceFolder);
            var message = new Message
            {
                ChatId = dto.ChatId,
                SenderUserId = currentId,
                MessageType = MessageType.Voice,
                FileName = fileName,
                Duration = dto.Duration,
                Waveform = waveform,
                ReplyToMessageId = dto.ReplyToMessageId is > 0 ? dto.ReplyToMessageId : null,
                CreatedAt = now,
                IsRead = false
            };
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            var result = await _context.Messages.AsNoTracking()
                .Where(m => m.Id == message.Id)
                .Select(ChatProjections.MessageToDto)
                .FirstAsync();

            await _chatNotifier.NotifyMessageAsync(chat.User1Id, chat.User2Id, result);
            return new Response<object>(result);
        }
        else
        {
            var member = await RequireGroupMemberAsync(dto.ChatId, currentId);

            if (dto.ReplyToMessageId is > 0 &&
                !await _context.GroupMessages.AnyAsync(m => m.Id == dto.ReplyToMessageId && m.GroupChatId == dto.ChatId))
                throw new BadRequestException("Сообщение для ответа не найдено в этой группе.");

            var fileName = await _fileService.SaveFileAsync(dto.File, VoiceExtensions, VoiceMaxSize, VoiceFolder);
            var message = new GroupMessage
            {
                GroupChatId = dto.ChatId,
                SenderUserId = currentId,
                MessageType = MessageType.Voice,
                FileName = fileName,
                Duration = dto.Duration,
                Waveform = waveform,
                ReplyToMessageId = dto.ReplyToMessageId is > 0 ? dto.ReplyToMessageId : null,
                CreatedAt = now
            };
            _context.GroupMessages.Add(message);
            member.LastReadAt = now;
            await _context.SaveChangesAsync();

            var result = await _context.GroupMessages.AsNoTracking()
                .Where(m => m.Id == message.Id)
                .Select(GroupChatProjections.MessageToDto)
                .FirstAsync();

            var memberIds = await GroupMemberIdsAsync(dto.ChatId);
            if (memberIds.Count > 0)
                await _groupNotifier.NotifyGroupMessageAsync(memberIds, result);
            return new Response<object>(result);
        }
    }

    // ── Вспомогательные ───────────────────────────────────────────────────────

    /// <summary>Содержимое пересылаемого сообщения (без ссылки на оригинал).</summary>
    private sealed record ForwardSource(
        string? MessageText, string? FileName, MessageType MessageType, int? Duration, string? Waveform);

    /// <summary>Читает содержимое источника пересылки и проверяет доступ текущего юзера к нему.</summary>
    private async Task<ForwardSource> ReadForwardSourceAsync(int messageId, MessageContext context, string currentId)
    {
        if (context == MessageContext.Direct)
        {
            var m = await _context.Messages.AsNoTracking()
                .Where(x => x.Id == messageId)
                .Select(x => new { x.ChatId, x.MessageText, x.FileName, x.MessageType, x.Duration, x.Waveform })
                .FirstOrDefaultAsync()
                ?? throw new NotFoundException("Сообщение не найдено.");

            await RequireDirectAccessAsync(m.ChatId, currentId);
            return new ForwardSource(m.MessageText, m.FileName, m.MessageType, m.Duration, m.Waveform);
        }

        var g = await _context.GroupMessages.AsNoTracking()
            .Where(x => x.Id == messageId)
            .Select(x => new { x.GroupChatId, x.MessageText, x.FileName, x.MessageType, x.Duration, x.Waveform })
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Сообщение не найдено.");

        if (g.MessageType == MessageType.System)
            throw new BadRequestException("Служебное сообщение нельзя переслать.");

        await RequireGroupMemberAsync(g.GroupChatId, currentId);
        return new ForwardSource(g.MessageText, g.FileName, g.MessageType, g.Duration, g.Waveform);
    }

    /// <summary>Загружает чат (no-track) и проверяет, что текущий юзер — участник (иначе 404/403).</summary>
    private async Task<Chat> RequireDirectAccessAsync(int chatId, string userId)
    {
        var chat = await _context.Chats.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == chatId)
            ?? throw new NotFoundException("Чат не найден.");

        if (chat.User1Id != userId && chat.User2Id != userId)
            throw new ForbiddenException("Нет доступа к этому чату.");

        return chat;
    }

    /// <summary>Возвращает членство пользователя (tracked) или бросает 404/403 (нет группы/не участник).</summary>
    private async Task<GroupChatMember> RequireGroupMemberAsync(int groupId, string userId)
    {
        var member = await _context.GroupChatMembers
            .FirstOrDefaultAsync(m => m.GroupChatId == groupId && m.UserId == userId);
        if (member is not null)
            return member;

        if (!await _context.GroupChats.AnyAsync(g => g.Id == groupId))
            throw new NotFoundException("Группа не найдена.");
        throw new ForbiddenException("Вы не участник этой группы.");
    }

    private static string FolderFor(MessageType type) =>
        type == MessageType.Voice ? VoiceFolder : ImagesFolder;

    private Task<List<string>> GroupMemberIdsAsync(int groupId) =>
        _context.GroupChatMembers.AsNoTracking()
            .Where(m => m.GroupChatId == groupId)
            .Select(m => m.UserId)
            .ToListAsync();
}
