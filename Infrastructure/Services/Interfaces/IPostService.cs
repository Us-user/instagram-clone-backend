using Domain.DTOs.Post;
using Domain.Responses;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Посты и взаимодействия: CRUD, ленты (общая/reels/подписки), тумблеры лайка и избранного,
/// уникальные просмотры и комментарии. Текущий юзер — из claims; владелец ресурса защищён.
/// </summary>
public interface IPostService
{
    /// <summary>Лента с фильтром по автору/заголовку/тексту, счётчиками и флагами текущего юзера.</summary>
    Task<PagedResponse<List<GetPostDto>>> GetPostsAsync(
        string? userId, string? title, string? content, int? pageNumber, int? pageSize);

    /// <summary>Только reels (IsReel = true) с пагинацией.</summary>
    Task<PagedResponse<List<GetPostDto>>> GetReelsAsync(int? pageNumber, int? pageSize);

    /// <summary>Пост по id со счётчиками и флагами текущего юзера.</summary>
    Task<Response<GetPostDto>> GetByIdAsync(int? id);

    /// <summary>Посты текущего пользователя.</summary>
    Task<Response<List<GetPostDto>>> GetMyPostsAsync();

    /// <summary>Лента из постов тех, на кого подписан <paramref name="userId"/> (по умолчанию — текущий).</summary>
    Task<PagedResponse<List<GetPostDto>>> GetFollowingPostAsync(string? userId, int? pageNumber, int? pageSize);

    /// <summary>Создание поста с изображениями (multipart). Images обязательны.</summary>
    Task<Response<GetPostDto>> AddPostAsync(AddPostDto dto);

    /// <summary>Удаление поста — только автор. Файлы изображений удаляются с диска.</summary>
    Task<Response<bool>> DeletePostAsync(int? id);

    /// <summary>Тумблер лайка. Возвращает новое состояние (true — лайкнут).</summary>
    Task<Response<bool>> LikePostAsync(int? postId);

    /// <summary>Зафиксировать просмотр (уникально на юзера). Идемпотентно.</summary>
    Task<Response<bool>> ViewPostAsync(int? postId);

    /// <summary>Добавить комментарий к посту или ответ на комментарий (parentCommentId).</summary>
    Task<Response<GetPostCommentDto>> AddCommentAsync(AddPostCommentDto dto);

    /// <summary>Тумблер лайка комментария. Возвращает новое состояние (true — лайкнут).</summary>
    Task<Response<bool>> LikeCommentAsync(int? commentId);

    /// <summary>Ответы под комментарием (2-й уровень) с пагинацией; скрывает заблокированных.</summary>
    Task<PagedResponse<List<GetPostCommentDto>>> GetCommentRepliesAsync(
        int? commentId, int? pageNumber, int? pageSize);

    /// <summary>Удалить комментарий — только автор комментария.</summary>
    Task<Response<bool>> DeleteCommentAsync(int? commentId);

    /// <summary>Тумблер избранного. Возвращает новое состояние (true — в избранном).</summary>
    Task<Response<bool>> AddPostFavoriteAsync(AddPostFavoriteDto dto);
}
