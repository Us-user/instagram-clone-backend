using Domain.DTOs.Live;
using FluentValidation;

namespace Infrastructure.Validators.Live;

/// <summary>Ограничения на старт эфира: заголовок — необязательный, но не длиннее лимита.</summary>
public class StartLiveDtoValidator : AbstractValidator<StartLiveDto>
{
    public StartLiveDtoValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Слишком длинный заголовок эфира (максимум 200 символов).");
    }
}
