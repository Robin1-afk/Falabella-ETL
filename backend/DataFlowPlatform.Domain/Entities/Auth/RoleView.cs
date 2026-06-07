namespace DataFlowPlatform.Domain.Entities.Auth;

public class RoleView
{
    // Clave primaria autogenerada por la base de datos
    public int Id { get; set; }

    // FK al rol que recibe el permiso
    public int RoleId { get; set; }

    // FK a la vista sobre la que se otorga acceso
    public int ViewId { get; set; }

    // Indica si el permiso está activo; permite revocar sin eliminar el registro
    public bool IsActive { get; set; } = true;

    // Rol al que pertenece este permiso
    public Role Role { get; set; } = null!;

    // Vista a la que se concede acceso
    public View View { get; set; } = null!;
}
