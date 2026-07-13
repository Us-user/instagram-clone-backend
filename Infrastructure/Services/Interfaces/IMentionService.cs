using Domain.Enums;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Разбор упоминаний (@username) при сохранении поста/коммента/ответа на сторис (Phase 13):
/// поиск существующих юзеров, проверка блокировок и настройки «кто может упоминать»
/// (<see cref="WhoCanMention"/>), создание <see cref="Domain.Entities.Mention"/> и уведомления
/// <see cref="NotificationType.Mention"/>. На собственные упоминания уведомления не шлём.
/// </summary>
public interface IMentionService
{
    /// <summary>
    /// Обрабатывает упоминания из <paramref name="text"/> для объекта
    /// (<paramref name="entityType"/>, <paramref name="entityId"/>), автором которого является
    /// <paramref name="authorUserId"/>. Создаёт записи упоминаний (для разрешённых адресатов)
    /// и уведомления. Идемпотентности на уровне БД не нарушает (пара уникальна).
    /// <paramref name="suppressNotificationUserId"/> — для этого адресата запись
    /// <see cref="Domain.Entities.Mention"/> создаётся (кликабельная ссылка), но уведомление
    /// <see cref="NotificationType.Mention"/> не шлётся: используется при авто-подстановке
    /// <c>@username</c> в ответе на комментарий, где адресат уже получает <c>CommentReply</c>.
    /// </summary>
    Task ProcessMentionsAsync(
        string? text, string authorUserId, MentionEntityType entityType, int entityId,
        string? suppressNotificationUserId = null);
}
