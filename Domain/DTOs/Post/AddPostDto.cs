using Microsoft.AspNetCore.Http;

namespace Domain.DTOs.Post;

/// <summary>Создание поста (multipart/form-data). Images обязательны.</summary>
public class AddPostDto
{
    public string? Title { get; set; }
    public string? Content { get; set; }

    /// <summary>Опционально: пометить пост как reel (по умолчанию обычный пост).</summary>
    public bool IsReel { get; set; }

    public List<IFormFile> Images { get; set; } = new();
}
