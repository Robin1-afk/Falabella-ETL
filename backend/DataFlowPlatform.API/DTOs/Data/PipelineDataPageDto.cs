namespace DataFlowPlatform.API.DTOs.Data;

public class PipelineDataPageDto
{
    // Página actual solicitada (base 1)
    public int  Page         { get; set; }
    public int  PageSize     { get; set; }

    // long porque pipeline_data usa BIGINT como PK y puede tener millones de filas
    public long TotalRecords { get; set; }
    public int  TotalPages   { get; set; }

    public List<PipelineDataItemDto> Items { get; set; } = [];
}
