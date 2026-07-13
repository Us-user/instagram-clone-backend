namespace Domain.DTOs.Location;

/// <summary>Создание локации. Все поля обязательны.</summary>
public class AddLocationDto
{
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}
