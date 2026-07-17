using Domain.DTOs.Post;
using Infrastructure.Services.Interfaces;

namespace Infrastructure.Common;

/// <summary>
/// Заполняет необязательные <c>*Url</c>-поля DTO абсолютными ссылками на картинки. Существующие
/// поля с именами файлов не трогает (контракт не ломается). Вызывается после материализации DTO —
/// как <see cref="MentionEnrichment"/>. Централизует логику, чтобы её не дублировали сервисы.
/// </summary>
public static class ImageUrlEnrichment
{
    /// <summary>Проставляет постам <see cref="GetPostDto.ImagesUrl"/> и <see cref="GetPostDto.UserImageUrl"/>.</summary>
    public static void FillPosts(IImageUrlBuilder urls, IEnumerable<GetPostDto> posts)
    {
        foreach (var post in posts)
        {
            post.ImagesUrl = urls.BuildMany(post.Images);
            post.UserImageUrl = urls.Build(post.UserImage);
        }
    }
}
