using Domain.DTOs.Account;
using Domain.DTOs.Session;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Responses;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.Options;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Реализация управления сессиями (access + refresh). Refresh-токены хранятся только хэшем; при каждом
/// обновлении выполняется ротация (старый инвалидируется) и reuse-detection (предъявление уже
/// ротированного/отозванного токена → отзыв всех сессий юзера и 401). Отзыв проверяется на каждом
/// авторизованном запросе через <see cref="ValidateAndTouchAsync"/>, что делает завершение сеанса
/// мгновенным. Id текущего юзера/сессии — из claims.
/// </summary>
public class SessionService : ISessionService
{
    private readonly DataContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITokenService _tokenService;
    private readonly UserManager<User> _userManager;
    private readonly IDeviceInfoService _deviceInfo;
    private readonly IGeoLocationService _geoLocation;
    private readonly ISessionActivityThrottle _activityThrottle;
    private readonly INotificationService _notificationService;
    private readonly JwtOptions _jwt;

    public SessionService(
        DataContext context,
        ICurrentUserService currentUser,
        ITokenService tokenService,
        UserManager<User> userManager,
        IDeviceInfoService deviceInfo,
        IGeoLocationService geoLocation,
        ISessionActivityThrottle activityThrottle,
        INotificationService notificationService,
        IOptions<JwtOptions> jwtOptions)
    {
        _context = context;
        _currentUser = currentUser;
        _tokenService = tokenService;
        _userManager = userManager;
        _deviceInfo = deviceInfo;
        _geoLocation = geoLocation;
        _activityThrottle = activityThrottle;
        _notificationService = notificationService;
        _jwt = jwtOptions.Value;
    }

    public async Task<AuthResultDto> CreateSessionAsync(User user, IEnumerable<string> roles, bool notifyOnNewDevice)
    {
        var now = DateTime.UtcNow;
        var device = _deviceInfo.GetCurrentDeviceInfo();
        var location = await _geoLocation.GetLocationAsync(device.IpAddress);

        // «Новое устройство» определяем по прежним сессиям ДО добавления текущей: если прежних нет —
        // это первый вход (в т.ч. сразу после регистрации), уведомление не шлём.
        var priorDevices = await _context.UserSessions
            .Where(s => s.UserId == user.Id)
            .Select(s => new { s.DeviceName, s.IpAddress })
            .ToListAsync();

        var isNewDevice = priorDevices.Count > 0
            && !priorDevices.Any(p => p.DeviceName == device.DeviceName && p.IpAddress == device.IpAddress);

        await EnforceSessionLimitAsync(user.Id, now);

        var refreshToken = RefreshTokenHasher.Generate();
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RefreshTokenHash = RefreshTokenHasher.Hash(refreshToken),
            DeviceName = device.DeviceName,
            DeviceType = device.DeviceType,
            Browser = device.Browser,
            OS = device.Os,
            IpAddress = device.IpAddress,
            Location = location,
            CreatedAt = now,
            LastActivityAt = now,
            ExpiresAt = now.AddDays(_jwt.RefreshTokenLifetimeDays),
            IsRevoked = false
        };

        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        if (notifyOnNewDevice && isNewDevice)
            await _notificationService.CreateNewLoginNotificationAsync(user.Id);

