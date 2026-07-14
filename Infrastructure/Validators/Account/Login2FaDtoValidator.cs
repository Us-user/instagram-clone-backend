using Domain.DTOs.Account;
using FluentValidation;

namespace Infrastructure.Validators.Account;

/// <summary>Валидация входа со вторым фактором: токен, код и метод обязательны (§11).</summary>
public class Login2FaDtoValidator : AbstractValidator<Login2FaDto>
{
    public Login2FaDtoValidator()
    {
        RuleFor(x => x.TwoFactorToken)
            .NotEmpty().WithMessage("Токен двухфакторной сессии обязателен.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Код подтверждения обязателен.");

        RuleFor(x => x.Method)
            .NotEmpty().WithMessage("Метод подтверждения обязателен (Totp/Email/Backup).");
    }
}
