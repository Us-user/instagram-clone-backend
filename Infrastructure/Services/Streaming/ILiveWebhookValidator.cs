namespace Infrastructure.Services.Streaming;

/// <summary>
/// Проверяет подпись входящего вебхука провайдера и разбирает его тело в <see cref="LiveWebhookEvent"/>.
/// Возвращает <c>null</c>, если подпись недействительна/отсутствует (запрос должен быть отклонён) или тело
/// нераспознано. Реализация зависит от активного провайдера (LiveKit — обязательная проверка подписи).
/// </summary>
public interface ILiveWebhookValidator
{
    /// <summary>
    /// Валидирует и разбирает вебхук. <paramref name="rawBody"/> — тело как есть (для сверки хэша),
    /// <paramref name="authHeader"/> — заголовок Authorization с подписанным токеном.
    /// </summary>
    LiveWebhookEvent? Validate(string rawBody, string? authHeader);
}
