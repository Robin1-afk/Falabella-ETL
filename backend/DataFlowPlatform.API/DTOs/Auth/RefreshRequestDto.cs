namespace DataFlowPlatform.API.DTOs.Auth;

public class RefreshRequestDto
{
    // Refresh token almacenado en user_sessions que se usará para emitir un nuevo access token
    public string RefreshToken { get; set; } = string.Empty;
}
