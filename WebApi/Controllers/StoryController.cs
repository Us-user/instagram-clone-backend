using Domain.DTOs.Story;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Сторис (жизнь 24ч): ленты (подписки/юзер/мои), лайк, просмотр, создание из поста/файла,
/// удаление автором. Пути/методы/параметры воспроизводят контракт дословно (включая
/// PascalCase-имена <c>LikeStory</c>/<c>GetStoryById</c>/<c>AddStories</c>/<c>DeleteStory</c>).
/// Id текущего юзера — из claims.
/// </summary>
[ApiController]
[Route("[controller]")]
public class StoryController : ControllerBase
{
    private readonly IStoryService _storyService;

    public StoryController(IStoryService storyService) => _storyService = storyService;

    /// <summary>Активные сторис тех, на кого подписан текущий юзер, сгруппированные по авторам.</summary>
    [HttpGet("get-stories")]
    public async Task<IActionResult> GetStories()
    {
        var result = await _storyService.GetStoriesAsync();
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Активные сторис конкретного пользователя.</summary>
    [HttpGet("get-user-stories/{userId}")]
    public async Task<IActionResult> GetUserStories(string userId)
    {
        var result = await _storyService.GetUserStoriesAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Активные сторис текущего пользователя.</summary>
    [HttpGet("get-my-stories")]
    public async Task<IActionResult> GetMyStories()
    {
        var result = await _storyService.GetMyStoriesAsync();
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Тумблер лайка сторис.</summary>
    [HttpPost("LikeStory")]
    public async Task<IActionResult> LikeStory([FromQuery] int? storyId)
    {
        var result = await _storyService.LikeStoryAsync(storyId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Сторис по id со сводкой по зрителям (viewerDto).</summary>
    [HttpGet("GetStoryById")]
    public async Task<IActionResult> GetStoryById([FromQuery] int? id)
    {
        var result = await _storyService.GetStoryByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Создание сторис из поста (PostId) или из файла (multipart/form-data: Image).</summary>
    [HttpPost("AddStories")]
    public async Task<IActionResult> AddStories([FromQuery] int? postId, [FromForm] AddStoryDto dto)
    {
        var result = await _storyService.AddStoriesAsync(postId, dto);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Удаление сторис — только автор.</summary>
    [HttpDelete("DeleteStory")]
    public async Task<IActionResult> DeleteStory([FromQuery] int? id)
    {
        var result = await _storyService.DeleteStoryAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Зафиксировать просмотр сторис (уникально на юзера).</summary>
    [HttpPost("add-story-view")]
    public async Task<IActionResult> AddStoryView([FromQuery] int? storyId)
    {
        var result = await _storyService.AddStoryViewAsync(storyId);
        return StatusCode(result.StatusCode, result);
    }
}
