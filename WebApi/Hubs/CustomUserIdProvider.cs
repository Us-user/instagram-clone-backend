using Infrastructure.Constants;
using Microsoft.AspNetCore.SignalR;

namespace WebApi.Hubs;

/// <summary>
/// Сопоставляет SignalR-подключение с userId из кастомного JWT-claim
/// (<see cref="CustomClaims.UserId"/>), чтобы адресовать сообщения через <c>Clients.Users(...)</c>.
/// Стандартный <c>DefaultUserIdProvider</c> читает <c>ClaimTypes.NameIdentifier</c>, которого
/// в нашем токене нет.
/// </summary>
public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst(CustomClaims.UserId)?.Value;
}
