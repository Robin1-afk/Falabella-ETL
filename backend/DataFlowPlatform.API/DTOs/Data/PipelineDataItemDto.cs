using System.Text.Json;

namespace DataFlowPlatform.API.DTOs.Data;

public class PipelineDataItemDto
{
    public long        Id         { get; set; }
    public int         PipelineId { get; set; }

    // JSON object deserializado desde la columna data de pipeline_data
    public JsonElement Data       { get; set; }

    public DateTime    LoadedAt   { get; set; }
}
