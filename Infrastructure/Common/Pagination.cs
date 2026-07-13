namespace Infrastructure.Common;

/// <summary>
/// Нормализация параметров пагинации: подставляет разумные значения по умолчанию
/// и ограничивает размер страницы, чтобы один запрос не тянул всю таблицу.
/// </summary>
public static class Pagination
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    /// <summary>
    /// Приводит <paramref name="pageNumber"/>/<paramref name="pageSize"/> к валидным значениям:
    /// номер страницы ≥ 1, размер в диапазоне [1; <see cref="MaxPageSize"/>].
    /// </summary>
    public static (int Page, int Size) Normalize(int? pageNumber, int? pageSize)
    {
        var page = pageNumber is > 0 ? pageNumber.Value : 1;
        var size = pageSize is > 0 ? pageSize.Value : DefaultPageSize;
        if (size > MaxPageSize)
            size = MaxPageSize;
        return (page, size);
    }
}
