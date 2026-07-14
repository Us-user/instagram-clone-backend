using Microsoft.AspNetCore.Http;

namespace Domain.DTOs.GroupChat;

/// <summary>Обновление инфо группы (multipart/form-data): название и/или аватар. Только Admin.</summary>
public class UpdateGroupInfoDto
{
    public string? Name { get; set; }
    public IFormFile? Avatar { get; set; }
}
