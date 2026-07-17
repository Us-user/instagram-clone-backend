namespace Domain.Enums;

/// <summary>
/// Статус заявки зрителя на выход в эфир гостем. Заявка сверх лимита остаётся <see cref="Pending"/>
/// в очереди. Значения фиксированы контрактом — новые добавлять только в конец.
/// </summary>
public enum LiveGuestRequestStatus
{
    /// <summary>Ожидает решения хоста (в т.ч. в очереди при достигнутом лимите гостей).</summary>
    Pending = 0,

    /// <summary>Одобрена хостом — зритель стал гостем (Publisher).</summary>
    Approved = 1,

    /// <summary>Отклонена хостом.</summary>
    Declined = 2,

    /// <summary>Отменена самим заявителем.</summary>
    Cancelled = 3,

    /// <summary>Гостя убрали из эфира (роль понижена до Subscriber).</summary>
    Removed = 4
}
