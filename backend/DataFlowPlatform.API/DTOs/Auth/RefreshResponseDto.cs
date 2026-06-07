namespace DataFlowPlatform.API.DTOs.Auth;

public class RefreshResponseDto
{
    // Nuevo access token JWT; el refresh token permanece sin cambios
    public string AccessToken { get; set; } = string.Empty;
}
