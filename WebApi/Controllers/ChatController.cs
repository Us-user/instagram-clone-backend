using Domain.DTOs.Chat;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Чаты и сообщения. Пути/методы/параметры воспроизводят контракт дословно (включая опечатку
/// <c>massageId</c> в delete-message). Отправка сообщения дополнительно рассылается в реальном
/// времени через SignalR-хаб <c>/chatHub</c>. Id текущего юзера — из claims.
/// </summary>
[ApiController]
[Route("[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService) => _chatService = chatService;

    /// <summary>Чаты текущего юзера: собеседник + последнее сообщение + непрочитанные.</summary>
    [HttpGet("get-chats")]
    public async Task<IActionResult> GetChats()
    {
        var result = await _chatService.GetChatsAsync();
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Чат со всей перепиской; входящие сообщения помечаются прочитанными.</summary>
    [HttpGet("get-chat-by-id")]
    public async Task<IActionResult> GetChatById([FromQuery] int? chatId)
    {
        var result = await _chatService.GetChatByIdAsync(chatId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Создать чат с получателем или вернуть существующий.</summary>
    [HttpPost("create-chat")]
    public async Task<IActionResult> CreateChat([FromQuery] string? receiverUserId)
    {
        var result = await _chatService.CreateChatAsync(receiverUserId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Отправить сообщение (multipart/form-data: ChatId, MessageText, File) + рассылка через SignalR.</summary>
    [HttpPut("send-message")]
    public async Task<IActionResult> SendMessage([FromForm] SendMessageDto dto)
    {
        var result = await _chatService.SendMessageAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Удалить сообщение — только отправитель (параметр <c>massageId</c> — опечатка контракта).</summary>
    [HttpDelete("delete-message")]
    public async Task<IActionResult> DeleteMessage([FromQuery] int? massageId)
    {
        var result = await _chatService.DeleteMessageAsync(massageId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Удалить чат со всеми сообщениями — только участник.</summary>
    [HttpDelete("delete-chat")]
    public async Task<IActionResult> DeleteChat([FromQuery] int? chatId)
    {
        var result = await _chatService.DeleteChatAsync(chatId);
        return StatusCode(result.StatusCode, result);
    }
}
