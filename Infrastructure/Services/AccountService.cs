using Domain.DTOs.Account;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Responses;
using FluentValidation;
using Infrastructure.Data;
using Infrastructure.Data.Seed;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Реализация аутентификации поверх ASP.NET Core Identity.
/// Проверяет уникальность email/userName, создаёт пустой профиль при регистрации,
/// выдаёт JWT через <see cref="ITokenService"/>; id текущего юзера берёт из claims.
/// </summary>
public class AccountService : IAccountService
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUser;
    private readonly DataContext _context;
    private readonly IValidator<RegisterDto> _registerValidator;
    private readonly IValidator<LoginDto> _loginValidator;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        UserManager<User> userManager,
        ITokenService tokenService,
        ICurrentUserService currentUser,
        DataContext context,
        IValidator<RegisterDto> registerValidator,
        IValidator<LoginDto> loginValidator,
        ILogger<AccountService> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _currentUser = currentUser;
        _context = context;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _logger = logger;
    }

    public async Task<Response<string>> RegisterAsync(RegisterDto dto)
    {
        await _registerValidator.ValidateAndThrowAsync(dto);

        if (await _userManager.FindByNameAsync(dto.UserName) is not null)
            throw new BadRequestException("Имя пользователя уже занято.");

        if (await _userManager.FindByEmailAsync(dto.Email) is not null)
            throw new BadRequestException("Email уже используется.");

        var user = new User
        {
            UserName = dto.UserName,
            Email = dto.Email,
            FullName = dto.FullName
        };

        var created = await _userManager.CreateAsync(user, dto.Password);
        if (!created.Succeeded)
            throw new BadRequestException(string.Join("; ", created.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, DbInitializer.UserRole);

        // При регистрации создаётся пустой профиль (правило контракта).
        _context.UserProfiles.Add(new UserProfile { UserId = user.Id });
        await _context.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateToken(user, roles);
        return new Response<string>(token);
    }

    public async Task<Response<string>> LoginAsync(LoginDto dto)
    {
        await _loginValidator.ValidateAndThrowAsync(dto);

        var user = await _userManager.FindByNameAsync(dto.UserName);
        if (user is null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            throw new BadRequestException("Неверное имя пользователя или пароль.");

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateToken(user, roles);
        return new Response<string>(token);
    }

    public async Task<Response<string>> ForgotPasswordAsync(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new BadRequestException("Email обязателен.");

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            throw new NotFoundException("Пользователь с таким email не найден.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // В учебных целях reset-токен возвращается в ответе и пишется в лог.
        _logger.LogInformation("Reset-токен для {Email}: {Token}", email, token);
        return new Response<string>(token);
    }

    public async Task<Response<string>> ResetPasswordAsync(
        string? token, string? email, string? password, string? confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new BadRequestException("Токен обязателен.");
        if (string.IsNullOrWhiteSpace(email))
            throw new BadRequestException("Email обязателен.");
        if (string.IsNullOrWhiteSpace(password))
            throw new BadRequestException("Пароль обязателен.");
        if (password != confirmPassword)
            throw new BadRequestException("Пароли не совпадают.");

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            throw new NotFoundException("Пользователь с таким email не найден.");

        var result = await _userManager.ResetPasswordAsync(user, token, password);
        if (!result.Succeeded)
            throw new BadRequestException(string.Join("; ", result.Errors.Select(e => e.Description)));

        return new Response<string>("Пароль успешно сброшен.");
    }

    public async Task<Response<string>> ChangePasswordAsync(
        string? oldPassword, string? password, string? confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(oldPassword))
            throw new BadRequestException("Текущий пароль обязателен.");
        if (string.IsNullOrWhiteSpace(password))
            throw new BadRequestException("Новый пароль обязателен.");
        if (password != confirmPassword)
            throw new BadRequestException("Пароли не совпадают.");

        var userId = _currentUser.GetRequiredUserId();
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            throw new NotFoundException("Пользователь не найден.");

        var result = await _userManager.ChangePasswordAsync(user, oldPassword, password);
        if (!result.Succeeded)
            throw new BadRequestException(string.Join("; ", result.Errors.Select(e => e.Description)));

        return new Response<string>("Пароль успешно изменён.");
    }
}
