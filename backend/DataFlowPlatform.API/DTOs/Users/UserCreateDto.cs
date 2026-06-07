namespace DataFlowPlatform.API.DTOs.Users;

public class UserCreateDto
{
    // Nombre completo o de pantalla del nuevo usuario
    public string Name { get; set; } = string.Empty;

    // Correo electrónico; debe ser único en la plataforma
    public string Email { get; set; } = string.Empty;

    // Contraseña en texto plano; se hashea con BCrypt antes de persistir
    public string Password { get; set; } = string.Empty;

    // Rol asignado al usuario
    public int RoleId { get; set; }

    // Estado inicial de la cuenta
    public bool IsActive { get; set; } = true;
}
