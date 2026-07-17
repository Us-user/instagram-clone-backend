namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Эфемерное сопоставление SignalR-подключений с (эфир, зритель) для обработки обрыва связи (§6).
/// In-memory singleton (как presence/typing-трекеры): при потере соединения зритель считается вышедшим,
/// но с грейс-периодом на переподключение — <see cref="IsUserWatching"/> позволяет проверить, осталось ли
/// у него другое живое соединение к эфиру, прежде чем фиксировать выход.
/// </summary>
public interface ILiveConnectionTracker
{
    /// <summary>Регистрирует подключение зрителя к эфиру (вызывается из <c>JoinStream</c>-метода хаба).</summary>
    void Add(string connectionId, int streamId, string userId);

    /// <summary>
    /// Снимает подключение и возвращает его привязку (эфир, зритель), если она была. Вызывается при
    /// обрыве связи и при явном выходе из группы.
    /// </summary>
    (int StreamId, string UserId)? Remove(string connectionId);

    /// <summary>Есть ли у пользователя ещё хотя бы одно живое соединение к этому эфиру.</summary>
    bool IsUserWatching(int streamId, string userId);
}
