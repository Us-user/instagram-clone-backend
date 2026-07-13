namespace Domain.Exceptions;

/// <summary>
/// Некорректный запрос (400). Бросается бизнес-логикой при нарушении входных условий
/// (например, недопустимое расширение/размер файла). Маппится middleware в статус 400.
/// </summary>
public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
}
