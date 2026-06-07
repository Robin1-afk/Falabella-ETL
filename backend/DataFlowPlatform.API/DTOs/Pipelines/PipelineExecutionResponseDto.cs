namespace DataFlowPlatform.API.DTOs.Pipelines;

public class PipelineExecutionResponseDto
{
    public int      Id             { get; set; }
    public int      PipelineId     { get; set; }

    // Pending | Running | Completed | Failed
    public string   Status         { get; set; } = string.Empty;

    public int      TotalRows      { get; set; }
    public int      SuccessRows    { get; set; }
    public int      ErrorRows      { get; set; }

    // FK y nombre del usuario que disparó la ejecución
    public int      ExecutedBy     { get; set; }
    public string   ExecutedByName { get; set; } = string.Empty;

    public DateTime  ExecutedAt    { get; set; }

    // Null mientras la ejecución sigue en curso
    public DateTime? FinishedAt    { get; set; }
}
