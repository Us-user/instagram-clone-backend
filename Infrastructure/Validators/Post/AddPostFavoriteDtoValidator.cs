using Domain.DTOs.Post;
using FluentValidation;

namespace Infrastructure.Validators.Post;

/// <summary>Указан корректный пост для избранного.</summary>
public class AddPostFavoriteDtoValidator : AbstractValidator<AddPostFavoriteDto>
{
    public AddPostFavoriteDtoValidator()
    {
        RuleFor(x => x.PostId).GreaterThan(0).WithMessage("Некорректный Id поста.");
    }
}
