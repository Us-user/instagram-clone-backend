using Domain.DTOs.Message;
using FluentValidation;

namespace Infrastructure.Validators.Message;

/// <summary>
/// Ограничения на голосовое сообщение (§8): корректный контекст/чат, положительная длительность
/// (до 10 минут), наличие файла и валидный Id для reply. Тип файла/размер проверяет FileService.
/// </summary>
public class SendVoiceDtoValidator : AbstractValidator<SendVoiceDto>
{
    public SendVoiceDtoValidator()
    {
        RuleFor(x => x.Context).IsInEnum().WithMessage("Некорректный контекст сообщения.");

        RuleFor(x => x.ChatId)
            .GreaterThan(0).WithMessage("Некорректный Id чата.");

        RuleFor(x => x.File)
            .NotNull().WithMessage("Не передан аудиофайл.");

        RuleFor(x => x.Duration)
            .GreaterThan(0).WithMessage("Длительность должна быть больше нуля.")
            .LessThanOrEqualTo(600).WithMessage("Голосовое не длиннее 10 минут.");

        RuleFor(x => x.ReplyToMessageId!.Value)
            .GreaterThan(0).WithMessage("Некорректный Id сообщения для ответа.")
            .When(x => x.ReplyToMessageId.HasValue);
    }
}
