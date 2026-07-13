namespace Domain.Responses;

/// <summary>
/// Единый формат ответа API: полезная нагрузка, список ошибок и HTTP-статус.
/// </summary>
public class Response<T>
{
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public int StatusCode { get; set; }

    /// <summary>Пустой конструктор — для десериализации.</summary>
    public Response() { }

    /// <summary>Успешный ответ (200) с данными.</summary>
    public Response(T data)
    {
        Data = data;
        StatusCode = 200;
    }

    /// <summary>Ответ с явным статусом и данными.</summary>
    public Response(int statusCode, T data)
    {
        StatusCode = statusCode;
        Data = data;
    }

    /// <summary>Ответ-ошибка с одним сообщением.</summary>
    public Response(int statusCode, string error)
    {
        StatusCode = statusCode;
        Errors = new List<string> { error };
    }

    /// <summary>Ответ-ошибка со списком сообщений.</summary>
    public Response(int statusCode, List<string> errors)
    {
        StatusCode = statusCode;
        Errors = errors;
    }
}
