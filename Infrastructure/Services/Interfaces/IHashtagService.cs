using Domain.DTOs.Hashtag;
using Domain.DTOs.Post;
using Domain.Responses;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Хэштеги (Phase 13): разбор <c>#tag</c> из Title/Content поста при создании (upsert +
/// инкремент <c>PostsCount</c>) и декремент при удалении, а также поиск/лента/тренды тегов.
/// </summary>
public interface IHashtagService
{
    /// <summary>
    /// Разбирает теги из <paramref name="title"/>/<paramref name="content"/>, создаёт
    /// отсутствующие, связывает с постом <paramref name="postId"/> и инкрементит счётчики.
    /// </summary>
    Task ProcessPostHashtagsAsync(int postId, string? title, string? content);

    /// <summary>
    /// Снимает связи поста <paramref name="postId"/> с тегами и декрементит их счётчики
    /// (вызывать перед удалением поста).
    /// </summary>
    Task RemovePostHashtagsAsync(int postId);

    /// <summary>Поиск тегов по префиксу, сортировка по популярности.</summary>
    Task<PagedResponse<List<GetHashtagDto>>> SearchAsync(string? query, int? pageNumber, int? pageSize);

    /// <summary>Лента постов по хэштегу (свежие сверху), с учётом блокировок/приватности.</summary>
    Task<PagedResponse<List<GetPostDto>>> GetPostsByTagAsync(string? tag, int? pageNumber, int? pageSize);

    /// <summary>Популярные теги за последний период.</summary>
    Task<PagedResponse<List<GetHashtagDto>>> GetTrendingAsync(int? pageNumber, int? pageSize);
}
