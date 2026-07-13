using Domain.DTOs.Location;
using FluentValidation;

namespace Infrastructure.Validators.Location;

/// <summary>Все поля локации обязательны.</summary>
public class AddLocationDtoValidator : AbstractValidator<AddLocationDto>
{
    public AddLocationDtoValidator()
    {
        RuleFor(x => x.City).NotEmpty().WithMessage("Город обязателен.");
        RuleFor(x => x.State).NotEmpty().WithMessage("Регион обязателен.");
        RuleFor(x => x.ZipCode).NotEmpty().WithMessage("Почтовый индекс обязателен.");
        RuleFor(x => x.Country).NotEmpty().WithMessage("Страна обязательна.");
    }
}
