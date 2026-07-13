using Domain.DTOs.Story;
using Domain.Responses;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Сторис с жизнью 24 часа: ленты (подписки/юзер/мои), лайк-тумблер, просмотр (уникально
/// на юзера), создание из поста или файла, удаление автором. Текущий юзер — из claims.
/// </summary>
public interface IStoryService
{
    /// <summary>Активные (&lt; 24ч) сторис тех, на кого подписан текущий юзер, сгруппированные по авторам.</summary>
    Task<Response<List<GetStoryDto>>> GetStoriesAsync();

    /// <summary>Активные (&lt; 24ч) сторис конкретного пользователя.</summary>
    Task<Response<List<GetStoryDto>>> GetUserStoriesAsync(string userId);

    /// <summary>Активные (&lt; 24ч) сторис текущего пользователя.</summary>
    Task<Response<List<GetStoryDto>>> GetMyStoriesAsync();

    /// <summary>Тумблер лайка сторис. Возвращает текстовый результат.</summary>
    Task<Response<string>> LikeStoryAsync(int? storyId);

    /// <summary>Сторис по id со сводкой по зрителям (viewerDto).</summary>
    Task<Response<GetStoryDto>> GetStoryByIdAsync(int? id);

    /// <summary>Создание сторис из поста (PostId) или из файла (multipart Image).</summary>
    Task<Response<GetStoryDto>> AddStoriesAsync(int? postId, AddStoryDto dto);

    /// <summary>Удаление сторис — только автор. Собственный файл удаляется с диска.</summary>
    Task<Response<bool>> DeleteStoryAsync(int? id);

    /// <summary>Зафиксировать просмотр сторис (уникально на юзера). Идемпотентно.</summary>
    Task<Response<GetStoryViewDto>> AddStoryViewAsync(int? storyId);
}
