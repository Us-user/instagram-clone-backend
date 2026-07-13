using Domain.DTOs.Account;
using Domain.Responses;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Аутентификация и управление паролями: регистрация, вход, восстановление/смена пароля.
/// </summary>
public interface IAccountService
{
    /// <summary>Регистрирует пользователя и создаёт для него пустой профиль. Возвращает JWT.</summary>
    Task<Response<string>> RegisterAsync(RegisterDto dto);

    /// <summary>Проверяет учётные данные и возвращает JWT в <c>data</c>.</summary>
    Task<Response<string>> LoginAsync(LoginDto dto);

    /// <summary>Генерирует токен сброса пароля (в учебных целях возвращается в ответе).</summary>
    Task<Response<string>> ForgotPasswordAsync(string? email);

    /// <summary>Сбрасывает пароль по ранее выданному токену.</summary>
    Task<Response<string>> ResetPasswordAsync(string? token, string? email, string? password, string? confirmPassword);

    /// <summary>Меняет пароль текущего пользователя (id берётся из claims).</summary>
    Task<Response<string>> ChangePasswordAsync(string? oldPassword, string? password, string? confirmPassword);
}
