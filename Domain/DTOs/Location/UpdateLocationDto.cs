namespace Domain.DTOs.Location;

/// <summary>Обновление локации. Все поля обязательны.</summary>
public class UpdateLocationDto
{
    public int LocationId { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}
