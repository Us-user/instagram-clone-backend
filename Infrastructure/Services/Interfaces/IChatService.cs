using Domain.DTOs.Chat;
using Domain.Responses;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Чаты и сообщения. Id текущего юзера — из claims. Доступ к чату — только участникам;
/// удалять можно только своё сообщение. Отправка сообщения дублируется в реальном времени
/// через <see cref="IChatNotifier"/>.
/// </summary>
public interface IChatService
{
    /// <summary>Чаты текущего юзера: собеседник + последнее сообщение + непрочитанные.</summary>
    Task<Response<List<GetChatDto>>> GetChatsAsync();

    /// <summary>Чат со всей перепиской; входящие непрочитанные помечаются прочитанными.</summary>
    Task<Response<GetChatByIdDto>> GetChatByIdAsync(int? chatId);

    /// <summary>Создать чат с получателем или вернуть существующий (дедуп по паре участников).</summary>
    Task<Response<GetChatDto>> CreateChatAsync(string? receiverUserId);

    /// <summary>Отправить сообщение (текст и/или файл) в чат + рассылка через SignalR.</summary>
    Task<Response<GetMessageDto>> SendMessageAsync(SendMessageDto dto);

    /// <summary>Удалить сообщение — только отправитель (параметр <c>massageId</c> — опечатка контракта).</summary>
    Task<Response<bool>> DeleteMessageAsync(int? massageId);

    /// <summary>Удалить чат со всеми сообщениями — только участник.</summary>
    Task<Response<bool>> DeleteChatAsync(int? chatId);
}
