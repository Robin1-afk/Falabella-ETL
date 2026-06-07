namespace DataFlowPlatform.API.DTOs.Pipelines;

public class PipelineCreateDto
{
    // Nombre descriptivo del pipeline ETL
    public string Name { get; set; } = string.Empty;

    // Descripción opcional del propósito del pipeline
    public string? Description { get; set; }

    // Tipo de fuente: CSV | Excel | API | Database
    public string SourceType { get; set; } = string.Empty;

    // Expresión CRON para ejecución automática; null = solo manual
    public string? Schedule { get; set; }
}
