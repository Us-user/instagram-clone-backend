using Domain.DTOs.Message;
using Domain.Enums;

namespace Domain.DTOs.Chat;

/// <summary>Сообщение личного чата для чтения. Поля §8 (тип/голос/reply/forward/реакции) — необязательные.</summary>
public class GetMessageDto
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public string SenderUserId { get; set; } = string.Empty;
    public string? MessageText { get; set; }
    public string? FileName { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }

    // ── §8: тип, голос, reply, forward, реакции ──
    public MessageType MessageType { get; set; }
    public int? Duration { get; set; }
    public string? Waveform { get; set; }

    public int? ReplyToMessageId { get; set; }

    /// <summary>Краткая цитата исходного сообщения (если это ответ).</summary>
    public MessageReplyDto? ReplyTo { get; set; }

    public bool IsForwarded { get; set; }

    /// <summary>Реакции на сообщение (кто и каким эмодзи отреагировал).</summary>
    public List<MessageReactionDto> Reactions { get; set; } = new();
}