        var accessToken = _tokenService.GenerateAccessToken(user, roles, session.Id);
        return BuildAuthResult(accessToken, refreshToken, session.Id);
    }

    public async Task<Response<AuthResultDto>> RefreshAsync(RefreshTokenDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.RefreshToken))
            throw new BadRequestException("Refresh-токен обязателен.");

        var now = DateTime.UtcNow;
        var hash = RefreshTokenHasher.Hash(dto.RefreshToken);

        var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.RefreshTokenHash == hash);
        if (session is not null)
        {
            // Предъявлен токен от отозванной/истёкшей сессии — считаем компрометацией.
            if (session.IsRevoked || session.ExpiresAt <= now)
            {
                await RevokeAllForUserAsync(session.UserId);
                throw new UnauthorizedAccessException("Сессия недействительна. Войдите заново.");
            }

            var user = await _userManager.FindByIdAsync(session.UserId)
                       ?? throw new UnauthorizedAccessException("Пользователь не найден.");
            var roles = await _userManager.GetRolesAsync(user);

            // Ротация: старый refresh инвалидируется (уходит в Previous для reuse-detection), выдаётся новый.
            var newRefresh = RefreshTokenHasher.Generate();
            session.PreviousRefreshTokenHash = session.RefreshTokenHash;
            session.RefreshTokenHash = RefreshTokenHasher.Hash(newRefresh);
            session.LastActivityAt = now;
            session.ExpiresAt = now.AddDays(_jwt.RefreshTokenLifetimeDays);
            await _context.SaveChangesAsync();
            _activityThrottle.ShouldPersist(session.Id, now); // фиксируем момент, чтобы middleware не переписал сразу

            var accessToken = _tokenService.GenerateAccessToken(user, roles, session.Id);
            return new Response<AuthResultDto>(BuildAuthResult(accessToken, newRefresh, session.Id));
        }

        // Не нашли по текущему хэшу — возможно, это уже ротированный (переиспользуемый) токен.
        var reusedFrom = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.PreviousRefreshTokenHash == hash);
        if (reusedFrom is not null)
        {
            await RevokeAllForUserAsync(reusedFrom.UserId);
            throw new UnauthorizedAccessException("Обнаружено повторное использование токена. Все сессии завершены.");
        }

        throw new UnauthorizedAccessException("Недействительный refresh-токен.");
    }

    public async Task<Response<string>> LogoutAsync()
    {
        var currentId = _currentUser.GetRequiredUserId();
        var sessionId = _currentUser.SessionId;

        if (sessionId is not null)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId.Value && s.UserId == currentId);
            if (session is not null && !session.IsRevoked)
            {
                session.IsRevoked = true;
                session.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            _activityThrottle.Forget(sessionId.Value);
        }

        return new Response<string>("Вы вышли из системы.");
    }

    public async Task<Response<List<SessionDto>>> GetActiveSessionsAsync()
    {
        var currentId = _currentUser.GetRequiredUserId();
        var currentSessionId = _currentUser.SessionId;
        var now = DateTime.UtcNow;

        var sessions = await _context.UserSessions.AsNoTracking()
            .Where(s => s.UserId == currentId && !s.IsRevoked && s.ExpiresAt > now)
            .ToListAsync();

        var dtos = sessions
            .Select(s => new SessionDto
            {
                Id = s.Id,
                DeviceName = s.DeviceName,
                DeviceType = s.DeviceType.ToString(),
                Browser = s.Browser,
                Os = s.OS,
                IpAddress = s.IpAddress,
                Location = s.Location,
                CreatedAt = s.CreatedAt,
                LastActivityAt = s.LastActivityAt,
                IsCurrent = currentSessionId is not null && s.Id == currentSessionId.Value
            })
            // Текущая сессия первой, затем по последней активности убыв.
            .OrderByDescending(s => s.IsCurrent)
            .ThenByDescending(s => s.LastActivityAt)
            .ToList();

        return new Response<List<SessionDto>>(dtos);
    }

    public async Task<Response<bool>> RevokeSessionAsync(Guid? sessionId)
    {
        if (sessionId is null || sessionId.Value == Guid.Empty)
            throw new BadRequestException("Некорректный Id сессии.");

        var currentId = _currentUser.GetRequiredUserId();

        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId.Value)
            ?? throw new NotFoundException("Сессия не найдена.");

        // Нельзя завершить чужую сессию.
        if (session.UserId != currentId)
            throw new ForbiddenException("Нет доступа к этой сессии.");

        if (!session.IsRevoked)
        {
            session.IsRevoked = true;
            session.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        _activityThrottle.Forget(session.Id);

        return new Response<bool>(true);
    }

    public async Task<Response<bool>> RevokeAllOthersAsync()
    {
        var currentId = _currentUser.GetRequiredUserId();
        var currentSessionId = _currentUser.SessionId;

        var query = _context.UserSessions.Where(s => s.UserId == currentId && !s.IsRevoked);
        if (currentSessionId is not null)
            query = query.Where(s => s.Id != currentSessionId.Value);

        await RevokeAsync(query);
        return new Response<bool>(true);
    }

    public async Task<bool> ValidateAndTouchAsync(Guid sessionId)
    {
        var now = DateTime.UtcNow;

        var state = await _context.UserSessions
            .Where(s => s.Id == sessionId)
            .Select(s => new { s.IsRevoked, s.ExpiresAt })
            .FirstOrDefaultAsync();

        if (state is null || state.IsRevoked || state.ExpiresAt <= now)
            return false;

        // Обновляем активность не чаще окна троттлинга — не бьём БД на каждом запросе.
        if (_activityThrottle.ShouldPersist(sessionId, now))
        {
            await _context.UserSessions
                .Where(s => s.Id == sessionId)
                .ExecuteUpdateAsync(set => set.SetProperty(s => s.LastActivityAt, now));
        }

        return true;
    }

    public Task RevokeAllForUserAsync(string userId)
    {
        var query = _context.UserSessions.Where(s => s.UserId == userId && !s.IsRevoked);
        return RevokeAsync(query);
    }

    public Task RevokeAllOtherForCurrentAsync(string userId)
    {
        var currentSessionId = _currentUser.SessionId;

        var query = _context.UserSessions.Where(s => s.UserId == userId && !s.IsRevoked);
        if (currentSessionId is not null)
            query = query.Where(s => s.Id != currentSessionId.Value);

        return RevokeAsync(query);
    }

    /// <summary>Помечает все сессии выборки отозванными и чистит их троттлинг-записи.</summary>
    private async Task RevokeAsync(IQueryable<UserSession> query)
    {
        var sessions = await query.ToListAsync();
        if (sessions.Count == 0)
            return;

        var now = DateTime.UtcNow;
        foreach (var session in sessions)
        {
            session.IsRevoked = true;
            session.RevokedAt = now;
            _activityThrottle.Forget(session.Id);
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Соблюдает лимит активных сессий: если их уже <c>MaxActiveSessionsPerUser</c> или больше,
    /// отзывает самые старые по активности, освобождая место под новую (0 — без ограничения).
    /// </summary>
    private async Task EnforceSessionLimitAsync(string userId, DateTime now)
    {
        var max = _jwt.MaxActiveSessionsPerUser;
        if (max <= 0)
            return;

        var active = await _context.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > now)
            .OrderBy(s => s.LastActivityAt)
            .ToListAsync();

        var excess = active.Count - (max - 1); // оставляем место под создаваемую сессию
        for (var i = 0; i < excess && i < active.Count; i++)
        {
            active[i].IsRevoked = true;
            active[i].RevokedAt = now;
            _activityThrottle.Forget(active[i].Id);
        }
        // SaveChanges выполнит вызывающий метод вместе с добавлением новой сессии.
    }

    private AuthResultDto BuildAuthResult(string accessToken, string refreshToken, Guid sessionId) => new()
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        ExpiresIn = _jwt.AccessTokenLifetimeMinutes * 60,
        SessionId = sessionId
    };
}
