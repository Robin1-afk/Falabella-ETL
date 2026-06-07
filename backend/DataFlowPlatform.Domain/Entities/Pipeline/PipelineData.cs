namespace DataFlowPlatform.Domain.Entities.Pipeline;

public class PipelineData
{
    // Clave primaria BIGINT para soportar volúmenes altos de filas
    public long Id { get; set; }

    // FK al pipeline que generó esta fila
    public int PipelineId { get; set; }

    // Fila original del CSV serializada como JSON (todas las columnas)
    public string Data { get; set; } = string.Empty;

    // Fecha y hora en que la fila fue insertada en la plataforma
    public DateTime LoadedAt { get; set; } = DateTime.UtcNow;

    // Pipeline al que pertenece esta fila
    public Pipeline Pipeline { get; set; } = null!;
}
