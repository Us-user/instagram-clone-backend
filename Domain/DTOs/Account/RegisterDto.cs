namespace Domain.DTOs.Account;

/// <summary>Регистрация. Все поля обязательны (валидация — FluentValidation).</summary>
public class RegisterDto
{
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
