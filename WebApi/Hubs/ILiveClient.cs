using Domain.DTOs.Live;

namespace WebApi.Hubs;

/// <summary>
/// Строго типизированные методы, которые сервер вызывает у клиентов прямого эфира (§6). Групповые
/// события (группа <c>live_{streamId}</c>) получают все в эфире; адресные — конкретный пользователь
/// (хост/заявитель/подписчики).
/// </summary>
public interface ILiveClient
{
    /// <summary>Зритель присоединился к эфиру.</summary>
    Task ViewerJoined(LiveUserDto viewer);

    /// <summary>Зритель вышел из эфира.</summary>
    Task ViewerLeft(LiveUserRefDto viewer);

    /// <summary>Актуальное число зрителей.</summary>
    Task ViewerCount(LiveViewerCountDto count);

    /// <summary>Новый комментарий.</summary>
    Task NewComment(LiveCommentDto comment);

    /// <summary>Новое «сердечко» (для анимации).</summary>
    Task NewLike(LiveUserRefDto like);

    /// <summary>Комментарий закреплён.</summary>
    Task CommentPinned(LiveCommentRefDto comment);

    /// <summary>Комментарий удалён.</summary>
    Task CommentDeleted(LiveCommentRefDto comment);

    /// <summary>Получена заявка в гости — только хосту.</summary>
    Task GuestRequestReceived(LiveGuestRequestDto request);

    /// <summary>
    /// Заявка в гости одобрена — только заявителю. Несёт <b>Publisher-токен</b> и URL сервера, чтобы
    /// клиент подключился/переподключился как publisher и включил камеру/микрофон.
    /// </summary>
    Task GuestApproved(JoinLiveResultDto result);

    /// <summary>Заявка в гости отклонена — только заявителю.</summary>
    Task GuestRequestDeclined(LiveGuestRequestRefDto request);

    /// <summary>Гость вышел в эфир (одобрен).</summary>
    Task GuestJoined(LiveUserDto guest);

    /// <summary>Гость покинул эфир (убран/понижен).</summary>
    Task GuestLeft(LiveUserRefDto guest);

    /// <summary>Зритель забанен (убрать из списка / отключиться забаненному).</summary>
    Task ViewerBanned(LiveUserRefDto viewer);

    /// <summary>Эфир завершён (с итоговой статистикой).</summary>
    Task StreamEnded(LiveStreamEndedDto ended);

    /// <summary>Эфир начался — подписчикам хоста.</summary>
    Task StreamStarted(LiveStreamStartedDto started);
}
