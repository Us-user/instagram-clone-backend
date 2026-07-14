using Domain.DTOs.Message;
using Domain.Enums;
using Domain.Responses;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Кросс-контекстные операции над сообщениями (§8): реакции, пересылка и голосовые — одинаково
/// для личных (Direct) и групповых (Group) чатов. Id текущего юзера — из claims.
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// Поставить/снять/заменить реакцию текущего юзера на сообщение (тумблер/замена). Возвращает
    /// и рассылает участникам актуальный набор реакций сообщения.
    /// </summary>
    Task<Response<MessageReactionsDto>> ReactAsync(int? messageId, MessageContext? context, string? emoji);

    /// <summary>
    /// Переслать сообщение (копия текста/файла с <c>IsForwarded=true</c>) в целевой чат/группу.
    /// Возвращает созданное сообщение в DTO целевого контекста.
    /// </summary>
    Task<Response<object>> ForwardAsync(
        int? messageId, MessageContext? context, int? targetChatId, MessageContext? targetContext);

    /// <summary>
    /// Отправить голосовое сообщение (аудио в <c>wwwroot/voice</c>, длительность + волна) в личный
    /// чат или группу. Возвращает созданное сообщение в DTO целевого контекста.
    /// </summary>
    Task<Response<object>> SendVoiceAsync(SendVoiceDto dto);
}
