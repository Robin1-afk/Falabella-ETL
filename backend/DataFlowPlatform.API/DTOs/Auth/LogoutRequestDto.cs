namespace DataFlowPlatform.API.DTOs.Auth;

public class LogoutRequestDto
{
    // Refresh token de la sesión a invalidar (is_active = false)
    public string RefreshToken { get; set; } = string.Empty;
}
