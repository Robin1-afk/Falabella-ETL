using DataFlowPlatform.API.DTOs.Pipelines;
using DataFlowPlatform.Infrastructure.Persistence;
using DataFlowPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PipelineEntity = DataFlowPlatform.Domain.Entities.Pipeline.Pipeline;

namespace DataFlowPlatform.API.Controllers;

[ApiController]
[Route("pipelines")]
[Authorize]
public class PipelinesController : ControllerBase
{
    private readonly DataFlowPlatformDbContext _context;
    private readonly IEtlService               _etl;

    public PipelinesController(DataFlowPlatformDbContext context, IEtlService etl)
    {
        _context = context;
        _etl     = etl;
    }

    // ── GET /pipelines ───────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var pipelines = await _context.Pipelines
            .AsNoTracking()
            .Select(p => new PipelineResponseDto
            {
                Id            = p.Id,
                Name          = p.Name,
                Description   = p.Description,
                SourceType    = p.SourceType,
                Schedule      = p.Schedule,
                Status        = p.Status,
                CreatedBy     = p.CreatedBy,
                CreatedByName = p.Creator.Name,
                CreatedAt     = p.CreatedAt
            })
            .ToListAsync();

        return Ok(pipelines);
    }

    // ── POST /pipelines ──────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PipelineCreateDto dto)
    {
        // Obtiene el userId del JwtMiddleware; fallback a claim si Items está vacío
        var userId = HttpContext.Items.TryGetValue("userId", out var uid)
            ? (int)uid!
            : int.Parse(User.FindFirst("userId")?.Value ?? "0");

        var pipeline = new PipelineEntity
        {
            Name        = dto.Name,
            Description = dto.Description,
            SourceType  = dto.SourceType,
            Schedule    = dto.Schedule,
            Status      = "Active",
            CreatedBy   = userId,
            CreatedAt   = DateTime.UtcNow
        };

        _context.Pipelines.Add(pipeline);
        await _context.SaveChangesAsync();

        var creatorName = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Name)
            .FirstOrDefaultAsync() ?? string.Empty;

        return CreatedAtAction(nameof(GetAll), new { id = pipeline.Id }, new PipelineResponseDto
        {
            Id            = pipeline.Id,
            Name          = pipeline.Name,
            Description   = pipeline.Description,
            SourceType    = pipeline.SourceType,
            Schedule      = pipeline.Schedule,
            Status        = pipeline.Status,
            CreatedBy     = pipeline.CreatedBy,
            CreatedByName = creatorName,
            CreatedAt     = pipeline.CreatedAt
        });
    }

    // ── POST /pipelines/{id}/run ─────────────────────────────────────────────
    // Recibe un archivo CSV via multipart/form-data (campo "file")
    [HttpPost("{id:int}/run")]
    [RequestSizeLimit(52_428_800)] // límite 50 MB
    public async Task<IActionResult> Run(int id, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Se requiere un archivo CSV válido." });

        // Verifica que el pipeline exista y esté activo
        var pipeline = await _context.Pipelines.FindAsync(id);
        if (pipeline is null)
            return NotFound(new { message = "Pipeline no encontrado." });

        if (pipeline.Status != "Active")
            return BadRequest(new { message = $"El pipeline está en estado '{pipeline.Status}' y no puede ejecutarse." });

        var userId = HttpContext.Items.TryGetValue("userId", out var uid)
            ? (int)uid!
            : int.Parse(User.FindFirst("userId")?.Value ?? "0");

        // Delega todo el proceso ETL al servicio
        await using var stream = file.OpenReadStream();
        var result = await _etl.RunAsync(id, userId, stream, file.FileName, HttpContext.RequestAborted);

        return Ok(new PipelineRunResponseDto
        {
            ExecutionId = result.ExecutionId,
            Status      = result.Status,
            TotalRows   = result.TotalRows,
            SuccessRows = result.SuccessRows,
            ErrorRows   = result.ErrorRows,
            ExecutedAt  = result.ExecutedAt,
            FinishedAt  = result.FinishedAt
        });
    }

    // ── GET /pipelines/{id}/executions ───────────────────────────────────────
    [HttpGet("{id:int}/executions")]
    public async Task<IActionResult> GetExecutions(int id)
    {
        if (!await _context.Pipelines.AnyAsync(p => p.Id == id))
            return NotFound(new { message = "Pipeline no encontrado." });

        var executions = await _context.PipelineExecutions
            .AsNoTracking()
            .Where(e => e.PipelineId == id)
            .OrderByDescending(e => e.ExecutedAt)
            .Select(e => new PipelineExecutionResponseDto
            {
                Id             = e.Id,
                PipelineId     = e.PipelineId,
                Status         = e.Status,
                TotalRows      = e.TotalRows,
                SuccessRows    = e.SuccessRows,
                ErrorRows      = e.ErrorRows,
                ExecutedBy     = e.ExecutedBy,
                ExecutedByName = e.Executor.Name,
                ExecutedAt     = e.ExecutedAt,
                FinishedAt     = e.FinishedAt
            })
            .ToListAsync();

        return Ok(executions);
    }
}
