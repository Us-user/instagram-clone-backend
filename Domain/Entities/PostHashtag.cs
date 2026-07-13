namespace Domain.Entities;

/// <summary>Связь многие-ко-многим между <see cref="Post"/> и <see cref="Hashtag"/> (Phase 13).</summary>
public class PostHashtag
{
    public int Id { get; set; }

    public int PostId { get; set; }
    public int HashtagId { get; set; }

    public Post? Post { get; set; }
    public Hashtag? Hashtag { get; set; }
}
