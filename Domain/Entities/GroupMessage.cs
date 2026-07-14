using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Сообщение группового чата (§7). Может быть текстом, вложением, голосовым (Phase 16) или
/// служебным (<see cref="MessageType.System"/>, <see cref="SenderUserId"/> = null). Поля
/// <see cref="Duration"/>/<see cref="Waveform"/>/<see cref="ReplyToMessageId"/>/<see cref="IsForwarded"/>
/// заведены под §8 (голосовые/reply/forward); в Phase 15 используются текст, вложение и reply.
/// </summary>
public class GroupMessage
{
    public int Id { get; set; }
    public int GroupChatId { get; set; }

    /// <summary>Отправитель. null для служебных (System) сообщений.</summary>
    public string? SenderUserId { get; set; }

    public string? MessageText { get; set; }

    /// <summary>Имя прикреплённого файла в wwwroot/images (nullable).</summary>
    public string? FileName { get; set; }

    public MessageType MessageType { get; set; }

    /// <summary>Длительность голосового в секундах (Phase 16).</summary>
    public int? Duration { get; set; }

    /// <summary>JSON-массив амплитуд волны голосового (Phase 16).</summary>
    public string? Waveform { get; set; }

    /// <summary>Id сообщения, на которое отвечают (reply).</summary>
    public int? ReplyToMessageId { get; set; }

    /// <summary>Переслано ли сообщение (forward, Phase 16).</summary>
    public bool IsForwarded { get; set; }

    public DateTime CreatedAt { get; set; }

    public GroupChat? GroupChat { get; set; }
    public User? Sender { get; set; }
    public GroupMessage? ReplyToMessage { get; set; }
}
