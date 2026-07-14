namespace Domain.DTOs.Presence;

/// <summary>
/// Онлайн-статус пользователя (§1). Сервер отдаёт <see cref="IsOnline"/> + <see cref="LastSeen"/>;
/// человекочитаемую строку («только что», «N минут назад», «вчера», дата) формирует клиент.
/// Видимость взаимная: если запрашивающий скрыл свой статус — все статусы возвращаются
/// скрытыми (<see cref="IsOnline"/>=false, <see cref="LastSeen"/>=null), и его статус скрыт для всех.
/// </summary>
public class UserPresenceDto
{
    public string UserId { get; set; } = string.Empty;

    /// <summary>Онлайн ли сейчас (есть активное real-time соединение).</summary>
    public bool IsOnline { get; set; }

    /// <summary>Когда был онлайн в последний раз; <c>null</c>, если онлайн сейчас или статус скрыт.</summary>
    public DateTime? LastSeen { get; set; }
}
