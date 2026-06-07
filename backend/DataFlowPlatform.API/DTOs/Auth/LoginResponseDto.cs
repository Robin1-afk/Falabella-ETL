namespace DataFlowPlatform.API.DTOs.Auth;

public class LoginResponseDto
{
    // JWT de corta duración para autenticar requests (15 min)
    public string AccessToken { get; set; } = string.Empty;

    // Token opaco de larga duración para renovar el access token (30 días)
    public string RefreshToken { get; set; } = string.Empty;

    // Datos básicos del usuario autenticado
    public int    UserId   { get; set; }
    public string Name     { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;
    public int    RoleId   { get; set; }
    public string RoleName { get; set; } = string.Empty;
}
