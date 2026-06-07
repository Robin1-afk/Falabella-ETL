namespace DataFlowPlatform.API.DTOs.Users;

public class UserResponseDto
{
    public int    Id       { get; set; }
    public string Name     { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;

    // FK y nombre del rol asignado
    public int    RoleId   { get; set; }
    public string RoleName { get; set; } = string.Empty;

    public bool   IsActive { get; set; }
}
