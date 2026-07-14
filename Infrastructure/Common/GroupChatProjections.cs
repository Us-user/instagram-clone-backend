using System.Linq.Expressions;
using Domain.DTOs.GroupChat;
using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Common;

/// <summary>
/// Переиспользуемые проекции группового чата в DTO (§7). Написаны выражениями, чтобы EF перевёл
/// их в SQL (использовать в <c>Select</c> над <see cref="IQueryable{T}"/>). Список групп считает
/// последнее сообщение и непрочитанные относительно членства текущего пользователя.
/// </summary>
public static class GroupChatProjections
{
    /// <summary>
    /// Проекция членства <see cref="GroupChatMember"/> текущего юзера → <see cref="GetGroupChatDto"/>
    /// для списка групп: инфо группы, последнее сообщение и непрочитанные (не свои, не служебные,
    /// после <see cref="GroupChatMember.LastReadAt"/>).
    /// </summary>
    public static Expression<Func<GroupChatMember, GetGroupChatDto>> ToListDto(string currentUserId) =>
        mem => new GetGroupChatDto
        {
            Id = mem.GroupChat!.Id,
            Name = mem.GroupChat.Name,
            Avatar = mem.GroupChat.Avatar,
            MembersCount = mem.GroupChat.Members.Count,
            LastMessage = mem.GroupChat.Messages
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => m.MessageText)
                .FirstOrDefault(),
            LastMessageDate = mem.GroupChat.Messages
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => (DateTime?)m.CreatedAt)
                .FirstOrDefault(),
            UnreadCount = mem.GroupChat.Messages.Count(m =>
                m.SenderUserId != currentUserId &&
                m.MessageType != MessageType.System &&
                (mem.LastReadAt == null || m.CreatedAt > mem.LastReadAt))
        };

    /// <summary>
    /// Проекция <see cref="GroupChatMember"/> → <see cref="GroupMemberDto"/> (участник + роль).
    /// </summary>
    public static Expression<Func<GroupChatMember, GroupMemberDto>> MemberToDto =>
        m => new GroupMemberDto
        {
            UserId = m.UserId,
            UserName = m.User!.UserName ?? string.Empty,
            UserImage = m.User.Avatar,
            IsVerified = m.User.IsVerified,
            Role = m.Role,
            JoinedAt = m.JoinedAt
        };

    /// <summary>
    /// Проекция <see cref="GroupMessage"/> → <see cref="GetGroupMessageDto"/> с данными отправителя
    /// (null для служебных) и краткой цитатой процитированного сообщения (reply).
    /// </summary>
    public static Expression<Func<GroupMessage, GetGroupMessageDto>> MessageToDto =>
        m => new GetGroupMessageDto
        {
            Id = m.Id,
            GroupChatId = m.GroupChatId,
            SenderUserId = m.SenderUserId,
            SenderUserName = m.Sender != null ? m.Sender.UserName : null,
            SenderImage = m.Sender != null ? m.Sender.Avatar : null,
            MessageText = m.MessageText,
            FileName = m.FileName,
            MessageType = m.MessageType,
            Duration = m.Duration,
            Waveform = m.Waveform,
            ReplyToMessageId = m.ReplyToMessageId,
            ReplyTo = m.ReplyToMessage == null ? null : new GroupMessageReplyDto
            {
                Id = m.ReplyToMessage.Id,
                SenderUserName = m.ReplyToMessage.Sender != null ? m.ReplyToMessage.Sender.UserName : null,
                MessageText = m.ReplyToMessage.MessageText,
                MessageType = m.ReplyToMessage.MessageType
            },
            IsForwarded = m.IsForwarded,
            CreatedAt = m.CreatedAt
        };
}
