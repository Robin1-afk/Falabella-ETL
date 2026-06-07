namespace DataFlowPlatform.API.DTOs.Data;

public class PipelineStatsDto
{
    public int       PipelineId      { get; set; }
    public string    PipelineName    { get; set; } = string.Empty;

    // Total de filas almacenadas en pipeline_data para este pipeline
    public long      TotalRecords    { get; set; }

    // Rango temporal de las cargas
    public DateTime? FirstLoadedAt   { get; set; }
    public DateTime? LastLoadedAt    { get; set; }

    // Cantidad de entradas en pipeline_executions
    public int       TotalExecutions { get; set; }
}
