using Domain.DTOs.Story;
using FluentValidation;

namespace Infrastructure.Validators.Story;

/// <summary>Ограничения на ответ на сторис (§9): непустой текст разумной длины.</summary>
public class StoryReplyRequestDtoValidator : AbstractValidator<StoryReplyRequestDto>
{
    public StoryReplyRequestDtoValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Текст ответа не может быть пустым.")
            .MaximumLength(2000).WithMessage("Слишком длинный текст ответа (максимум 2000 символов).");
    }
}
