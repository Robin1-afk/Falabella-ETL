using DataFlowPlatform.Domain.Entities.Auth;

namespace DataFlowPlatform.Domain.Entities.Pipeline;

public class Pipeline
{
    // Clave primaria autogenerada por la base de datos
    public int Id { get; set; }

    // Nombre descriptivo del pipeline ETL
    public string Name { get; set; } = string.Empty;

    // Descripción opcional del propósito del pipeline
    public string? Description { get; set; }

    // Tipo de fuente de datos: CSV | Excel | API | Database
    public string SourceType { get; set; } = string.Empty;

    // Expresión CRON para ejecución programada; null indica ejecución manual
    public string? Schedule { get; set; }

    // Estado actual del pipeline: Active | Inactive | Archived
    public string Status { get; set; } = "Active";

    // FK al usuario que creó el pipeline
    public int CreatedBy { get; set; }

    // Fecha y hora de creación del pipeline
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Usuario que creó el pipeline
    public User Creator { get; set; } = null!;

    // Historial de ejecuciones de este pipeline
    public ICollection<PipelineExecution> Executions { get; set; } = [];

    // Entradas de auditoría asociadas a este pipeline
    public ICollection<EtlAuditLog> AuditLogs { get; set; } = [];
}
