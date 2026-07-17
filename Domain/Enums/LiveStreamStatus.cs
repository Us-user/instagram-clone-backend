namespace Domain.Enums;

/// <summary>Состояние прямого эфира. Значения фиксированы контрактом — новые добавлять только в конец.</summary>
public enum LiveStreamStatus
{
    /// <summary>Эфир идёт.</summary>
    Live = 0,

    /// <summary>Эфир завершён.</summary>
    Ended = 1
}
