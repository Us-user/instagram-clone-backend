namespace Domain.Enums;

/// <summary>
/// Способ подтверждения второго фактора при входе (§11): приложение-аутентификатор (TOTP),
/// код на email или одноразовый резервный код.
/// </summary>
public enum TwoFactorMethod
{
    /// <summary>TOTP из приложения (Google Authenticator/Authy) — основной способ.</summary>
    Totp = 0,

    /// <summary>6-значный код, высланный на email (резервный способ).</summary>
    Email = 1,

    /// <summary>Одноразовый резервный код из выданной при включении пачки.</summary>
    Backup = 2
}
