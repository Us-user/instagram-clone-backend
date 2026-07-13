using Domain.DTOs.Settings;
using Domain.Responses;

namespace Infrastructure.Services.Interfaces;

/// <summary>Настройки приватности текущего пользователя (§6). Id юзера — из claims.</summary>
public interface ISettingsService
{
    /// <summary>Текущие настройки приватности (создаются по умолчанию при первом обращении).</summary>
    Task<Response<GetPrivacySettingsDto>> GetPrivacyAsync();

    /// <summary>Обновить настройки приватности (синхронизирует флаг приватности с профилем).</summary>
    Task<Response<GetPrivacySettingsDto>> UpdatePrivacyAsync(UpdatePrivacySettingsDto dto);
}
