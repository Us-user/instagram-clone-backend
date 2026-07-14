using Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Domain.DTOs.Story;

/// <summary>
/// Создание сторис (multipart/form-data). <see cref="Image"/> обязателен, если сторис
/// создаётся из файла; если сторис из поста — задаётся <c>PostId</c> (query), а Image не нужен.
/// </summary>
public class AddStoryDto
{
    /// <summary>Файл сторис. Обязателен, когда сторис не создаётся из поста (PostId не задан).</summary>
    public IFormFile? Image { get; set; }

    /// <summary>Аудитория сторис (§9): <c>All</c> (по умолчанию) или <c>CloseFriends</c>.</summary>
    public StoryAudience? Audience { get; set; }
}
