namespace Domain.Entities;

/// <summary>Сообщение в чате. Может содержать текст и/или файл.</summary>
public class Message
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public string SenderUserId { get; set; } = string.Empty;
    public string? MessageText { get; set; }

    /// <summary>Имя прикреплённого файла в wwwroot/images (nullable).</summary>
    public string? FileName { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }

    public Chat? Chat { get; set; }
    public User? Sender { get; set; }
}
