using Domain.DTOs.Account;
using FluentValidation;

namespace Infrastructure.Validators.Account;

/// <summary>Валидация регистрации: все поля обязательны, пароли совпадают, корректный email.</summary>
public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Имя пользователя обязательно.")
            .MinimumLength(3).WithMessage("Имя пользователя не короче 3 символов.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Полное имя обязательно.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен.")
            .EmailAddress().WithMessage("Некорректный формат email.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен.")
            .MinimumLength(6).WithMessage("Пароль не короче 6 символов.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Подтверждение пароля обязательно.")
            .Equal(x => x.Password).WithMessage("Пароли не совпадают.");
    }
}
