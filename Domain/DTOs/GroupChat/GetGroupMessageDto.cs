using Domain.Enums;

namespace Domain.DTOs.GroupChat;

/// <summary>Сообщение группы для чтения. У служебных (System) сообщений отправитель отсутствует.</summary>
public class GetGroupMessageDto
{
    public int Id { get; set; }
    public int GroupChatId { get; set; }

    public string? SenderUserId { get; set; }
    public string? SenderUserName { get; set; }
    public string? SenderImage { get; set; }

    public string? MessageText { get; set; }
    public string? FileName { get; set; }
    public MessageType MessageType { get; set; }

    public int? Duration { get; set; }
    public string? Waveform { get; set; }

    public int? ReplyToMessageId { get; set; }

    /// <summary>Краткая цитата исходного сообщения (если это ответ).</summary>
    public GroupMessageReplyDto? ReplyTo { get; set; }

    public bool IsForwarded { get; set; }
    public DateTime CreatedAt { get; set; }
}
