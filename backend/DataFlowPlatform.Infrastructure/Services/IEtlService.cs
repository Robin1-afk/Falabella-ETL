namespace DataFlowPlatform.Infrastructure.Services;

// Resultado devuelto tras procesar un archivo CSV en un pipeline
public record EtlResult(
    int       ExecutionId,
    string    Status,
    int       TotalRows,
    int       SuccessRows,
    int       ErrorRows,
    DateTime  ExecutedAt,
    DateTime  FinishedAt);

public interface IEtlService
{
    // Ejecuta el proceso ETL completo: lee el CSV, valida, transforma e inserta
    Task<EtlResult> RunAsync(
        int               pipelineId,
        int               executedBy,
        Stream            csvStream,
        string            fileName,
        CancellationToken ct = default);
}
