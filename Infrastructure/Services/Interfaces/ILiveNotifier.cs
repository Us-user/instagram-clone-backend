using Domain.DTOs.Live;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Абстракция real-time доставки событий эфира (§6). Реализация живёт в WebApi (SignalR <c>LiveHub</c>),
/// поэтому слой Infrastructure зависит только от абстракции. Групповые события адресуются группе
/// <c>live_{streamId}</c>; адресные — конкретному пользователю/подписчикам. <see cref="LiveStreamService"/>
/// вызывает эти методы после изменения состояния.
/// </summary>
public interface ILiveNotifier
{
    /// <summary>Зритель присоединился — всем в эфире.</summary>
    Task ViewerJoinedAsync(int streamId, LiveUserDto viewer);

    /// <summary>Зритель вышел — всем в эфире.</summary>
    Task ViewerLeftAsync(int streamId, string userId);

    /// <summary>Актуальное число зрителей — всем в эфире.</summary>
    Task ViewerCountAsync(int streamId, int count);

    /// <summary>Новый комментарий — всем в эфире.</summary>
    Task NewCommentAsync(int streamId, LiveCommentDto comment);

    /// <summary>Новое «сердечко» (для анимации) — всем в эфире.</summary>
    Task NewLikeAsync(int streamId, string userId);

    /// <summary>Комментарий закреплён — всем в эфире.</summary>
    Task CommentPinnedAsync(int streamId, int commentId);

    /// <summary>Комментарий удалён — всем в эфире.</summary>
    Task CommentDeletedAsync(int streamId, int commentId);

    /// <summary>Заявка в гости получена — только хосту.</summary>
    Task GuestRequestReceivedAsync(string hostUserId, LiveGuestRequestDto request);

    /// <summary>Заявка одобрена — только заявителю (Publisher-токен для публикации видео/аудио).</summary>
    Task GuestApprovedAsync(string userId, JoinLiveResultDto result);

    /// <summary>Заявка отклонена — только заявителю.</summary>
    Task GuestRequestDeclinedAsync(string userId, int requestId);

    /// <summary>Гость вышел в эфир (одобрен) — всем в эфире.</summary>
    Task GuestJoinedAsync(int streamId, LiveUserDto guest);

    /// <summary>Гостя убрали — всем в эфире.</summary>
    Task GuestLeftAsync(int streamId, string userId);

    /// <summary>Зритель забанен — всем в эфире (в т.ч. самому забаненному, чтобы отключиться).</summary>
    Task ViewerBannedAsync(int streamId, string bannedUserId);

    /// <summary>Эфир завершён (с итоговой статистикой) — всем в эфире.</summary>
    Task StreamEndedAsync(int streamId, LiveStreamEndedDto payload);

    /// <summary>Эфир начался — подписчикам хоста, попавшим в аудиторию.</summary>
    Task StreamStartedAsync(IReadOnlyCollection<string> subscriberIds, LiveStreamStartedDto payload);
}
