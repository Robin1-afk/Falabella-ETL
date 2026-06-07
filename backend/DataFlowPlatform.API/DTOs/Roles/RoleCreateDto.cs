namespace DataFlowPlatform.API.DTOs.Roles;

public class RoleCreateDto
{
    // Nombre único del rol (ej. "Operador", "Auditor")
    public string Name { get; set; } = string.Empty;

    // Estado inicial del rol
    public bool IsActive { get; set; } = true;
}
