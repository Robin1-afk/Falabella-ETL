namespace DataFlowPlatform.API.DTOs.Pipelines;

public class PipelineRunResponseDto
{
    // ID del registro creado en pipeline_executions
    public int      ExecutionId  { get; set; }

    // Completed | Failed
    public string   Status       { get; set; } = string.Empty;

    public int      TotalRows    { get; set; }
    public int      SuccessRows  { get; set; }
    public int      ErrorRows    { get; set; }

    public DateTime ExecutedAt   { get; set; }
    public DateTime FinishedAt   { get; set; }
}
