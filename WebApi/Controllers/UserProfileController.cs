using Domain.DTOs.UserProfile;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Профиль пользователя: просмотр со счётчиками и флагом подписки, редактирование,
/// изображение профиля и избранные посты. Пути/методы/параметры — дословно из контракта.
/// </summary>
[ApiController]
[Route("[controller]")]
public class UserProfileController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;

    public UserProfileController(IUserProfileService userProfileService)
        => _userProfileService = userProfileService;

    /// <summary>Профиль по id пользователя: счётчики постов/подписчиков/подписок + isFollowing.</summary>
    [HttpGet("get-user-profile-by-id")]
    public async Task<IActionResult> GetUserProfileById([FromQuery] string? id)
    {
        var result = await _userProfileService.GetByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Подписан ли текущий пользователь на указанный профиль.</summary>
    [HttpGet("get-is-follow-user-profile-by-id")]
    public async Task<IActionResult> GetIsFollowUserProfileById([FromQuery] string? followingUserId)
    {
        var result = await _userProfileService.IsFollowAsync(followingUserId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Профиль текущего пользователя.</summary>
    [HttpGet("get-my-profile")]
    public async Task<IActionResult> GetMyProfile()
    {
        var result = await _userProfileService.GetMyProfileAsync();
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Обновление «о себе» и пола текущего пользователя.</summary>
    [HttpPut("update-user-profile")]
    public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateUserProfileDto dto)
    {
        var result = await _userProfileService.UpdateAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Избранные посты текущего пользователя с пагинацией.</summary>
    [HttpGet("get-post-favorites")]
    public async Task<IActionResult> GetPostFavorites(
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize)
    {
        var result = await _userProfileService.GetPostFavoritesAsync(pageNumber, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Загрузка/замена изображения профиля (multipart/form-data).</summary>
    [HttpPut("update-user-image-profile")]
    public async Task<IActionResult> UpdateUserImageProfile(IFormFile? imageFile)
    {
        var result = await _userProfileService.UpdateImageAsync(imageFile);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Удаление изображения профиля.</summary>
    [HttpDelete("delete-user-image-profile")]
    public async Task<IActionResult> DeleteUserImageProfile()
    {
        var result = await _userProfileService.DeleteImageAsync();
        return StatusCode(result.StatusCode, result);
    }
}
