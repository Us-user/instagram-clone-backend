namespace Domain.Exceptions;

/// <summary>
/// Ресурс не найден (404). Маппится middleware в статус 404.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
