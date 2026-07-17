using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Заявка зрителя на выход в эфир гостем. Лимит одновременных гостей — <c>Streaming:MaxGuests</c>;
/// заявки сверх лимита остаются <see cref="LiveGuestRequestStatus.Pending"/> в очереди. Статусы
/// отражают весь жизненный цикл: одобрена/отклонена/отменена/убран.
/// </summary>
public class LiveGuestRequest
{
    public int Id { get; set; }

    public int LiveStreamId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public LiveGuestRequestStatus Status { get; set; } = LiveGuestRequestStatus.Pending;

    public DateTime RequestedAt { get; set; }

    /// <summary>Момент решения хоста / отмены. <c>null</c> — пока в статусе Pending.</summary>
    public DateTime? RespondedAt { get; set; }

    public LiveStream? LiveStream { get; set; }
    public User? User { get; set; }
}
