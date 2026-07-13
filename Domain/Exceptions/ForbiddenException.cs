namespace Domain.Exceptions;

/// <summary>
/// Доступ запрещён (403). Бросается при попытке изменить/удалить чужой ресурс
/// (пост, комментарий, сторис, сообщение). Маппится middleware в статус 403.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}
