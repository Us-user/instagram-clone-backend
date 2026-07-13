using Domain.DTOs.Settings;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Responses;
using FluentValidation;
using Infrastructure.Data;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Настройки приватности: чтение и обновление. Источник истины — <see cref="PrivacySettings"/>
/// (1:1 с пользователем); создаётся лениво со значениями по умолчанию при первом обращении.
/// Флаг <see cref="PrivacySettings.IsPrivate"/> синхронизируется с <see cref="User.IsPrivate"/>,
/// который используется как быстрый признак приватности в лентах/подписках. Id юзера — из claims.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly DataContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<UpdatePrivacySettingsDto> _updateValidator;

    public SettingsService(
        DataContext context,
        ICurrentUserService currentUser,
        IValidator<UpdatePrivacySettingsDto> updateValidator)
    {
        _context = context;
        _currentUser = currentUser;
        _updateValidator = updateValidator;
    }

    public async Task<Response<GetPrivacySettingsDto>> GetPrivacyAsync()
    {
        var currentId = _currentUser.GetRequiredUserId();
        var settings = await GetOrCreateAsync(currentId);
        return new Response<GetPrivacySettingsDto>(ToDto(settings));
    }

    public async Task<Response<GetPrivacySettingsDto>> UpdatePrivacyAsync(UpdatePrivacySettingsDto dto)
    {
        await _updateValidator.ValidateAndThrowAsync(dto);

        var currentId = _currentUser.GetRequiredUserId();
        var settings = await GetOrCreateAsync(currentId);

        settings.IsPrivate = dto.IsPrivate;
        settings.ShowOnlineStatus = dto.ShowOnlineStatus;
        settings.WhoCanMessage = dto.WhoCanMessage;
        settings.WhoCanMention = dto.WhoCanMention;
        settings.WhoCanReplyStory = dto.WhoCanReplyStory;

        // Синхронизируем быстрый флаг приватности в профиле пользователя.
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentId)
            ?? throw new NotFoundException("Пользователь не найден.");
        user.IsPrivate = dto.IsPrivate;

        await _context.SaveChangesAsync();
        return new Response<GetPrivacySettingsDto>(ToDto(settings));
    }

    /// <summary>Возвращает настройки пользователя, создавая их со значениями по умолчанию при отсутствии.</summary>
    private async Task<PrivacySettings> GetOrCreateAsync(string userId)
    {
        var settings = await _context.PrivacySettings.FirstOrDefaultAsync(p => p.UserId == userId);
        if (settings is not null)
            return settings;

        settings = new PrivacySettings { UserId = userId };
        _context.PrivacySettings.Add(settings);
        await _context.SaveChangesAsync();
        return settings;
    }

    private static GetPrivacySettingsDto ToDto(PrivacySettings s) => new()
    {
        IsPrivate = s.IsPrivate,
        ShowOnlineStatus = s.ShowOnlineStatus,
        WhoCanMessage = s.WhoCanMessage,
        WhoCanMention = s.WhoCanMention,
        WhoCanReplyStory = s.WhoCanReplyStory
    };
}
