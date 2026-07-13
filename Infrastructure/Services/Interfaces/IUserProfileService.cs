using Domain.DTOs.Post;
using Domain.DTOs.UserProfile;
using Domain.Responses;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Профиль пользователя: просмотр со счётчиками и флагом подписки, редактирование,
/// работа с изображением профиля и список избранных постов. Текущий юзер — из claims.
/// </summary>
public interface IUserProfileService
{
    /// <summary>Профиль по userId со счётчиками постов/подписчиков/подписок и isFollowing текущего юзера.</summary>
    Task<Response<GetUserProfileDto>> GetByIdAsync(string? id);

    /// <summary>Подписан ли текущий пользователь на <paramref name="followingUserId"/>.</summary>
    Task<Response<bool>> IsFollowAsync(string? followingUserId);

    /// <summary>Профиль текущего пользователя.</summary>
    Task<Response<GetUserProfileDto>> GetMyProfileAsync();

    /// <summary>Обновление «о себе» и пола текущего пользователя.</summary>
    Task<Response<GetUserProfileDto>> UpdateAsync(UpdateUserProfileDto dto);

    /// <summary>Избранные посты текущего пользователя с пагинацией.</summary>
    Task<PagedResponse<List<GetPostDto>>> GetPostFavoritesAsync(int? pageNumber, int? pageSize);

    /// <summary>Загрузка/замена изображения профиля. Возвращает имя нового файла.</summary>
    Task<Response<string>> UpdateImageAsync(IFormFile? imageFile);

    /// <summary>Удаление изображения профиля (файл с диска + очистка поля).</summary>
    Task<Response<bool>> DeleteImageAsync();
}
