namespace Domain.Enums;

/// <summary>
/// Тип устройства сессии (определяется по User-Agent при логине). Числовые коды фиксированы —
/// новые значения добавлять только в конец, чтобы не сдвинуть уже сохранённые в БД.
/// </summary>
public enum DeviceType
{
    /// <summary>Не удалось определить (пустой/нераспознанный User-Agent).</summary>
    Unknown = 0,

    /// <summary>Мобильное устройство (iOS/Android, мобильный браузер или приложение).</summary>
    Mobile = 1,

    /// <summary>Десктоп (нативное приложение/клиент на Windows/macOS/Linux).</summary>
    Desktop = 2,

    /// <summary>Веб-браузер на десктопе.</summary>
    Web = 3
}
