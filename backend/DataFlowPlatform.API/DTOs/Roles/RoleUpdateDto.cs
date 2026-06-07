namespace DataFlowPlatform.API.DTOs.Roles;

public class RoleUpdateDto
{
    // Nuevo nombre del rol
    public string Name { get; set; } = string.Empty;

    // Permite activar o desactivar el rol
    public bool IsActive { get; set; }
}
