namespace DataFlowPlatform.API.DTOs.Users;

public class UserUpdateDto
{
    // Nombre completo o de pantalla
    public string Name { get; set; } = string.Empty;

    // Correo electrónico; debe ser único excluyendo al propio usuario
    public string Email { get; set; } = string.Empty;

    // Nueva contraseña en texto plano; null o vacío = no se cambia
    public string? Password { get; set; }

    // Rol que se asignará al usuario
    public int RoleId { get; set; }

    // Permite activar o desactivar la cuenta
    public bool IsActive { get; set; }
}
