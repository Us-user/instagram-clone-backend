using Infrastructure.Data.Seed;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Поиск пользователей, история поиска (текст и просмотренные профили) и удаление
/// пользователя (только Admin). Пути/методы/параметры воспроизводят контракт дословно.
/// </summary>
[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService) => _userService = userService;

    /// <summary>Поиск пользователей по userName/email с пагинацией.</summary>
    [HttpGet("get-users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? userName,
        [FromQuery] string? email,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize)
    {
        var result = await _userService.GetUsersAsync(userName, email, pageNumber, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Добавить запись в историю текстового поиска текущего юзера.</summary>
    [HttpPost("add-search-history")]
    public async Task<IActionResult> AddSearchHistory([FromQuery] string? text)
    {
        var result = await _userService.AddSearchHistoryAsync(text);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>История текстового поиска текущего юзера.</summary>
    [HttpGet("get-search-histories")]
    public async Task<IActionResult> GetSearchHistories()
    {
        var result = await _userService.GetSearchHistoriesAsync();
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Удалить одну запись истории поиска.</summary>
    [HttpDelete("delete-search-history")]
    public async Task<IActionResult> DeleteSearchHistory([FromQuery] int id)
    {
        var result = await _userService.DeleteSearchHistoryAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Очистить всю историю текстового поиска текущего юзера.</summary>
    [HttpDelete("delete-search-histories")]
    public async Task<IActionResult> DeleteSearchHistories()
    {
        var result = await _userService.DeleteSearchHistoriesAsync();
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Записать «просмотрел профиль» в историю просмотренных профилей.</summary>
    [HttpPost("add-user-search-history")]
    public async Task<IActionResult> AddUserSearchHistory([FromQuery] string? userSearchId)
    {
        var result = await _userService.AddUserSearchHistoryAsync(userSearchId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>История просмотренных профилей текущего юзера.</summary>
    [HttpGet("get-user-search-histories")]
    public async Task<IActionResult> GetUserSearchHistories()
    {
        var result = await _userService.GetUserSearchHistoriesAsync();
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Удалить одну запись истории просмотренных профилей.</summary>
    [HttpDelete("delete-user-search-history")]
    public async Task<IActionResult> DeleteUserSearchHistory([FromQuery] int id)
    {
        var result = await _userService.DeleteUserSearchHistoryAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Очистить всю историю просмотренных профилей текущего юзера.</summary>
    [HttpDelete("delete-user-search-histories")]
    public async Task<IActionResult> DeleteUserSearchHistories()
    {
        var result = await _userService.DeleteUserSearchHistoriesAsync();
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Удаление пользователя. Доступно только роли Admin.</summary>
    [HttpDelete("delete-user")]
    [Authorize(Roles = DbInitializer.AdminRole)]
    public async Task<IActionResult> DeleteUser([FromQuery] string? userId)
    {
        var result = await _userService.DeleteUserAsync(userId);
        return StatusCode(result.StatusCode, result);
    }
}
