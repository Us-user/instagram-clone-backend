using Domain.DTOs.Message;
using Domain.Enums;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Кросс-контекстные операции над сообщениями (§8): реакции, пересылка и голосовые — одинаково для
/// личных (<c>context=Direct</c>) и групповых (<c>context=Group</c>) чатов. Результат рассылается в
/// реальном времени через SignalR-хабы <c>/chatHub</c> (Direct) и <c>/groupChatHub</c> (Group).
/// Id текущего юзера — из claims.
/// </summary>
[ApiController]
[Route("[controller]")]
public class MessageController : ControllerBase
{
    private readonly IMessageService _messageService;

    public MessageController(IMessageService messageService) => _messageService = messageService;

    /// <summary>Поставить/снять/заменить реакцию-эмодзи на сообщение (тумблер/замена) + real-time пуш.</summary>
    [HttpPost("react")]
    public async Task<IActionResult> React(
        [FromQuery] int? messageId, [FromQuery] MessageContext? context, [FromQuery] string? emoji)
    {
        var result = await _messageService.ReactAsync(messageId, context, emoji);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Переслать сообщение (копия содержимого) в целевой чат/группу с пометкой «Переслано».</summary>
    [HttpPost("forward")]
    public async Task<IActionResult> Forward(
        [FromQuery] int? messageId, [FromQuery] MessageContext? context,
        [FromQuery] int? targetChatId, [FromQuery] MessageContext? targetContext)
    {
        var result = await _messageService.ForwardAsync(messageId, context, targetChatId, targetContext);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Отправить голосовое сообщение (multipart/form-data: Context, ChatId, File, Duration, ReplyToMessageId).</summary>
    [HttpPost("send-voice")]
    public async Task<IActionResult> SendVoice([FromForm] SendVoiceDto dto)
    {
        var result = await _messageService.SendVoiceAsync(dto);
        return StatusCode(result.StatusCode, result);
    }
}
