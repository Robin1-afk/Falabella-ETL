namespace DataFlowPlatform.API.DTOs.Pipelines;

public class PipelineResponseDto
{
    public int     Id            { get; set; }
    public string  Name          { get; set; } = string.Empty;
    public string? Description   { get; set; }
    public string  SourceType    { get; set; } = string.Empty;
    public string? Schedule      { get; set; }

    // Active | Inactive | Archived
    public string  Status        { get; set; } = string.Empty;

    // FK y nombre del usuario que creó el pipeline
    public int     CreatedBy     { get; set; }
    public string  CreatedByName { get; set; } = string.Empty;

    public DateTime CreatedAt    { get; set; }
}
