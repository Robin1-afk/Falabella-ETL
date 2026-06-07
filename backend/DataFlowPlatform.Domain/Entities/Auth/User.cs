namespace DataFlowPlatform.Domain.Entities.Auth;

public class User
{
    // Clave primaria autogenerada por la base de datos
    public int Id { get; set; }

    // Nombre completo o de pantalla del usuario
    public string Name { get; set; } = string.Empty;

    // Correo electrónico; usado como identificador de login
    public string Email { get; set; } = string.Empty;

    // Hash bcrypt de la contraseña; nunca se almacena en texto plano
    public string Password { get; set; } = string.Empty;

    // FK hacia el rol asignado al usuario
    public int RoleId { get; set; }

    // Indica si la cuenta está habilitada
    public bool IsActive { get; set; } = true;

    // Rol asignado al usuario
    public Role Role { get; set; } = null!;

    // Sesiones activas del usuario
    public ICollection<UserSession> Sessions { get; set; } = [];
}
