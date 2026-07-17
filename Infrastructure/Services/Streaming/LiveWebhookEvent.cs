namespace Infrastructure.Services.Streaming;

/// <summary>
/// Разобранное и провалидированное событие вебхука от видео-провайдера. Несёт минимум, нужный
/// бэкенду для синхронизации состояния эфира: тип события, имя комнаты, идентификатор участника
/// и уникальный <see cref="Id"/> события (для идемпотентности).
/// </summary>
public sealed record LiveWebhookEvent(
    string Event,
    string? Room,
    string? ParticipantIdentity,
    string? Id);
