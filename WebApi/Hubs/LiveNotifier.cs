using Domain.DTOs.Live;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace WebApi.Hubs;

/// <summary>
/// Реализация <see cref="ILiveNotifier"/> поверх <see cref="LiveHub"/>. Живёт в WebApi (там, где SignalR),
/// а <c>LiveStreamService</c> в Infrastructure зависит только от абстракции. Групповые события адресуются
/// группе <c>live_{streamId}</c>, адресные — конкретному пользователю/подписчикам через <c>Clients.User(s)</c>.
/// </summary>
public class LiveNotifier : ILiveNotifier
{
    private readonly IHubContext<LiveHub, ILiveClient> _hub;

    public LiveNotifier(IHubContext<LiveHub, ILiveClient> hub) => _hub = hub;

    private ILiveClient Group(int streamId) => _hub.Clients.Group(LiveHub.GroupName(streamId));

    public Task ViewerJoinedAsync(int streamId, LiveUserDto viewer) =>
        Group(streamId).ViewerJoined(viewer);

    public Task ViewerLeftAsync(int streamId, string userId) =>
        Group(streamId).ViewerLeft(new LiveUserRefDto { UserId = userId });

    public Task ViewerCountAsync(int streamId, int count) =>
        Group(streamId).ViewerCount(new LiveViewerCountDto { StreamId = streamId, Count = count });

    public Task NewCommentAsync(int streamId, LiveCommentDto comment) =>
        Group(streamId).NewComment(comment);

    public Task NewLikeAsync(int streamId, string userId) =>
        Group(streamId).NewLike(new LiveUserRefDto { UserId = userId });

    public Task CommentPinnedAsync(int streamId, int commentId) =>
        Group(streamId).CommentPinned(new LiveCommentRefDto { CommentId = commentId });

    public Task CommentDeletedAsync(int streamId, int commentId) =>
        Group(streamId).CommentDeleted(new LiveCommentRefDto { CommentId = commentId });

    public Task GuestRequestReceivedAsync(string hostUserId, LiveGuestRequestDto request) =>
        _hub.Clients.User(hostUserId).GuestRequestReceived(request);

    public Task GuestApprovedAsync(string userId, JoinLiveResultDto result) =>
        _hub.Clients.User(userId).GuestApproved(result);

    public Task GuestRequestDeclinedAsync(string userId, int requestId) =>
        _hub.Clients.User(userId).GuestRequestDeclined(new LiveGuestRequestRefDto { RequestId = requestId });

    public Task GuestJoinedAsync(int streamId, LiveUserDto guest) =>
        Group(streamId).GuestJoined(guest);

    public Task GuestLeftAsync(int streamId, string userId) =>
        Group(streamId).GuestLeft(new LiveUserRefDto { UserId = userId });

    public async Task ViewerBannedAsync(int streamId, string bannedUserId)
    {
        var payload = new LiveUserRefDto { UserId = bannedUserId };
        // Всем в эфире (убрать из списка) и адресно забаненному (мог не быть в группе — чтобы отключиться).
        await Group(streamId).ViewerBanned(payload);
        await _hub.Clients.User(bannedUserId).ViewerBanned(payload);
    }

    public Task StreamEndedAsync(int streamId, LiveStreamEndedDto payload) =>
        Group(streamId).StreamEnded(payload);

    public Task StreamStartedAsync(IReadOnlyCollection<string> subscriberIds, LiveStreamStartedDto payload) =>
        _hub.Clients.Users(subscriberIds.ToList()).StreamStarted(payload);
}
