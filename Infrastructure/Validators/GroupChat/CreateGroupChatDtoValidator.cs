using Domain.DTOs.GroupChat;
using FluentValidation;

namespace Infrastructure.Validators.GroupChat;

/// <summary>Название группы обязательно; список стартовых участников без пустых Id.</summary>
public class CreateGroupChatDtoValidator : AbstractValidator<CreateGroupChatDto>
{
    public CreateGroupChatDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название группы обязательно.")
            .MaximumLength(100).WithMessage("Название не длиннее 100 символов.");

        RuleForEach(x => x.MemberUserIds)
            .NotEmpty().WithMessage("Id участника не может быть пустым.");
    }
}
