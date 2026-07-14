using Domain.DTOs.Post;
using Domain.Responses;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Explore / рекомендации (§12): персональная лента открытия нового контента (content-based,
/// без ML) и фолбэк по чистой популярности для новых юзеров (cold start). Id текущего юзера —
/// из claims. Возвращает те же <see cref="GetPostDto"/>, что и остальные ленты постов.
/// </summary>
public interface IExploreService
{
    /// <summary>
    /// Персональная лента рекомендаций: профиль интересов (веса favorite&gt;comment&gt;like&gt;view
    /// + затухание) → скоринг кандидатов по хэштегам/автору/популярности/свежести → разнообразие.
    /// Исключаются свои посты, уже просмотренные, блок в любую сторону, приватные без Accepted-
    /// подписки и авторы, на которых уже подписан.
    /// </summary>
    Task<PagedResponse<List<GetPostDto>>> GetFeedAsync(int? pageNumber, int? pageSize);

    /// <summary>
    /// Cold start: чистая популярность (лайки+комменты+просмотры), свежесть — тай-брейк. Те же
    /// фильтры доступа (блок/приват) и исключение своих постов, что и в персональной ленте.
    /// </summary>
    Task<PagedResponse<List<GetPostDto>>> GetPopularAsync(int? pageNumber, int? pageSize);
}
