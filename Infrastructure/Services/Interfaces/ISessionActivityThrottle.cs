namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Троттлинг обновления <c>LastActivityAt</c> сессий, чтобы не писать в БД на каждом запросе.
/// Реализация — singleton in-memory (как presence/typing-трекеры).
/// </summary>
public interface ISessionActivityThrottle
{
    /// <summary>
    /// Возвращает <c>true</c> не чаще одного раза за окно троттлинга для данной сессии и в этом
    /// случае фиксирует момент — тогда вызывающий делает запись <c>LastActivityAt</c> в БД.
    /// </summary>
    bool ShouldPersist(Guid sessionId, DateTime utcNow);

    /// <summary>Забыть сессию (например, при отзыве), чтобы не удерживать память.</summary>
    void Forget(Guid sessionId);
}
