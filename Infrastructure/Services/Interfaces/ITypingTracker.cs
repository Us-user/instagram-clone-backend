using Domain.DTOs.Presence;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Эфемерное состояние «кто печатает» в групповых чатах (§1). Живёт в памяти (singleton), в БД
/// не пишется. Нужен, чтобы группе можно было отдать весь актуальный список печатающих
/// («X и ещё N печатают…»), а не отдельные события. Записи протухают по короткому TTL.
/// </summary>
public interface ITypingTracker
{
    /// <summary>
    /// Отмечает/продлевает набор пользователя в группе и возвращает актуальный список печатающих
    /// (протухшие записи отсеиваются). Повторный вызов тем же пользователем обновляет его TTL/kind.
    /// </summary>
    List<TypingUserDto> Update(int groupChatId, string userId, string userName, string kind);
}
