namespace DataFlowPlatform.API.DTOs.Auth;

public class LoginRequestDto
{
    // Correo electrónico del usuario
    public string Email { get; set; } = string.Empty;

    // Contraseña en texto plano; se verifica contra el hash almacenado
    public string Password { get; set; } = string.Empty;
}
