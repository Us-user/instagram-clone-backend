using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Сообщение личного чата. Может содержать текст и/или файл, быть голосовым
/// (<see cref="MessageType.Voice"/>), ответом (reply) или пересланным (forward) — §8.
/// </summary>
public class Message
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public string SenderUserId { get; set; } = string.Empty;
    public string? MessageText { get; set; }

    /// <summary>Имя прикреплённого файла в wwwroot/images (или wwwroot/voice для голосовых), nullable.</summary>
    public string? FileName { get; set; }

    /// <summary>Тип сообщения (§8). Для личных используется без <see cref="MessageType.System"/>.</summary>
    public MessageType MessageType { get; set; }

    /// <summary>Длительность голосового в секундах (§8).</summary>
    public int? Duration { get; set; }

    /// <summary>JSON-массив нормализованных амплитуд волны голосового (§8).</summary>
    public string? Waveform { get; set; }

    /// <summary>Id сообщения, на которое отвечают (reply).</summary>
    public int? ReplyToMessageId { get; set; }

    /// <summary>Переслано ли сообщение (forward) — §8.</summary>
    public bool IsForwarded { get; set; }

    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }

    public Chat? Chat { get; set; }
    public User? Sender { get; set; }
    public Message? ReplyToMessage { get; set; }
}
