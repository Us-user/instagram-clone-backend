namespace Domain.Entities;

/// <summary>
/// «Сердечко» в эфире. В отличие от лайка поста, это не тумблер, а поток событий — один зритель может
/// слать много (с троттлингом). Каждая запись = одно сердечко; счётчик <c>LiveStream.LikesCount</c>
/// денормализован и растёт инкрементами.
/// </summary>
public class LiveLike
{
    public int Id { get; set; }

    public int LiveStreamId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public LiveStream? LiveStream { get; set; }
    public User? User { get; set; }
}
