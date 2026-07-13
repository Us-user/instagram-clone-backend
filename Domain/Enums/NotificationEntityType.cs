namespace Domain.Enums;

/// <summary>
/// Тип объекта, к которому относится уведомление (полиморфная ссылка вместе с
/// <c>EntityId</c>). Позволяет фронту построить переход к посту/комменту/сторис/профилю.
/// </summary>
public enum NotificationEntityType
{
    Post = 0,
    Comment = 1,
    Story = 2,
    User = 3
}
