using Domain.DTOs.Account;
using FluentValidation;

namespace Infrastructure.Validators.Account;

/// <summary>Валидация запроса email-кода: токен двухфакторной сессии обязателен (§11).</summary>
public class Send2FaEmailDtoValidator : AbstractValidator<Send2FaEmailDto>
{
    public Send2FaEmailDtoValidator()
    {
        RuleFor(x => x.TwoFactorToken)
            .NotEmpty().WithMessage("Токен двухфакторной сессии обязателен.");
    }
}
