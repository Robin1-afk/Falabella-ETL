using System.Text.Json;
using DataFlowPlatform.API.DTOs.Data;
using DataFlowPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataFlowPlatform.API.Controllers;

[ApiController]
[Route("data")]
[Authorize]
public class DataController : ControllerBase
{
    private readonly DataFlowPlatformDbContext _context;

    public DataController(DataFlowPlatformDbContext context) => _context = context;

    // ── GET /data/{pipelineId}?page=1&pageSize=10 ────────────────────────────
    [HttpGet("{pipelineId:int}")]
    public async Task<IActionResult> GetData(
        int pipelineId,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 10)
    {
        // Normaliza parámetros de paginación
        if (page < 1) page = 1;
        pageSize = Math.Clamp(pageSize, 1, 100);

        if (!await _context.Pipelines.AnyAsync(p => p.Id == pipelineId))
            return NotFound(new { message = "Pipeline no encontrado." });

        var total = await _context.PipelineData
            .LongCountAsync(pd => pd.PipelineId == pipelineId);

        // EF Core trae la columna data como string; el JSON se parsea en memoria
        var raw = await _context.PipelineData
            .AsNoTracking()
            .Where(pd => pd.PipelineId == pipelineId)
            .OrderByDescending(pd => pd.LoadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(pd => new { pd.Id, pd.PipelineId, pd.Data, pd.LoadedAt })
            .ToListAsync();

        // Deserializa cada fila a JsonElement para que el cliente reciba un objeto, no una cadena
        var items = raw.Select(pd => new PipelineDataItemDto
        {
            Id         = pd.Id,
            PipelineId = pd.PipelineId,
            Data       = JsonSerializer.Deserialize<JsonElement>(pd.Data),
            LoadedAt   = pd.LoadedAt
        }).ToList();

        return Ok(new PipelineDataPageDto
        {
            Page         = page,
            PageSize     = pageSize,
            TotalRecords = total,
            TotalPages   = (int)Math.Ceiling(total / (double)pageSize),
            Items        = items
        });
    }

    // ── GET /data/{pipelineId}/stats ─────────────────────────────────────────
    [HttpGet("{pipelineId:int}/stats")]
    public async Task<IActionResult> GetStats(int pipelineId)
    {
        var pipeline = await _context.Pipelines
            .AsNoTracking()
            .Where(p => p.Id == pipelineId)
            .Select(p => new { p.Id, p.Name })
            .FirstOrDefaultAsync();

        if (pipeline is null)
            return NotFound(new { message = "Pipeline no encontrado." });

        // Agrega en un solo query: total, primera y última carga
        var dataStats = await _context.PipelineData
            .AsNoTracking()
            .Where(pd => pd.PipelineId == pipelineId)
            .GroupBy(pd => pd.PipelineId)
            .Select(g => new
            {
                TotalRecords  = g.LongCount(),
                FirstLoadedAt = g.Min(pd => (DateTime?)pd.LoadedAt),
                LastLoadedAt  = g.Max(pd => (DateTime?)pd.LoadedAt)
            })
            .FirstOrDefaultAsync();

        var totalExecutions = await _context.PipelineExecutions
            .AsNoTracking()
            .CountAsync(pe => pe.PipelineId == pipelineId);

        return Ok(new PipelineStatsDto
        {
            PipelineId      = pipelineId,
            PipelineName    = pipeline.Name,
            TotalRecords    = dataStats?.TotalRecords  ?? 0,
            FirstLoadedAt   = dataStats?.FirstLoadedAt,
            LastLoadedAt    = dataStats?.LastLoadedAt,
            TotalExecutions = totalExecutions
        });
    }

    // ── GET /audit ───────────────────────────────────────────────────────────
    // Ruta absoluta: la / inicial ignora el prefijo "data" del controlador
    [HttpGet("/audit")]
    public async Task<IActionResult> GetAudit()
    {
        var logs = await _context.EtlAuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.ExecutedAt)
            .Select(a => new AuditLogResponseDto
            {
                Id             = a.Id,
                PipelineId     = a.PipelineId,
                PipelineName   = a.Pipeline.Name,
                FileName       = a.FileName,
                TotalRows      = a.TotalRows,
                SuccessRows    = a.SuccessRows,
                ErrorRows      = a.ErrorRows,
                Metadata       = a.Metadata,
                ExecutedBy     = a.ExecutedBy,
                ExecutedByName = a.Executor.Name,
                ExecutedAt     = a.ExecutedAt
            })
            .ToListAsync();

        return Ok(logs);
    }
}
