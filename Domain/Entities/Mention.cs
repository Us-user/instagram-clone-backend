using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Упоминание пользователя (@username) в посте/комментарии/ответе на сторис (Phase 13).
/// Ссылка на объект полиморфная: <see cref="EntityType"/> + <see cref="EntityId"/>.
/// Создаётся только если упоминающий входит в разрешённую адресатом аудиторию
/// (<see cref="WhoCanMention"/>) и между ними нет блокировки.
/// </summary>
public class Mention
{
    public int Id { get; set; }

    /// <summary>Кого упомянули (FK на AspNetUsers).</summary>
    public string MentionedUserId { get; set; } = string.Empty;

    /// <summary>Кто упомянул (FK на AspNetUsers).</summary>
    public string AuthorUserId { get; set; } = string.Empty;

    public MentionEntityType EntityType { get; set; }

    /// <summary>Id объекта (поста/коммента/ответа на сторис) — полиморфная ссылка.</summary>
    public int EntityId { get; set; }

    public DateTime CreatedAt { get; set; }

    public User? MentionedUser { get; set; }
    public User? AuthorUser { get; set; }
}
