namespace Domain.DTOs.Account;

/// <summary>Тело запроса <c>/Account/refresh-token</c>: refresh-токен, полученный при логине.</summary>
public class RefreshTokenDto
{
    public string RefreshToken { get; set; } = string.Empty;
}
