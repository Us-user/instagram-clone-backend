using Domain.DTOs.Settings;
using FluentValidation;

namespace Infrastructure.Validators.Settings;

/// <summary>Валидация настроек приватности: все enum-поля — из допустимых значений.</summary>
public class UpdatePrivacySettingsDtoValidator : AbstractValidator<UpdatePrivacySettingsDto>
{
    public UpdatePrivacySettingsDtoValidator()
    {
        RuleFor(x => x.WhoCanMessage).IsInEnum().WithMessage("Некорректное значение «кто может писать».");
        RuleFor(x => x.WhoCanMention).IsInEnum().WithMessage("Некорректное значение «кто может упоминать».");
        RuleFor(x => x.WhoCanReplyStory).IsInEnum().WithMessage("Некорректное значение «кто может отвечать на сторис».");
    }
}
