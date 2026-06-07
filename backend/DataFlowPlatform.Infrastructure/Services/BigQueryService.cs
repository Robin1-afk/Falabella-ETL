// Autenticación: Application Default Credentials (ADC)
// Configurar con: gcloud auth application-default login
// O setear la variable de entorno: GOOGLE_APPLICATION_CREDENTIALS=ruta/al/key.json

using System.Text.Json;
using Google.Apis.Bigquery.v2.Data;
using Google.Cloud.BigQuery.V2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DataFlowPlatform.Infrastructure.Services;

public class BigQueryService
{
    private readonly string  _projectId;
    private readonly string  _datasetId;
    private readonly string  _tableId;
    private readonly ILogger<BigQueryService> _logger;

    // Schema de la tabla en BigQuery
    private static readonly TableSchema Schema = new TableSchema
    {
        Fields =
        [
            new TableFieldSchema { Name = "pipeline_id",   Type = "INTEGER", Mode = "REQUIRED" },
            new TableFieldSchema { Name = "pipeline_name", Type = "STRING",  Mode = "REQUIRED" },
            new TableFieldSchema { Name = "data",          Type = "STRING",  Mode = "REQUIRED" },
            new TableFieldSchema { Name = "loaded_at",     Type = "TIMESTAMP",Mode = "REQUIRED" }
        ]
    };

    public BigQueryService(IConfiguration config, ILogger<BigQueryService> logger)
    {
        _projectId = config["BigQuery:ProjectId"] ?? throw new InvalidOperationException("BigQuery:ProjectId no configurado.");
        _datasetId = config["BigQuery:DatasetId"] ?? throw new InvalidOperationException("BigQuery:DatasetId no configurado.");
        _tableId   = config["BigQuery:TableId"]   ?? throw new InvalidOperationException("BigQuery:TableId no configurado.");
        _logger    = logger;
    }

    // Crea el dataset y la tabla si no existen (idempotente)
    public async Task EnsureTableExistsAsync()
    {
        var client = await BigQueryClient.CreateAsync(_projectId);

        // Dataset
        var dataset = await client.GetOrCreateDatasetAsync(_datasetId);

        // Tabla con schema definido
        await dataset.GetOrCreateTableAsync(_tableId, Schema);

        _logger.LogInformation("BigQuery: dataset '{D}' y tabla '{T}' verificados.", _datasetId, _tableId);
    }

    // Inserta las filas en batch; cada fila lleva el JSON de los datos originales
    public async Task InsertRowsAsync(
        int    pipelineId,
        string pipelineName,
        IEnumerable<string> dataJsonRows)
    {
        var client = await BigQueryClient.CreateAsync(_projectId);
        var table  = client.GetTable(_datasetId, _tableId);

        var now  = DateTime.UtcNow;
        var rows = dataJsonRows.Select(json => new BigQueryInsertRow
        {
            { "pipeline_id",   pipelineId   },
            { "pipeline_name", pipelineName },
            { "data",          json         },
            { "loaded_at",     now          }
        }).ToList();

        if (rows.Count == 0) return;

        await table.InsertRowsAsync(rows);

        _logger.LogInformation("BigQuery: {N} filas insertadas para pipeline {Id}.", rows.Count, pipelineId);
    }
}
