using Domain.DTOs.Live;
using Domain.Responses;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Бизнес-логика прямых эфиров: управление эфиром (старт/стоп/заголовок), просмотр, гости, комментарии
/// и реакции, модерация, статистика, вебхуки провайдера и обработка обрыва связи. Все проверки доступа
/// (аудитория, блокировки, баны, лимит гостей) — на сервере, до выдачи токена. Id текущего юзера — из claims.
/// </summary>
public interface ILiveStreamService
{
    // ── Управление эфиром (хост) ────────────────────────────────────────────
    Task<Response<StartLiveResultDto>> StartAsync(StartLiveDto dto);
    Task<Response<LiveStatsDto>> EndAsync(int? streamId);
    Task<Response<string>> UpdateTitleAsync(int? streamId, UpdateLiveTitleDto dto);

    // ── Просмотр (зритель) ──────────────────────────────────────────────────
    Task<PagedResponse<List<LiveStreamDto>>> GetActiveAsync(int? pageNumber, int? pageSize);
    Task<Response<LiveStreamDto>> GetByIdAsync(int? streamId);
    Task<Response<JoinLiveResultDto>> JoinAsync(int? streamId);
    Task<Response<bool>> LeaveAsync(int? streamId);

    // ── Гости ───────────────────────────────────────────────────────────────
    Task<Response<LiveGuestRequestDto>> RequestGuestAsync(int? streamId);
    Task<Response<bool>> CancelGuestRequestAsync(int? streamId);
    Task<Response<List<LiveGuestRequestDto>>> GetGuestRequestsAsync(int? streamId);
    Task<Response<bool>> ApproveGuestAsync(int? requestId);
    Task<Response<bool>> DeclineGuestAsync(int? requestId);
    Task<Response<bool>> RemoveGuestAsync(int? streamId, string? userId);
    Task<Response<List<LiveUserDto>>> GetActiveGuestsAsync(int? streamId);

    // ── Комментарии и реакции ────────────────────────────────────────────────
    Task<Response<LiveCommentDto>> AddCommentAsync(int? streamId, AddLiveCommentDto dto);
    Task<Response<bool>> DeleteCommentAsync(int? commentId);
    Task<Response<bool>> PinCommentAsync(int? commentId);
    Task<PagedResponse<List<LiveCommentDto>>> GetCommentsAsync(int? streamId, int? pageNumber, int? pageSize);

    /// <summary>Ставит «сердечко». Возвращает <c>true</c>, если засчитано, <c>false</c> — если сработал троттлинг.</summary>
    Task<Response<bool>> SendLikeAsync(int? streamId);

    // ── Модерация ─────────────────────────────────────────────────────────────
    Task<Response<bool>> BanViewerAsync(int? streamId, string? userId);
    Task<Response<bool>> UnbanViewerAsync(int? streamId, string? userId);
    Task<PagedResponse<List<LiveViewerDto>>> GetViewersAsync(int? streamId, int? pageNumber, int? pageSize);

    // ── Статистика / после эфира ──────────────────────────────────────────────
    Task<Response<LiveStatsDto>> GetStatsAsync(int? streamId);
    Task<PagedResponse<List<LiveStreamDto>>> GetMyStreamsAsync(int? pageNumber, int? pageSize);
    Task<Response<string>> SaveToStoryAsync(int? streamId);

    // ── Вебхуки провайдера / real-time инфраструктура ─────────────────────────
    /// <summary>Обрабатывает вебхук провайдера (проверка подписи + идемпотентная синхронизация состояния).</summary>
    Task<Response<string>> HandleWebhookAsync(string rawBody, string? authHeader);

    /// <summary>Фиксирует выход зрителя при подтверждённом обрыве связи (после грейс-периода в хабе).</summary>
    Task HandleViewerDisconnectAsync(int streamId, string userId);

    /// <summary>Автозавершение «висящих» эфиров: без активности дольше порога или превысивших макс. длительность.</summary>
    Task AutoEndInactiveStreamsAsync(TimeSpan inactivity, TimeSpan maxDuration);
}
