using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Explore / рекомендации (§12): персональная лента открытия нового контента (content-based) и
/// фолбэк по популярности для новых юзеров (cold start). Id текущего юзера — из JWT claims;
/// оба эндпоинта авторизованы по умолчанию. Пути/методы/параметры воспроизводят контракт дословно.
/// </summary>
[ApiController]
[Route("[controller]")]
public class ExploreController : ControllerBase
{
    private readonly IExploreService _service;

    public ExploreController(IExploreService service) => _service = service;

    /// <summary>Персональная лента рекомендаций (по интересам: хэштеги/авторы + популярность/свежесть).</summary>
    [HttpGet("get-feed")]
    public async Task<IActionResult> GetFeed(
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize)
    {
        var result = await _service.GetFeedAsync(pageNumber, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Популярное (cold start): чистая популярность за вычетом своих/скрытых постов.</summary>
    [HttpGet("get-popular")]
    public async Task<IActionResult> GetPopular(
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize)
    {
        var result = await _service.GetPopularAsync(pageNumber, pageSize);
        return StatusCode(result.StatusCode, result);
    }
}
