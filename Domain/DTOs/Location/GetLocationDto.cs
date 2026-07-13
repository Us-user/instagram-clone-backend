namespace Domain.DTOs.Location;

/// <summary>Локация для чтения.</summary>
public class GetLocationDto
{
    public int Id { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}
