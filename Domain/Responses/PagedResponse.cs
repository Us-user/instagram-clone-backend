namespace Domain.Responses;

/// <summary>
/// Ответ для списков с пагинацией: данные + метаданные страницы.
/// </summary>
public class PagedResponse<T> : Response<T>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }

    public PagedResponse() { }

    /// <summary>Успешная страница данных.</summary>
    public PagedResponse(T data, int totalRecords, int pageNumber, int pageSize) : base(data)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalRecords = totalRecords;
        TotalPages = pageSize > 0 ? (int)Math.Ceiling(totalRecords / (double)pageSize) : 0;
    }

    /// <summary>Ответ-ошибка для пагинированного эндпоинта.</summary>
    public PagedResponse(int statusCode, string error) : base(statusCode, error) { }
}
