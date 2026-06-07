namespace DataFlowPlatform.API.DTOs.Roles;

public class RoleViewAssignItemDto
{
    // Vista que se asigna o revoca
    public int ViewId { get; set; }

    // true = concede acceso, false = revoca sin eliminar el registro
    public bool IsActive { get; set; }
}
