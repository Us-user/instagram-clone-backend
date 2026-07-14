using Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Domain.DTOs.Message;

/// <summary>
/// Отправка голосового сообщения (§8, multipart/form-data) в личный или групповой чат.
/// <see cref="ChatId"/> — Id чата (Direct) либо группы (Group). Волна генерируется на сервере.
/// </summary>
public class SendVoiceDto
{
    /// <summary>Куда шлём: личный чат или группа.</summary>
    public MessageContext Context { get; set; }

    /// <summary>Id личного чата или группы (в зависимости от <see cref="Context"/>).</summary>
    public int ChatId { get; set; }

    /// <summary>Аудиофайл голосового.</summary>
    public IFormFile? File { get; set; }

    /// <summary>Длительность записи в секундах.</summary>
    public int Duration { get; set; }

    /// <summary>Необязательный ответ на сообщение того же чата/группы.</summary>
    public int? ReplyToMessageId { get; set; }
}
