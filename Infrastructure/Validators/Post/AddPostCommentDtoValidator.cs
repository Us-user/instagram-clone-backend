using Domain.DTOs.Post;
using FluentValidation;

namespace Infrastructure.Validators.Post;

/// <summary>Комментарий не пуст, указан корректный пост.</summary>
public class AddPostCommentDtoValidator : AbstractValidator<AddPostCommentDto>
{
    public AddPostCommentDtoValidator()
    {
        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("Текст комментария обязателен.")
            .MaximumLength(1000).WithMessage("Комментарий не длиннее 1000 символов.");

        RuleFor(x => x.PostId).GreaterThan(0).WithMessage("Некорректный Id поста.");
    }
}
