namespace Domain.DTOs.User;

/// <summary>Пользователь для списков/поиска.</summary>
public class GetUserDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
}
