namespace Domain.Enums;

/// <summary>
/// Тип сообщения (§7/§8). Общий для групповых и (в Phase 16) личных сообщений.
/// <see cref="System"/> — служебное сообщение группы без отправителя.
/// </summary>
public enum MessageType
{
    /// <summary>Текстовое сообщение.</summary>
    Text = 0,

    /// <summary>Вложение-изображение.</summary>
    Image = 1,

    /// <summary>Вложение-файл (не изображение).</summary>
    File = 2,

    /// <summary>Голосовое сообщение (Phase 16).</summary>
    Voice = 3,

    /// <summary>Служебное сообщение группы (создал/добавил/удалил/вышел/сменил название/назначен админом).</summary>
    System = 4
}
