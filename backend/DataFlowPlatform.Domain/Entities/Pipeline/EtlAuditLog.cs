using DataFlowPlatform.Domain.Entities.Auth;

namespace DataFlowPlatform.Domain.Entities.Pipeline;

public class EtlAuditLog
{
    // Clave primaria autogenerada por la base de datos
    public int Id { get; set; }

    // FK al pipeline que generó este registro de auditoría
    public int PipelineId { get; set; }

    // Nombre del archivo procesado (incluye extensión y ruta relativa si aplica)
    public string FileName { get; set; } = string.Empty;

    // Total de filas leídas en el archivo
    public int TotalRows { get; set; }

    // Filas procesadas exitosamente
    public int SuccessRows { get; set; }

    // Filas que generaron error durante la transformación o carga
    public int ErrorRows { get; set; }

    // JSON libre con detalles extra: errores por fila, columnas rechazadas, etc.
    public string? Metadata { get; set; }

    // FK al usuario que ejecutó el proceso ETL
    public int ExecutedBy { get; set; }

    // Fecha y hora en que se procesó el archivo
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    // Pipeline asociado a este log de auditoría
    public Pipeline Pipeline { get; set; } = null!;

    // Usuario que procesó el archivo
    public User Executor { get; set; } = null!;
}
