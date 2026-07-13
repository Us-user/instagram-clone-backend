using Domain.DTOs.Settings;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Настройки приватности текущего пользователя (§6): приватность аккаунта, показ онлайн-статуса,
/// кто может писать/упоминать/отвечать на сторис. Id юзера — из claims.
/// </summary>
[ApiController]
[Route("[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _service;

    public SettingsController(ISettingsService service) => _service = service;

    /// <summary>Текущие настройки приватности (создаются по умолчанию при первом обращении).</summary>
    [HttpGet("get-privacy")]
    public async Task<IActionResult> GetPrivacy()
    {
        var result = await _service.GetPrivacyAsync();
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Обновить настройки приватности.</summary>
    [HttpPut("update-privacy")]
    public async Task<IActionResult> UpdatePrivacy([FromBody] UpdatePrivacySettingsDto dto)
    {
        var result = await _service.UpdatePrivacyAsync(dto);
        return StatusCode(result.StatusCode, result);
    }
}
