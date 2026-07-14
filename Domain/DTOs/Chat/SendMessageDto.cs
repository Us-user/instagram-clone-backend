using Microsoft.AspNetCore.Http;

namespace Domain.DTOs.Chat;

/// <summary>Отправка сообщения (multipart/form-data). ChatId обязателен.</summary>
public class SendMessageDto
{
    public int ChatId { get; set; }
    public string? MessageText { get; set; }
    public IFormFile? File { get; set; }

    /// <summary>Необязательный ответ на сообщение того же чата (reply, §8).</summary>
    public int? ReplyToMessageId { get; set; }
}
