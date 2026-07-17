using System.Text;
using Domain.DTOs.Live;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Прямые эфиры (Live Streaming). Видео идёт напрямую клиент↔LiveKit; бэкенд управляет всей логикой:
/// старт/стоп эфира и выдача токенов доступа, просмотр, гости, комментарии/сердечки, модерация,
/// статистика и вебхуки провайдера. Все эндпоинты авторизованы, кроме <c>webhook</c>. Id текущего
/// пользователя — из JWT claims.
/// </summary>
[ApiController]
[Route("Live")]
public class LiveStreamController : ControllerBase
{
    private readonly ILiveStreamService _service;

    public LiveStreamController(ILiveStreamService service) => _service = service;

    // ── Управление эфиром (хост) ────────────────────────────────────────────

    /// <summary>Начать эфир. Возвращает id, имя комнаты и Publisher-токен доступа.</summary>
    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] StartLiveDto dto)
    {
        var result = await _service.StartAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Завершить эфир (только хост). Возвращает итоговую статистику.</summary>
    [HttpPost("end")]
    public async Task<IActionResult> End([FromQuery] int? streamId)
    {
        var result = await _service.EndAsync(streamId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Изменить заголовок эфира (только хост).</summary>
    [HttpPut("update-title")]
    public async Task<IActionResult> UpdateTitle([FromQuery] int? streamId, [FromBody] UpdateLiveTitleDto dto)
    {
        var result = await _service.UpdateTitleAsync(streamId, dto);
        return StatusCode(result.StatusCode, result);
    }

    // ── Просмотр (зритель) ────────────────────────────────────────────────────

    /// <summary>Активные эфиры тех, на кого подписан (с учётом приватности/блокировок/close friends).</summary>
    [HttpGet("get-active")]
    public async Task<IActionResult> GetActive([FromQuery] int? pageNumber, [FromQuery] int? pageSize)
    {
        var result = await _service.GetActiveAsync(pageNumber, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Информация об эфире (хост, заголовок, счётчики, статус, гости).</summary>
    [HttpGet("get-stream-by-id")]
    public async Task<IActionResult> GetStreamById([FromQuery] int? streamId)
    {
        var result = await _service.GetByIdAsync(streamId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Присоединиться к эфиру зрителем. Возвращает Subscriber-токен доступа.</summary>
    [HttpPost("join")]
    public async Task<IActionResult> Join([FromQuery] int? streamId)
    {
        var result = await _service.JoinAsync(streamId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Выйти из эфира.</summary>
    [HttpPost("leave")]
    public async Task<IActionResult> Leave([FromQuery] int? streamId)
    {
        var result = await _service.LeaveAsync(streamId);
        return StatusCode(result.StatusCode, result);
    }

    // ── Гости ──────────────────────────────────────────────────────────────────

    /// <summary>Подать заявку на выход в эфир гостем.</summary>
    [HttpPost("request-guest")]
    public async Task<IActionResult> RequestGuest([FromQuery] int? streamId)
    {
        var result = await _service.RequestGuestAsync(streamId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Отменить свою заявку в гости.</summary>
    [HttpDelete("cancel-guest-request")]
    public async Task<IActionResult> CancelGuestRequest([FromQuery] int? streamId)
    {
        var result = await _service.CancelGuestRequestAsync(streamId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Список ожидающих заявок в гости (только хост).</summary>
    [HttpGet("get-guest-requests")]
    public async Task<IActionResult> GetGuestRequests([FromQuery] int? streamId)
    {
        var result = await _service.GetGuestRequestsAsync(streamId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Одобрить заявку в гости (только хост, с проверкой лимита гостей).</summary>
    [HttpPost("approve-guest")]
    public async Task<IActionResult> ApproveGuest([FromQuery] int? requestId)
    {
        var result = await _service.ApproveGuestAsync(requestId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Отклонить заявку в гости (только хост).</summary>
    [HttpPost("decline-guest")]
    public async Task<IActionResult> DeclineGuest([FromQuery] int? requestId)
    {
        var result = await _service.DeclineGuestAsync(requestId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Убрать гостя из эфира (только хост).</summary>
    [HttpPost("remove-guest")]
    public async Task<IActionResult> RemoveGuest([FromQuery] int? streamId, [FromQuery] string? userId)
    {
        var result = await _service.RemoveGuestAsync(streamId, userId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Текущие гости эфира.</summary>
    [HttpGet("get-active-guests")]
    public async Task<IActionResult> GetActiveGuests([FromQuery] int? streamId)
    {
        var result = await _service.GetActiveGuestsAsync(streamId);
        return StatusCode(result.StatusCode, result);
    }

    // ── Комментарии и реакции ───────────────────────────────────────────────────

    /// <summary>Добавить комментарий в эфир.</summary>
    [HttpPost("add-comment")]
    public async Task<IActionResult> AddComment([FromQuery] int? streamId, [FromBody] AddLiveCommentDto dto)
    {
        var result = await _service.AddCommentAsync(streamId, dto);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Удалить комментарий (автор коммента или хост).</summary>
    [HttpDelete("delete-comment")]
    public async Task<IActionResult> DeleteComment([FromQuery] int? commentId)
    {
        var result = await _service.DeleteCommentAsync(commentId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Закрепить комментарий (только хост; одновременно закреплён один).</summary>
    [HttpPost("pin-comment")]
    public async Task<IActionResult> PinComment([FromQuery] int? commentId)
    {
        var result = await _service.PinCommentAsync(commentId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>История комментариев эфира (догрузка / после эфира).</summary>
    [HttpGet("get-comments")]
    public async Task<IActionResult> GetComments(
        [FromQuery] int? streamId, [FromQuery] int? pageNumber, [FromQuery] int? pageSize)
    {
        var result = await _service.GetCommentsAsync(streamId, pageNumber, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Поставить «сердечко» (многократно, с троттлингом).</summary>
    [HttpPost("send-like")]
    public async Task<IActionResult> SendLike([FromQuery] int? streamId)
    {
        var result = await _service.SendLikeAsync(streamId);
        return StatusCode(result.StatusCode, result);
    }

    // ── Модерация ────────────────────────────────────────────────────────────────

    /// <summary>Кикнуть зрителя и запретить возвращаться (только хост).</summary>
    [HttpPost("ban-viewer")]
    public async Task<IActionResult> BanViewer([FromQuery] int? streamId, [FromQuery] string? userId)
    {
        var result = await _service.BanViewerAsync(streamId, userId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Снять бан зрителя (только хост).</summary>
    [HttpDelete("unban-viewer")]
    public async Task<IActionResult> UnbanViewer([FromQuery] int? streamId, [FromQuery] string? userId)
    {
        var result = await _service.UnbanViewerAsync(streamId, userId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Кто сейчас смотрит эфир (только хост).</summary>
    [HttpGet("get-viewers")]
    public async Task<IActionResult> GetViewers(
        [FromQuery] int? streamId, [FromQuery] int? pageNumber, [FromQuery] int? pageSize)
    {
        var result = await _service.GetViewersAsync(streamId, pageNumber, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    // ── Статистика / после эфира ───────────────────────────────────────────────────

    /// <summary>Статистика эфира (только хост): live-режим и итоги после эфира.</summary>
    [HttpGet("get-stats")]
    public async Task<IActionResult> GetStats([FromQuery] int? streamId)
    {
        var result = await _service.GetStatsAsync(streamId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>История моих эфиров со статистикой.</summary>
    [HttpGet("get-my-streams")]
    public async Task<IActionResult> GetMyStreams([FromQuery] int? pageNumber, [FromQuery] int? pageSize)
    {
        var result = await _service.GetMyStreamsAsync(pageNumber, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Сохранить завершённый эфир в сторис (если есть запись).</summary>
    [HttpPost("save-to-story")]
    public async Task<IActionResult> SaveToStory([FromQuery] int? streamId)
    {
        var result = await _service.SaveToStoryAsync(streamId);
        return StatusCode(result.StatusCode, result);
    }

    // ── Вебхуки провайдера ────────────────────────────────────────────────────────

    /// <summary>
    /// Вебхук LiveKit (анонимный, но с обязательной проверкой подписи в сервисе). Синхронизирует
    /// состояние эфира (напр. автозавершение по <c>room_finished</c>). Тело читается как есть — подпись
    /// сверяется по «сырому» телу.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        var auth = Request.Headers.Authorization.ToString();

        var result = await _service.HandleWebhookAsync(body, auth);
        return StatusCode(result.StatusCode, result);
    }
}
