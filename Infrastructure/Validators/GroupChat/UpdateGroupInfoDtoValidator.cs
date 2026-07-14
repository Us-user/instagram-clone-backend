using Domain.DTOs.GroupChat;
using FluentValidation;

namespace Infrastructure.Validators.GroupChat;

/// <summary>Название группы (если передано) не длиннее 100 символов.</summary>
public class UpdateGroupInfoDtoValidator : AbstractValidator<UpdateGroupInfoDto>
{
    public UpdateGroupInfoDtoValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("Название не длиннее 100 символов.")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));
    }
}
