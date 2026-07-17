using Domain.DTOs.Live;
using FluentValidation;

namespace Infrastructure.Validators.Live;

/// <summary>
/// Базовые ограничения комментария эфира: непустой текст и жёсткий верхний предел под колонку БД.
/// Бизнес-лимит длины (<c>Streaming:MaxCommentLength</c>) — конфигурируемый и проверяется в сервисе.
/// </summary>
public class AddLiveCommentDtoValidator : AbstractValidator<AddLiveCommentDto>
{
    public AddLiveCommentDtoValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Комментарий не может быть пустым.")
            .MaximumLength(1000).WithMessage("Слишком длинный комментарий.");
    }
}
