namespace Domain.DTOs.Live;

/// <summary>Тело запроса смены заголовка эфира: <c>PUT /Live/update-title?streamId</c> — <c>{ title }</c>.</summary>
public class UpdateLiveTitleDto
{
    public string? Title { get; set; }
}
