namespace DataFlowPlatform.Domain.Entities.Auth;

public class Role
{
    // Clave primaria autogenerada por la base de datos
    public int Id { get; set; }

    // Nombre único del rol (ej. "Administrador", "Operador")
    public string Name { get; set; } = string.Empty;

    // Indica si el rol está habilitado en el sistema
    public bool IsActive { get; set; } = true;

    // Usuarios asignados a este rol
    public ICollection<User> Users { get; set; } = [];

    // Vistas accesibles para este rol
    public ICollection<RoleView> RoleViews { get; set; } = [];
}
