using Domain.DTOs.Account;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Responses;
using FluentValidation;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.Data.Seed;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Реализация аутентификации поверх ASP.NET Core Identity.
/// Проверяет уникальность email/userName, создаёт пустой профиль при регистрации,
/// выдаёт JWT через <see cref="ITokenService"/>; id текущего юзера берёт из claims.
/// </summary>
public class AccountService : IAccountService
{
    /// <summary>Имя издателя в otpauth-URI (отображается в приложении-аутентификаторе).</summary>
    private const string TwoFactorIssuer = "InstagramClone";

    /// <summary>Сколько резервных кодов выдавать при включении/перевыпуске.</summary>
    private const int BackupCodesCount = 10;

    private readonly UserManager<User> _userManager;
    private readonly ISessionService _sessionService;
    private readonly ICurrentUserService _currentUser;
    private readonly DataContext _context;
    private readonly ITotpService _totpService;
    private readonly ITwoFactorTokenStore _twoFactorStore;
    private readonly IValidator<RegisterDto> _registerValidator;
    private readonly IValidator<LoginDto> _loginValidator;
    private readonly IValidator<Login2FaDto> _login2FaValidator;
    private readonly IValidator<Send2FaEmailDto> _send2FaEmailValidator;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        UserManager<User> userManager,
        ISessionService sessionService,
        ICurrentUserService currentUser,
        DataContext context,
        ITotpService totpService,
        ITwoFactorTokenStore twoFactorStore,
        IValidator<RegisterDto> registerValidator,
        IValidator<LoginDto> loginValidator,
        IValidator<Login2FaDto> login2FaValidator,
        IValidator<Send2FaEmailDto> send2FaEmailValidator,
        ILogger<AccountService> logger)
    {
        _userManager = userManager;
        _sessionService = sessionService;
        _currentUser = currentUser;
        _context = context;
        _totpService = totpService;
        _twoFactorStore = twoFactorStore;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _login2FaValidator = login2FaValidator;
        _send2FaEmailValidator = send2FaEmailValidator;
        _logger = logger;
    }

    public async Task<Response<AuthResultDto>> RegisterAsync(RegisterDto dto)
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
        // Сразу открываем сессию и выдаём пару токенов (как логин). Уведомление о новом входе не шлём —
        // это первый вход после регистрации.
        var auth = await _sessionService.CreateSessionAsync(user, roles, notifyOnNewDevice: false);
        return new Response<AuthResultDto>(auth);
    }

    public async Task<Response<object>> LoginAsync(LoginDto dto)
    {
        await _loginValidator.ValidateAndThrowAsync(dto);

        var user = await _userManager.FindByNameAsync(dto.UserName);
        if (user is null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            throw new BadRequestException("Неверное имя пользователя или пароль.");

        // При включённой 2FA пароль — только первый фактор: JWT не выдаём, отдаём временный токен
        // сессии для завершения через /Account/login-2fa (§11).
        if (user.TwoFactorEnabled)
        {
            var twoFactorToken = _twoFactorStore.IssueLoginToken(user.Id);
            var payload = new TwoFactorRequiredDto
            {
                RequiresTwoFactor = true,
                TwoFactorToken = twoFactorToken,
                Methods = new List<string>
                {
                    nameof(TwoFactorMethod.Totp),
                    nameof(TwoFactorMethod.Email),
                    nameof(TwoFactorMethod.Backup)
                }
            };
            return new Response<object>(payload);
        }

        // Без 2FA — создаём сессию и возвращаем пару токенов (access + refresh).
        var roles = await _userManager.GetRolesAsync(user);
        var auth = await _sessionService.CreateSessionAsync(user, roles, notifyOnNewDevice: true);
        return new Response<object>(auth);
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

        // Безопасность: сброс пароля завершает все активные сессии (запрос анонимный — текущей сессии нет).
        await _sessionService.RevokeAllForUserAsync(user.Id);

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

        // Безопасность: смена пароля завершает все прочие сессии, кроме текущей.
        await _sessionService.RevokeAllOtherForCurrentAsync(userId);

        return new Response<string>("Пароль успешно изменён.");
    }

    // ── Двухфакторная аутентификация (§11) ────────────────────────────────────────

    public async Task<Response<AuthResultDto>> LoginTwoFactorAsync(Login2FaDto dto)
    {
        await _login2FaValidator.ValidateAndThrowAsync(dto);

        var userId = _twoFactorStore.PeekLoginToken(dto.TwoFactorToken);
        if (userId is null)
            throw new BadRequestException("Сессия двухфакторной аутентификации истекла. Войдите заново.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || !user.TwoFactorEnabled)
            throw new BadRequestException("Двухфакторная аутентификация недоступна для этого пользователя.");

        if (!Enum.TryParse<TwoFactorMethod>(dto.Method, ignoreCase: true, out var method))
            throw new BadRequestException("Неизвестный метод. Допустимо: Totp, Email, Backup.");

        var verified = method switch
        {
            TwoFactorMethod.Totp => _totpService.VerifyCode(user.TwoFactorSecret, dto.Code),
            TwoFactorMethod.Email => _twoFactorStore.VerifyAndConsumeEmailCode(userId, dto.Code),
            TwoFactorMethod.Backup => await VerifyAndConsumeBackupCodeAsync(userId, dto.Code),
            _ => false
        };

        if (!verified)
            throw new BadRequestException("Неверный или просроченный код подтверждения.");

        _twoFactorStore.InvalidateLoginToken(dto.TwoFactorToken);

        // Второй фактор пройден — только теперь создаём сессию и выдаём пару токенов.
        var roles = await _userManager.GetRolesAsync(user);
        var auth = await _sessionService.CreateSessionAsync(user, roles, notifyOnNewDevice: true);
        return new Response<AuthResultDto>(auth);
    }

    public async Task<Response<Enable2FaResultDto>> EnableTwoFactorAsync()
    {
        var userId = _currentUser.GetRequiredUserId();
        var user = await _userManager.FindByIdAsync(userId)
                   ?? throw new NotFoundException("Пользователь не найден.");

        if (user.TwoFactorEnabled)
            throw new BadRequestException("Двухфакторная аутентификация уже включена.");

        // Секрет генерируется, но 2FA пока НЕ активна — нужно подтвердить кодом (confirm-2fa).
        var secret = _totpService.GenerateSecret();
        user.TwoFactorSecret = secret;
        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
            throw new BadRequestException(string.Join("; ", update.Errors.Select(e => e.Description)));

        var backupCodes = await ReplaceBackupCodesAsync(userId);

        var otpauthUri = _totpService.BuildOtpauthUri(
            secret, user.Email ?? user.UserName ?? user.Id, TwoFactorIssuer);

        return new Response<Enable2FaResultDto>(new Enable2FaResultDto
        {
            Secret = secret,
            OtpauthUri = otpauthUri,
            ManualEntryKey = secret,
            BackupCodes = backupCodes
        });
    }

    public async Task<Response<string>> ConfirmTwoFactorAsync(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new BadRequestException("Код обязателен.");

        var userId = _currentUser.GetRequiredUserId();
        var user = await _userManager.FindByIdAsync(userId)
                   ?? throw new NotFoundException("Пользователь не найден.");

        if (user.TwoFactorEnabled)
            throw new BadRequestException("Двухфакторная аутентификация уже подтверждена.");
        if (string.IsNullOrWhiteSpace(user.TwoFactorSecret))
            throw new BadRequestException("Сначала вызовите enable-2fa.");

        if (!_totpService.VerifyCode(user.TwoFactorSecret, code))
            throw new BadRequestException("Неверный код.");

        user.TwoFactorEnabled = true;
        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
            throw new BadRequestException(string.Join("; ", update.Errors.Select(e => e.Description)));

        return new Response<string>("Двухфакторная аутентификация включена.");
    }

    public async Task<Response<string>> DisableTwoFactorAsync(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new BadRequestException("Код обязателен.");

        var userId = _currentUser.GetRequiredUserId();
        var user = await _userManager.FindByIdAsync(userId)
                   ?? throw new NotFoundException("Пользователь не найден.");

        if (!user.TwoFactorEnabled)
            throw new BadRequestException("Двухфакторная аутентификация не включена.");

        // Отключить можно валидным TOTP-кодом ИЛИ резервным кодом (на случай потери устройства).
        var verified = _totpService.VerifyCode(user.TwoFactorSecret, code)
                       || await VerifyAndConsumeBackupCodeAsync(userId, code);
        if (!verified)
            throw new BadRequestException("Неверный код.");

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
            throw new BadRequestException(string.Join("; ", update.Errors.Select(e => e.Description)));

        _context.BackupCodes.RemoveRange(_context.BackupCodes.Where(b => b.UserId == userId));
        await _context.SaveChangesAsync();

        // Безопасность: отключение 2FA завершает все прочие сессии, кроме текущей.
        await _sessionService.RevokeAllOtherForCurrentAsync(userId);

        return new Response<string>("Двухфакторная аутентификация отключена.");
    }

    public async Task<Response<string>> SendTwoFactorEmailAsync(Send2FaEmailDto dto)
    {
        await _send2FaEmailValidator.ValidateAndThrowAsync(dto);

        var userId = _twoFactorStore.PeekLoginToken(dto.TwoFactorToken)
                     ?? throw new BadRequestException("Сессия двухфакторной аутентификации истекла. Войдите заново.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || !user.TwoFactorEnabled)
            throw new BadRequestException("Двухфакторная аутентификация недоступна для этого пользователя.");

        var emailCode = _twoFactorStore.IssueEmailCode(userId);

        // В учебных целях реальная почта не отправляется — код пишется в лог и возвращается в data
        // (как reset-токен в ForgotPasswordAsync). В проде здесь был бы вызов почтового сервиса.
        _logger.LogInformation("2FA email-код для {Email}: {Code}", user.Email, emailCode);
        return new Response<string>(emailCode);
    }

    public async Task<Response<List<string>>> RegenerateBackupCodesAsync()
    {
        var userId = _currentUser.GetRequiredUserId();
        var user = await _userManager.FindByIdAsync(userId)
                   ?? throw new NotFoundException("Пользователь не найден.");

        if (!user.TwoFactorEnabled)
            throw new BadRequestException("Сначала включите двухфакторную аутентификацию.");

        var backupCodes = await ReplaceBackupCodesAsync(userId);
        return new Response<List<string>>(backupCodes);
    }

    /// <summary>Проверяет резервный код и помечает его использованным (одноразовый).</summary>
    private async Task<bool> VerifyAndConsumeBackupCodeAsync(string userId, string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        var hash = BackupCodeHasher.Hash(code);
        var backup = await _context.BackupCodes
            .FirstOrDefaultAsync(b => b.UserId == userId && !b.IsUsed && b.CodeHash == hash);
        if (backup is null)
            return false;

        backup.IsUsed = true;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>Удаляет прежние резервные коды пользователя и создаёт новую пачку; возвращает plaintext.</summary>
    private async Task<List<string>> ReplaceBackupCodesAsync(string userId)
    {
        _context.BackupCodes.RemoveRange(_context.BackupCodes.Where(b => b.UserId == userId));

        var codes = BackupCodeHasher.GenerateCodes(BackupCodesCount);
        var now = DateTime.UtcNow;
        foreach (var code in codes)
        {
            _context.BackupCodes.Add(new BackupCode
            {
                UserId = userId,
                CodeHash = BackupCodeHasher.Hash(code),
                IsUsed = false,
                CreatedAt = now
            });
        }

        await _context.SaveChangesAsync();
        return codes;
    }
}
