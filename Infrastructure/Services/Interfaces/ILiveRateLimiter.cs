namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Троттлинг действий зрителя в эфире (требование качества §9.3): защита от спама сердечками и
/// комментариями. In-memory singleton (эфемерное состояние скользящих окон на пользователя+эфир).
/// </summary>
public interface ILiveRateLimiter
{
    /// <summary>Разрешено ли сейчас поставить «сердечко» (не чаще N в секунду на юзера в этом эфире).</summary>
    bool AllowLike(string userId, int streamId);

    /// <summary>Разрешено ли сейчас отправить комментарий (не чаще M за окно на юзера в этом эфире).</summary>
    bool AllowComment(string userId, int streamId);
}
