using Domain.DTOs.Location;
using FluentValidation;

namespace Infrastructure.Validators.Location;

/// <summary>Обновление локации: корректный Id и все поля обязательны.</summary>
public class UpdateLocationDtoValidator : AbstractValidator<UpdateLocationDto>
{
    public UpdateLocationDtoValidator()
    {
        RuleFor(x => x.LocationId).GreaterThan(0).WithMessage("Некорректный Id локации.");
        RuleFor(x => x.City).NotEmpty().WithMessage("Город обязателен.");
        RuleFor(x => x.State).NotEmpty().WithMessage("Регион обязателен.");
        RuleFor(x => x.ZipCode).NotEmpty().WithMessage("Почтовый индекс обязателен.");
        RuleFor(x => x.Country).NotEmpty().WithMessage("Страна обязательна.");
    }
}
