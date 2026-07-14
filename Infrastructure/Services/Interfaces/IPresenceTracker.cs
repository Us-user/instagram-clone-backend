namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Реестр присутствия (§1): кто сейчас онлайн, по активным SignalR-соединениям всех хабов.
/// Живёт в памяти как singleton (без БД) — один пользователь может держать несколько
/// подключений (вкладки/устройства/разные хабы), поэтому считаем соединения, а не факт «зашёл».
/// Онлайн = хотя бы одно активное соединение.
/// </summary>
public interface IPresenceTracker
{
    /// <summary>
    /// Регистрирует новое соединение пользователя. Возвращает <c>true</c>, если это первое
    /// соединение (пользователь перешёл из офлайна в онлайн) — повод разослать presence.
    /// </summary>
    bool Connect(string userId, string connectionId);

    /// <summary>
    /// Снимает соединение пользователя. Возвращает <c>true</c>, если оно было последним
    /// (пользователь ушёл в офлайн) — повод обновить <c>LastSeen</c> и разослать presence.
    /// </summary>
    bool Disconnect(string userId, string connectionId);

    /// <summary>Онлайн ли пользователь (есть ли хотя бы одно активное соединение).</summary>
    bool IsOnline(string userId);
}
