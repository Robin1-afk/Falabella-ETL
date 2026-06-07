namespace DataFlowPlatform.API.DTOs.Views;

public class ViewResponseDto
{
    public int     Id          { get; set; }
    public string  Name        { get; set; } = string.Empty;

    // Descripción funcional de la vista; puede ser nula
    public string? Description { get; set; }

    public bool    IsActive    { get; set; }
}
