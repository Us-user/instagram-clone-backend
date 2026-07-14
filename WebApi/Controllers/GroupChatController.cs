using Domain.DTOs.GroupChat;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Групповые чаты (§7) — отдельная ветка от личных 1:1. Роли: Admin управляет составом/инфо и
/// назначает админов; Member пишет и выходит. Изменения состава/инфо фиксируются служебными
/// сообщениями. Отправка рассылается участникам через SignalR-хаб <c>/groupChatHub</c>.
/// Id текущего юзера — из claims.
/// </summary>
[ApiController]
[Route("[controller]")]
public class GroupChatController : ControllerBase
{
    private readonly IGroupChatService _groupChatService;

    public GroupChatController(IGroupChatService groupChatService) => _groupChatService = groupChatService;

    /// <summary>Создать группу: создатель — Admin, указанные пользователи — Member.</summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateGroupChatDto dto)
    {
        var result = await _groupChatService.CreateAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Группы текущего юзера: последнее сообщение + непрочитанные.</summary>
    [HttpGet("get-my-groups")]
    public async Task<IActionResult> GetMyGroups([FromQuery] int? pageNumber, [FromQuery] int? pageSize)
    {
        var result = await _groupChatService.GetMyGroupsAsync(pageNumber, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Карточка группы: инфо + участники + сообщения; входящие помечаются прочитанными.</summary>
    [HttpGet("get-group-by-id")]
    public async Task<IActionResult> GetGroupById([FromQuery] int? groupId)
    {
        var result = await _groupChatService.GetGroupByIdAsync(groupId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Добавить участника — только Admin.</summary>
    [HttpPost("add-member")]
    public async Task<IActionResult> AddMember([FromQuery] int? groupId, [FromQuery] string? userId)
    {
        var result = await _groupChatService.AddMemberAsync(groupId, userId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Удалить участника — только Admin.</summary>
    [HttpDelete("remove-member")]
    public async Task<IActionResult> RemoveMember([FromQuery] int? groupId, [FromQuery] string? userId)
    {
        var result = await _groupChatService.RemoveMemberAsync(groupId, userId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Назначить участника админом — только Admin.</summary>
    [HttpPost("promote-admin")]
    public async Task<IActionResult> PromoteAdmin([FromQuery] int? groupId, [FromQuery] string? userId)
    {
        var result = await _groupChatService.PromoteAdminAsync(groupId, userId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Выйти из группы самому.</summary>
    [HttpPost("leave")]
    public async Task<IActionResult> Leave([FromQuery] int? groupId)
    {
        var result = await _groupChatService.LeaveAsync(groupId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Обновить название/аватар группы (multipart/form-data: Name, Avatar) — только Admin.</summary>
    [HttpPut("update-info")]
    public async Task<IActionResult> UpdateInfo([FromQuery] int? groupId, [FromForm] UpdateGroupInfoDto dto)
    {
        var result = await _groupChatService.UpdateInfoAsync(groupId, dto);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Отправить сообщение (multipart/form-data: MessageText, File, ReplyToMessageId) + рассылка через SignalR.</summary>
    [HttpPut("send-message")]
    public async Task<IActionResult> SendMessage([FromQuery] int? groupId, [FromForm] SendGroupMessageDto dto)
    {
        var result = await _groupChatService.SendMessageAsync(groupId, dto);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Удалить сообщение — автор сообщения или админ группы.</summary>
    [HttpDelete("delete-message")]
    public async Task<IActionResult> DeleteMessage([FromQuery] int? messageId)
    {
        var result = await _groupChatService.DeleteMessageAsync(messageId);
        return StatusCode(result.StatusCode, result);
    }
}
