namespace Domain.DTOs.Live;

/// <summary>
/// Результат старта эфира: id созданного эфира, имя комнаты и <b>токен доступа с ролью Publisher</b>
/// (генерирует только бэкенд), а также URL сервера провайдера для подключения клиента к WebRTC.
/// </summary>
public class StartLiveResultDto
{
    public int StreamId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string ServerUrl { get; set; } = string.Empty;
}
