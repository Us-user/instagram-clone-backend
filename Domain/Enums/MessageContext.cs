namespace Domain.Enums;

/// <summary>
/// Контекст сообщения (§8): личный чат или групповой. Нужен реакциям/пересылке, чтобы
/// один и тот же <c>MessageId</c> различался между таблицами <c>Messages</c> и <c>GroupMessages</c>.
/// </summary>
public enum MessageContext
{
    /// <summary>Сообщение личного чата (<see cref="Entities.Message"/>).</summary>
    Direct = 0,

    /// <summary>Сообщение группового чата (<see cref="Entities.GroupMessage"/>).</summary>
    Group = 1
}
