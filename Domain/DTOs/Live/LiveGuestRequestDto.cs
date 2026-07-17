namespace Domain.DTOs.Live;

/// <summary>
/// Заявка на выход в эфир гостем — для списка Pending у хоста (<c>get-guest-requests</c>) и для
/// real-time события <c>GuestRequestReceived</c>.
/// </summary>
public class LiveGuestRequestDto
{
    public int RequestId { get; set; }
    public LiveUserDto User { get; set; } = new();
    public DateTime RequestedAt { get; set; }

    /// <summary>Статус заявки строкой: <c>Pending</c>/<c>Approved</c>/<c>Declined</c>/<c>Cancelled</c>/<c>Removed</c>.</summary>
    public string Status { get; set; } = string.Empty;
}
