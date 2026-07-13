using Domain.DTOs.Chat;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Responses;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Чаты и сообщения: список чатов с последним сообщением и непрочитанными, открытие чата
/// (с пометкой прочитанным), создание/дедуп чата, отправка сообщения с рассылкой через SignalR,
/// удаление сообщения (только отправитель) и чата (только участник). Id текущего юзера — из claims.
/// </summary>
public class ChatService : IChatService
{
    private readonly DataContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileService _fileService;
    private readonly IChatNotifier _notifier;

    public ChatService(
        DataContext context,
        ICurrentUserService currentUser,
        IFileService fileService,
        IChatNotifier notifier)
    {
        _context = context;
        _currentUser = currentUser;
        _fileService = fileService;
        _notifier = notifier;
    }

    public async Task<Response<List<GetChatDto>>> GetChatsAsync()
    {
        var currentId = _currentUser.GetRequiredUserId();

        var chats = await _context.Chats.AsNoTracking()
            .Where(c => c.User1Id == currentId || c.User2Id == currentId)
            .Select(ChatProjections.ToListDto(currentId))
            .ToListAsync();

        // Свежие переписки выше; чаты без сообщений (LastMessageDate == null) — в конце.
        chats = chats.OrderByDescending(c => c.LastMessageDate).ToList();

        return new Response<List<GetChatDto>>(chats);
    }

    public async Task<Response<GetChatByIdDto>> GetChatByIdAsync(int? chatId)
    {
        if (chatId is null or <= 0)
            throw new BadRequestException("Некорректный Id чата.");

        var currentId = _currentUser.GetRequiredUserId();

        var chat = await _context.Chats
            .FirstOrDefaultAsync(c => c.Id == chatId)
            ?? throw new NotFoundException("Чат не найден.");

        if (chat.User1Id != currentId && chat.User2Id != currentId)
            throw new ForbiddenException("Нет доступа к этому чату.");

        // Пометить прочитанными входящие непрочитанные сообщения (не свои).
        var unread = await _context.Messages
            .Where(m => m.ChatId == chat.Id && m.SenderUserId != currentId && !m.IsRead)
            .ToListAsync();
        if (unread.Count > 0)
        {
            unread.ForEach(m => m.IsRead = true);
            await _context.SaveChangesAsync();
        }

        var interlocutorId = chat.User1Id == currentId ? chat.User2Id : chat.User1Id;
        var interlocutor = await _context.Users.AsNoTracking()
            .Where(u => u.Id == interlocutorId)
            .Select(u => new { u.UserName, u.Avatar })
            .FirstOrDefaultAsync();

        var messages = await _context.Messages.AsNoTracking()
            .Where(m => m.ChatId == chat.Id)
            .OrderBy(m => m.CreatedAt)
            .Select(ChatProjections.MessageToDto)
            .ToListAsync();

        var dto = new GetChatByIdDto
        {
            Id = chat.Id,
            UserId = interlocutorId,
            UserName = interlocutor?.UserName ?? string.Empty,
            UserImage = interlocutor?.Avatar,
            Messages = messages
        };

        return new Response<GetChatByIdDto>(dto);
    }

    public async Task<Response<GetChatDto>> CreateChatAsync(string? receiverUserId)
    {
        var currentId = _currentUser.GetRequiredUserId();

        if (string.IsNullOrWhiteSpace(receiverUserId))
            throw new BadRequestException("Не указан получатель.");
        if (receiverUserId == currentId)
            throw new BadRequestException("Нельзя создать чат с самим собой.");

        var receiverExists = await _context.Users.AnyAsync(u => u.Id == receiverUserId);
        if (!receiverExists)
            throw new NotFoundException("Получатель не найден.");

        // Нормализуем порядок участников — одна пара всегда даёт один (User1Id, User2Id),
        // что обеспечивает дедуп и работу уникального индекса на паре.
        var (user1, user2) = string.CompareOrdinal(currentId, receiverUserId) <= 0
            ? (currentId, receiverUserId)
            : (receiverUserId, currentId);

        var chat = await _context.Chats
            .FirstOrDefaultAsync(c => c.User1Id == user1 && c.User2Id == user2);

        if (chat is null)
        {
            chat = new Chat { User1Id = user1, User2Id = user2, CreatedAt = DateTime.UtcNow };
            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();
        }

        var dto = await _context.Chats.AsNoTracking()
            .Where(c => c.Id == chat.Id)
            .Select(ChatProjections.ToListDto(currentId))
            .FirstAsync();

        return new Response<GetChatDto>(dto);
    }

    public async Task<Response<GetMessageDto>> SendMessageAsync(SendMessageDto dto)
    {
        var currentId = _currentUser.GetRequiredUserId();

        if (dto.ChatId <= 0)
            throw new BadRequestException("Некорректный Id чата.");
        if (string.IsNullOrWhiteSpace(dto.MessageText) && dto.File is null)
            throw new BadRequestException("Сообщение должно содержать текст или файл.");

        var chat = await _context.Chats
            .FirstOrDefaultAsync(c => c.Id == dto.ChatId)
            ?? throw new NotFoundException("Чат не найден.");

        if (chat.User1Id != currentId && chat.User2Id != currentId)
            throw new ForbiddenException("Нет доступа к этому чату.");

        string? fileName = null;
        if (dto.File is not null)
            fileName = await _fileService.SaveFileAsync(dto.File);

        var message = new Message
        {
            ChatId = chat.Id,
            SenderUserId = currentId,
            MessageText = string.IsNullOrWhiteSpace(dto.MessageText) ? null : dto.MessageText,
            FileName = fileName,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        var result = await _context.Messages.AsNoTracking()
            .Where(m => m.Id == message.Id)
            .Select(ChatProjections.MessageToDto)
            .FirstAsync();

        // Реал-тайм доставка обоим участникам (получателю и другим устройствам отправителя).
        await _notifier.NotifyMessageAsync(chat.User1Id, chat.User2Id, result);

        return new Response<GetMessageDto>(result);
    }

    public async Task<Response<bool>> DeleteMessageAsync(int? massageId)
    {
        if (massageId is null or <= 0)
            throw new BadRequestException("Некорректный Id сообщения.");

        var currentId = _currentUser.GetRequiredUserId();

        var message = await _context.Messages
            .FirstOrDefaultAsync(m => m.Id == massageId)
            ?? throw new NotFoundException("Сообщение не найдено.");

        if (message.SenderUserId != currentId)
            throw new ForbiddenException("Нельзя удалить чужое сообщение.");

        var fileName = message.FileName;

        _context.Messages.Remove(message);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(fileName))
            _fileService.DeleteFile(fileName);

        return new Response<bool>(true);
    }

    public async Task<Response<bool>> DeleteChatAsync(int? chatId)
    {
        if (chatId is null or <= 0)
            throw new BadRequestException("Некорректный Id чата.");

        var currentId = _currentUser.GetRequiredUserId();

        var chat = await _context.Chats
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == chatId)
            ?? throw new NotFoundException("Чат не найден.");

        if (chat.User1Id != currentId && chat.User2Id != currentId)
            throw new ForbiddenException("Нет доступа к этому чату.");

        // Имена файлов вложений собираем до удаления; сам чат каскадом чистит сообщения.
        var fileNames = chat.Messages
            .Where(m => !string.IsNullOrWhiteSpace(m.FileName))
            .Select(m => m.FileName!)
            .ToList();

        _context.Chats.Remove(chat);
        await _context.SaveChangesAsync();

        foreach (var fileName in fileNames)
            _fileService.DeleteFile(fileName);

        return new Response<bool>(true);
    }
}
