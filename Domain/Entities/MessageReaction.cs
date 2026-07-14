using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Реакция-эмодзи на сообщение (§8). Полиморфна по контексту: <see cref="MessageId"/> указывает на
/// <see cref="Message"/> (Direct) либо <see cref="GroupMessage"/> (Group) в зависимости от
/// <see cref="MessageContext"/>, поэтому жёсткого FK на таблицу сообщений нет — целостность
/// поддерживается уникальным индексом <c>(MessageId, MessageContext, UserId)</c> и ручной очисткой
/// при удалении сообщения/чата. Один юзер = одна реакция на сообщение.
/// </summary>
public class MessageReaction
{
    public int Id { get; set; }

    /// <summary>Id сообщения (личного или группового — см. <see cref="MessageContext"/>).</summary>
    public int MessageId { get; set; }

    public MessageContext MessageContext { get; set; }

    public string UserId { get; set; } = string.Empty;

    /// <summary>Эмодзи реакции (например ❤️ 😂 🔥).</summary>
    public string Emoji { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
}
