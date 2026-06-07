namespace DataFlowPlatform.Domain.Entities.Auth;

public class View
{
    // Clave primaria autogenerada por la base de datos
    public int Id { get; set; }

    // Nombre único del módulo o página del sistema (ej. "Dashboard", "Pipelines")
    public string Name { get; set; } = string.Empty;

    // Descripción funcional de la vista; puede ser nula
    public string? Description { get; set; }

    // Indica si la vista está disponible para asignación
    public bool IsActive { get; set; } = true;

    // Roles que tienen acceso a esta vista
    public ICollection<RoleView> RoleViews { get; set; } = [];
}
