namespace Domain.Entities;

/// <summary>
/// Ответ на сторис (§9). Ответ на активную чужую сторис уходит в директ автора: создаётся личное
/// сообщение (<see cref="MessageId"/>) в чате 1:1, а эта запись связывает его с исходной сторис
/// (<see cref="StoryId"/>) для превью. Автор сторис получает уведомление <see cref="Enums.NotificationType.StoryReply"/>.
/// </summary>
public class StoryReply
{
    public int Id { get; set; }

    /// <summary>Сторис, на которую ответили (FK).</summary>
    public int StoryId { get; set; }

    /// <summary>Кто ответил (FK на AspNetUsers).</summary>
    public string FromUserId { get; set; } = string.Empty;

    /// <summary>Созданное личное сообщение с текстом ответа (FK).</summary>
    public int MessageId { get; set; }

    public DateTime CreatedAt { get; set; }

    public Story? Story { get; set; }
    public User? FromUser { get; set; }
    public Message? Message { get; set; }
}
