using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using DataFlowPlatform.Domain.Entities.Pipeline;
using DataFlowPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DataFlowPlatform.Infrastructure.Services;

public class EtlService : IEtlService
{
    private readonly DataFlowPlatformDbContext _context;
    private readonly BigQueryService          _bigQuery;
    private readonly ILogger<EtlService>      _logger;

    public EtlService(
        DataFlowPlatformDbContext context,
        BigQueryService          bigQuery,
        ILogger<EtlService>      logger)
    {
        _context  = context;
        _bigQuery = bigQuery;
        _logger   = logger;
    }

    public async Task<EtlResult> RunAsync(
        int               pipelineId,
        int               executedBy,
        Stream            csvStream,
        string            fileName,
        CancellationToken ct = default)
    {
        var startedAt = DateTime.UtcNow;

        // ── 1. Registra la ejecución como Running ────────────────────────────────
        var execution = new PipelineExecution
        {
            PipelineId = pipelineId,
            Status     = "Running",
            ExecutedBy = executedBy,
            ExecutedAt = startedAt
        };
        _context.PipelineExecutions.Add(execution);
        await _context.SaveChangesAsync(ct);

        int total = 0, success = 0, errors = 0;
        var batch      = new List<PipelineData>();
        var jsonRows   = new List<string>();   // copia de los JSON para enviar a BigQuery
        string pipelineName;

        // Obtiene el nombre del pipeline para el log de BigQuery
        var pipeline = await _context.Pipelines.FindAsync([pipelineId], ct);
        pipelineName = pipeline?.Name ?? $"pipeline-{pipelineId}";

        try
        {
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord   = true,
                BadDataFound      = _ => errors++,
                MissingFieldFound = null
            };

            using var reader = new StreamReader(csvStream, leaveOpen: true);
            using var csv    = new CsvReader(reader, csvConfig);

            csv.Read();
            csv.ReadHeader();

            while (csv.Read())
            {
                total++;
                try
                {
                    var record = (IDictionary<string, object>)csv.GetRecord<dynamic>();

                    if (record.Values.Any(v => string.IsNullOrWhiteSpace(v?.ToString())))
                    {
                        errors++;
                        continue;
                    }

                    var json = JsonSerializer.Serialize(record);

                    batch.Add(new PipelineData
                    {
                        PipelineId = pipelineId,
                        Data       = json,
                        LoadedAt   = DateTime.UtcNow
                    });
                    jsonRows.Add(json);   // guarda para BigQuery
                    success++;
                }
                catch
                {
                    errors++;
                }
            }

            // ── 2. Inserta en SQL Server ─────────────────────────────────────────
            if (batch.Count > 0)
                _context.PipelineData.AddRange(batch);

            execution.Status = "Completed";
        }
        catch
        {
            execution.Status = "Failed";
        }

        var finishedAt = DateTime.UtcNow;

        execution.TotalRows   = total;
        execution.SuccessRows = success;
        execution.ErrorRows   = errors;
        execution.FinishedAt  = finishedAt;

        _context.EtlAuditLogs.Add(new EtlAuditLog
        {
            PipelineId  = pipelineId,
            FileName    = fileName,
            TotalRows   = total,
            SuccessRows = success,
            ErrorRows   = errors,
            Metadata    = JsonSerializer.Serialize(new
            {
                source      = "CSV",
                fileName,
                executionId = execution.Id,
                durationMs  = (finishedAt - startedAt).TotalMilliseconds
            }),
            ExecutedBy = executedBy,
            ExecutedAt = startedAt
        });

        await _context.SaveChangesAsync(ct);

        // ── 3. Envía a BigQuery — fallo no interrumpe el ETL ────────────────────
        if (jsonRows.Count > 0)
        {
            try
            {
                await _bigQuery.EnsureTableExistsAsync();
                await _bigQuery.InsertRowsAsync(pipelineId, pipelineName, jsonRows);
            }
            catch (Exception ex)
            {
                // Error de BigQuery: solo loguea, el resultado del ETL no cambia
                _logger.LogError(ex, "BigQuery: error al insertar {N} filas del pipeline {Id}.",
                    jsonRows.Count, pipelineId);
            }
        }

        return new EtlResult(
            execution.Id, execution.Status,
            total, success, errors,
            startedAt, finishedAt);
    }
}
