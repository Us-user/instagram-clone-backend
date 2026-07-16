using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Активная сессия пользователя — привязка выданного refresh-токена к устройству (модуль
/// «активные сеансы + refresh»). Access-токен (JWT, 15 мин) несёт <c>sessionId</c> этой записи;
/// refresh-токен (30 дней) хранится только хэшем (<see cref="RefreshTokenHash"/>), в открытом виде
/// отдаётся клиенту один раз при выдаче. Отзыв (<see cref="IsRevoked"/>) проверяется на каждом
/// авторизованном запросе — это делает завершение сеанса мгновенным, а не через 15 минут.
/// </summary>
public class UserSession
{
    public Guid Id { get; set; }

    /// <summary>Владелец сессии (FK на AspNetUsers).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>SHA-256 (hex) текущего refresh-токена. Сам токен в БД не хранится.</summary>
    public string RefreshTokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Хэш предыдущего (уже ротированного) refresh-токена. Нужен для reuse-detection: предъявление
    /// старого токена после ротации трактуется как компрометация — все сессии юзера отзываются.
    /// </summary>
    public string? PreviousRefreshTokenHash { get; set; }

    /// <summary>Человекочитаемое имя устройства, напр. «Chrome на Windows».</summary>
    public string? DeviceName { get; set; }

    public DeviceType DeviceType { get; set; }

    /// <summary>Браузер (семейство), напр. «Chrome». Null, если не определён.</summary>
    public string? Browser { get; set; }

    /// <summary>Операционная система, напр. «Windows». Null, если не определена.</summary>
    public string? OS { get; set; }

    /// <summary>IP-адрес входа (с учётом X-Forwarded-For за прокси).</summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>Примерный город/страна по IP (может быть null — геолокация не обязательна).</summary>
    public string? Location { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Момент последней активности (обновляется троттлингом, не чаще раза в N минут).</summary>
    public DateTime LastActivityAt { get; set; }

    /// <summary>Срок жизни сессии = срок жизни refresh-токена (30 дней от последней ротации).</summary>
    public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }

    public User? User { get; set; }
}
