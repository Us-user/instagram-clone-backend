using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Хэштеги (Phase 13): поиск по префиксу, лента постов по тегу и тренды за период.
/// Теги нормализуются (нижний регистр, без <c>#</c>). Пути/методы/параметры воспроизводят
/// контракт дословно. Id текущего юзера — из claims (для фильтрации ленты по тегу).
/// </summary>
[ApiController]
[Route("[controller]")]
public class HashtagController : ControllerBase
{
    private readonly IHashtagService _hashtagService;

    public HashtagController(IHashtagService hashtagService) => _hashtagService = hashtagService;

    /// <summary>Поиск/автодополнение тегов по префиксу, сортировка по популярности.</summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? query,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize)
    {
        var result = await _hashtagService.SearchAsync(query, pageNumber, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Лента постов по хэштегу (свежие сверху), с учётом блокировок/приватности.</summary>
    [HttpGet("get-posts-by-tag")]
    public async Task<IActionResult> GetPostsByTag(
        [FromQuery] string? tag,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize)
    {
        var result = await _hashtagService.GetPostsByTagAsync(tag, pageNumber, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Популярные (трендовые) теги за последний период.</summary>
    [HttpGet("get-trending")]
    public async Task<IActionResult> GetTrending(
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize)
    {
        var result = await _hashtagService.GetTrendingAsync(pageNumber, pageSize);
        return StatusCode(result.StatusCode, result);
    }
}
