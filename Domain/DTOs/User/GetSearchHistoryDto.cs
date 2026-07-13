namespace Domain.DTOs.User;

/// <summary>Запись истории текстового поиска.</summary>
public class GetSearchHistoryDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
