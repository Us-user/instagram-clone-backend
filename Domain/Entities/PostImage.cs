namespace Domain.Entities;

/// <summary>Изображение поста. В БД хранится только имя файла.</summary>
public class PostImage
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string ImageName { get; set; } = string.Empty;

    public Post? Post { get; set; }
}
