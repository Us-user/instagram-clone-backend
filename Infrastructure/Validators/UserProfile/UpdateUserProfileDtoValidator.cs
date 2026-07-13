using Domain.DTOs.UserProfile;
using FluentValidation;

namespace Infrastructure.Validators.UserProfile;

/// <summary>Валидация обновления профиля: корректный пол, разумная длина «о себе».</summary>
public class UpdateUserProfileDtoValidator : AbstractValidator<UpdateUserProfileDto>
{
    public UpdateUserProfileDtoValidator()
    {
        RuleFor(x => x.Gender).IsInEnum().WithMessage("Некорректное значение пола.");
        RuleFor(x => x.About)
            .MaximumLength(500).WithMessage("«О себе» не длиннее 500 символов.");
    }
}
