using Domain.DTOs.Location;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Справочник локаций (независимая фича): фильтр с пагинацией, чтение по Id и CRUD.
/// Пути/методы/параметры воспроизводят контракт дословно. Все эндпоинты требуют авторизации.
/// </summary>
[ApiController]
[Route("[controller]")]
public class LocationController : ControllerBase
{
    private readonly ILocationService _locationService;

    public LocationController(ILocationService locationService) => _locationService = locationService;

    /// <summary>Локации с фильтром по City/State/ZipCode/Country и пагинацией.</summary>
    [HttpGet("get-Locations")]
    public async Task<IActionResult> GetLocations(
        [FromQuery] string? city,
        [FromQuery] string? state,
        [FromQuery] string? zipCode,
        [FromQuery] string? country,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize)
    {
        var result = await _locationService.GetLocationsAsync(city, state, zipCode, country, pageNumber, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Локация по Id.</summary>
    [HttpGet("get-Location-by-id")]
    public async Task<IActionResult> GetLocationById([FromQuery] int? id)
    {
        var result = await _locationService.GetLocationByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Создать локацию (body <c>AddLocationDto</c>, все поля обязательны).</summary>
    [HttpPost("add-Location")]
    public async Task<IActionResult> AddLocation([FromBody] AddLocationDto dto)
    {
        var result = await _locationService.AddLocationAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Обновить локацию (body <c>UpdateLocationDto</c> с <c>locationId</c>).</summary>
    [HttpPut("update-Location")]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationDto dto)
    {
        var result = await _locationService.UpdateLocationAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Удалить локацию по Id.</summary>
    [HttpDelete("delete-Location")]
    public async Task<IActionResult> DeleteLocation([FromQuery] int? id)
    {
        var result = await _locationService.DeleteLocationAsync(id);
        return StatusCode(result.StatusCode, result);
    }
}
