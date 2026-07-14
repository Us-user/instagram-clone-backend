using Domain.Entities;
using Domain.Exceptions;
using Domain.Responses;
using Infrastructure.Data.Seed;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Services;

/// <summary>
/// Административные действия (§10): верификация пользователей и управление ролью Admin.
/// Доступ ограничен ролью Admin на уровне контроллера; здесь — бизнес-правила и валидация.
/// Флаг «синей галочки» хранится на <see cref="User.IsVerified"/>, роль Admin — через
/// ASP.NET Identity. Текущий администратор — из claims (для защиты от самолокаута).
/// </summary>
public class AdminService : IAdminService
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUserService _currentUser;

    public AdminService(UserManager<User> userManager, ICurrentUserService currentUser)
    {
        _userManager = userManager;
        _currentUser = currentUser;
    }

    public async Task<Response<bool>> VerifyUserAsync(string? userId)
    {
        var user = await FindUserAsync(userId);

        if (user.IsVerified)
            throw new BadRequestException("Пользователь уже верифицирован.");

        user.IsVerified = true;
        await UpdateAsync(user);

        return new Response<bool>(true);
    }

    public async Task<Response<bool>> UnverifyUserAsync(string? userId)
    {
        var user = await FindUserAsync(userId);

        if (!user.IsVerified)
            throw new BadRequestException("Пользователь не верифицирован.");

        user.IsVerified = false;
        await UpdateAsync(user);

        return new Response<bool>(true);
    }

    public async Task<Response<bool>> GrantAdminAsync(string? userId)
    {
        var user = await FindUserAsync(userId);

        if (await _userManager.IsInRoleAsync(user, DbInitializer.AdminRole))
            throw new BadRequestException("Пользователь уже является администратором.");

        var result = await _userManager.AddToRoleAsync(user, DbInitializer.AdminRole);
        if (!result.Succeeded)
            throw new BadRequestException(string.Join("; ", result.Errors.Select(e => e.Description)));

        return new Response<bool>(true);
    }

    public async Task<Response<bool>> RevokeAdminAsync(string? userId)
    {
        var user = await FindUserAsync(userId);

        // Защита от самолокаута: администратор не может снять роль Admin с самого себя.
        if (user.Id == _currentUser.GetRequiredUserId())
            throw new BadRequestException("Нельзя снять роль администратора с самого себя.");

        if (!await _userManager.IsInRoleAsync(user, DbInitializer.AdminRole))
            throw new BadRequestException("Пользователь не является администратором.");

        var result = await _userManager.RemoveFromRoleAsync(user, DbInitializer.AdminRole);
        if (!result.Succeeded)
            throw new BadRequestException(string.Join("; ", result.Errors.Select(e => e.Description)));

        return new Response<bool>(true);
    }

    /// <summary>Найти пользователя по id или бросить понятную ошибку.</summary>
    private async Task<User> FindUserAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new BadRequestException("Id пользователя обязателен.");

        return await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("Пользователь не найден.");
    }

    /// <summary>Сохранить изменения пользователя через Identity, пробросив ошибки store.</summary>
    private async Task UpdateAsync(User user)
    {
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new BadRequestException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
