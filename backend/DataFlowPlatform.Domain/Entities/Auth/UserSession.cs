namespace DataFlowPlatform.Domain.Entities.Auth;

public class UserSession
{
    // Clave primaria autogenerada por la base de datos
    public int Id { get; set; }

    // FK al usuario dueño de la sesión
    public int UserId { get; set; }

    // Token JWT completo emitido en el login
    public string Token { get; set; } = string.Empty;

    // Hash SHA-256 del token; columna computada en SQL Server usada para indexar
    public string TokenHash { get; set; } = string.Empty;

    // Indica si la sesión sigue vigente (false = logout o expirada)
    public bool IsActive { get; set; } = true;

    // Fecha y hora en que se emitió el token
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Usuario dueño de la sesión
    public User User { get; set; } = null!;
}
