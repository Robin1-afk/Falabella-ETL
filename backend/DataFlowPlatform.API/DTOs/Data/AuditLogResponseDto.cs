namespace DataFlowPlatform.API.DTOs.Data;

public class AuditLogResponseDto
{
    public int      Id             { get; set; }

    // FK y nombre del pipeline que originó el log
    public int      PipelineId     { get; set; }
    public string   PipelineName   { get; set; } = string.Empty;

    // Nombre del archivo procesado
    public string   FileName       { get; set; } = string.Empty;

    public int      TotalRows      { get; set; }
    public int      SuccessRows    { get; set; }
    public int      ErrorRows      { get; set; }

    // JSON con contexto de la ejecución (source, durationMs, executionId…)
    public string?  Metadata       { get; set; }

    // FK y nombre del usuario que ejecutó el proceso
    public int      ExecutedBy     { get; set; }
    public string   ExecutedByName { get; set; } = string.Empty;

    public DateTime ExecutedAt     { get; set; }
}
