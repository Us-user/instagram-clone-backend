using Domain.DTOs.GroupChat;
using FluentValidation;

namespace Infrastructure.Validators.GroupChat;

/// <summary>
/// Ограничения на сообщение группы: длина текста и корректный Id для reply. Требование
/// «текст или файл» проверяется в сервисе (зависит от файла из multipart).
/// </summary>
public class SendGroupMessageDtoValidator : AbstractValidator<SendGroupMessageDto>
{
    public SendGroupMessageDtoValidator()
    {
        RuleFor(x => x.MessageText)
            .MaximumLength(4000).WithMessage("Сообщение не длиннее 4000 символов.");

        RuleFor(x => x.ReplyToMessageId!.Value)
            .GreaterThan(0).WithMessage("Некорректный Id сообщения для ответа.")
            .When(x => x.ReplyToMessageId.HasValue);
    }
}
