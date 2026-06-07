namespace DataFlowPlatform.API.DTOs.Roles;

public class RoleResponseDto
{
    public int    Id       { get; set; }
    public string Name     { get; set; } = string.Empty;
    public bool   IsActive { get; set; }
}
