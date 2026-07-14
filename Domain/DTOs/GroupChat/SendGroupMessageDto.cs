using Microsoft.AspNetCore.Http;

namespace Domain.DTOs.GroupChat;

/// <summary>
/// Отправка сообщения в группу (multipart/form-data). Идентификатор группы передаётся
/// отдельным query-параметром <c>groupId</c>. Должен присутствовать текст или файл.
/// </summary>
public class SendGroupMessageDto
{
    public string? MessageText { get; set; }
    public IFormFile? File { get; set; }

    /// <summary>Id сообщения этой же группы, на которое отвечают (reply, §8).</summary>
    public int? ReplyToMessageId { get; set; }
}
