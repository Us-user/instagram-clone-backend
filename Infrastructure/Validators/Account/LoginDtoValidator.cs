using Domain.DTOs.Account;
using FluentValidation;

namespace Infrastructure.Validators.Account;

/// <summary>Валидация входа: имя пользователя и пароль обязательны.</summary>
public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Имя пользователя обязательно.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен.");
    }
}
