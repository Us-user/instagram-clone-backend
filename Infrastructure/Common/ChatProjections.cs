using System.Linq.Expressions;
using Domain.DTOs.Chat;
using Domain.Entities;

namespace Infrastructure.Common;

/// <summary>
/// Переиспользуемые проекции чата в DTO. Написаны выражениями, чтобы EF перевёл их в SQL
/// (использовать в <c>Select</c> над <see cref="IQueryable{T}"/>). Собеседник, последнее
/// сообщение и число непрочитанных вычисляются относительно текущего пользователя.
/// </summary>
public static class ChatProjections
{
    /// <summary>
    /// Проекция <see cref="Chat"/> → <see cref="GetChatDto"/> для списка чатов:
    /// собеседник (не <paramref name="currentUserId"/>), последнее сообщение и непрочитанные.
    /// </summary>
    public static Expression<Func<Chat, GetChatDto>> ToListDto(string currentUserId) =>
        c => new GetChatDto
        {
            Id = c.Id,
            UserId = c.User1Id == currentUserId ? c.User2Id : c.User1Id,
            UserName = (c.User1Id == currentUserId ? c.User2!.UserName : c.User1!.UserName) ?? string.Empty,
            UserImage = c.User1Id == currentUserId ? c.User2!.Avatar : c.User1!.Avatar,
            LastMessage = c.Messages
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => m.MessageText)
                .FirstOrDefault(),
            LastMessageDate = c.Messages
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => (DateTime?)m.CreatedAt)
                .FirstOrDefault(),
            UnreadCount = c.Messages.Count(m => m.SenderUserId != currentUserId && !m.IsRead)
        };

    /// <summary>Проекция <see cref="Message"/> → <see cref="GetMessageDto"/>.</summary>
    public static Expression<Func<Message, GetMessageDto>> MessageToDto =>
        m => new GetMessageDto
        {
            Id = m.Id,
            ChatId = m.ChatId,
            SenderUserId = m.SenderUserId,
            MessageText = m.MessageText,
            FileName = m.FileName,
            CreatedAt = m.CreatedAt,
            IsRead = m.IsRead
        };
}
