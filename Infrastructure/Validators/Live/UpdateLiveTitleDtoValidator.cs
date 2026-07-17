using Domain.DTOs.Live;
using FluentValidation;

namespace Infrastructure.Validators.Live;

/// <summary>Ограничения на смену заголовка эфира: не длиннее лимита (пустой — снять заголовок).</summary>
public class UpdateLiveTitleDtoValidator : AbstractValidator<UpdateLiveTitleDto>
{
    public UpdateLiveTitleDtoValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Слишком длинный заголовок эфира (максимум 200 символов).");
    }
}
