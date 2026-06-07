using DataFlowPlatform.Domain.Entities.Auth;

namespace DataFlowPlatform.Domain.Entities.Pipeline;

public class PipelineExecution
{
    // Clave primaria autogenerada por la base de datos
    public int Id { get; set; }

    // FK al pipeline que originó esta ejecución
    public int PipelineId { get; set; }

    // Estado de la ejecución: Pending | Running | Completed | Failed
    public string Status { get; set; } = string.Empty;

    // Total de filas encontradas en el archivo o fuente
    public int TotalRows { get; set; }

    // Filas que se procesaron e insertaron sin errores
    public int SuccessRows { get; set; }

    // Filas que fallaron durante el procesamiento
    public int ErrorRows { get; set; }

    // FK al usuario que disparó la ejecución
    public int ExecutedBy { get; set; }

    // Fecha y hora en que inició la ejecución
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    // Fecha y hora en que finalizó; null mientras la ejecución está en curso
    public DateTime? FinishedAt { get; set; }

    // Pipeline al que pertenece esta ejecución
    public Pipeline Pipeline { get; set; } = null!;

    // Usuario que ejecutó el pipeline
    public User Executor { get; set; } = null!;
}
