using DataFlowPlatform.API.DTOs.Views;
using DataFlowPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataFlowPlatform.API.Controllers;

[ApiController]
[Route("views")]
[Authorize]
public class ViewsController : ControllerBase
{
    private readonly DataFlowPlatformDbContext _context;

    public ViewsController(DataFlowPlatformDbContext context)
        => _context = context;

    // ── GET /views ───────────────────────────────────────────────────────────
    // Devuelve el catálogo completo de vistas del sistema
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var views = await _context.Views
            .AsNoTracking()
            .Select(v => new ViewResponseDto
            {
                Id          = v.Id,
                Name        = v.Name,
                Description = v.Description,
                IsActive    = v.IsActive
            })
            .ToListAsync();

        return Ok(views);
    }
}
