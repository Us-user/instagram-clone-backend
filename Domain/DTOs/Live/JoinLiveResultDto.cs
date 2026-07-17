namespace Domain.DTOs.Live;

/// <summary>
/// Результат присоединения зрителя к эфиру: <b>токен доступа с ролью Subscriber</b> и URL сервера
/// провайдера. Выдаётся только после серверных проверок доступа (аудитория/бан/блокировка).
/// </summary>
public class JoinLiveResultDto
{
    public int StreamId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string ServerUrl { get; set; } = string.Empty;
}
