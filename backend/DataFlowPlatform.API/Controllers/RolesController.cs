using DataFlowPlatform.API.DTOs.Roles;
using DataFlowPlatform.Domain.Entities.Auth;
using DataFlowPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataFlowPlatform.API.Controllers;

[ApiController]
[Route("roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly DataFlowPlatformDbContext _context;

    public RolesController(DataFlowPlatformDbContext context)
        => _context = context;

    // ── GET /roles ───────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var roles = await _context.Roles
            .AsNoTracking()
            .Select(r => new RoleResponseDto
            {
                Id       = r.Id,
                Name     = r.Name,
                IsActive = r.IsActive
            })
            .ToListAsync();

        return Ok(roles);
    }

    // ── POST /roles ──────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RoleCreateDto dto)
    {
        // El nombre del rol debe ser único en el sistema
        if (await _context.Roles.AnyAsync(r => r.Name == dto.Name))
            return Conflict(new { message = "Ya existe un rol con ese nombre." });

        var role = new Role { Name = dto.Name, IsActive = dto.IsActive };
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new RoleResponseDto
        {
            Id       = role.Id,
            Name     = role.Name,
            IsActive = role.IsActive
        });
    }

    // ── PUT /roles/{id} ──────────────────────────────────────────────────────
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] RoleUpdateDto dto)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role is null)
            return NotFound(new { message = "Rol no encontrado." });

        // Verifica unicidad del nombre excluyendo al propio rol
        if (await _context.Roles.AnyAsync(r => r.Name == dto.Name && r.Id != id))
            return Conflict(new { message = "Ya existe un rol con ese nombre." });

        role.Name     = dto.Name;
        role.IsActive = dto.IsActive;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ── PUT /roles/{id}/views ────────────────────────────────────────────────
    // Upsert de permisos: recibe la lista completa de asignaciones para el rol.
    // Actualiza los existentes y crea los nuevos; no elimina los no incluidos.
    [HttpPut("{id:int}/views")]
    public async Task<IActionResult> AssignViews(
        int id, [FromBody] List<RoleViewAssignItemDto> dto)
    {
        if (!await _context.Roles.AnyAsync(r => r.Id == id))
            return NotFound(new { message = "Rol no encontrado." });

        // Carga asignaciones actuales del rol en memoria
        var existing = await _context.RoleViews
            .Where(rv => rv.RoleId == id)
            .ToListAsync();

        foreach (var item in dto)
        {
            var entry = existing.FirstOrDefault(rv => rv.ViewId == item.ViewId);
            if (entry is null)
            {
                // Nueva asignación
                _context.RoleViews.Add(new RoleView
                {
                    RoleId   = id,
                    ViewId   = item.ViewId,
                    IsActive = item.IsActive
                });
            }
            else
            {
                // Actualiza el estado de la asignación existente
                entry.IsActive = item.IsActive;
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }
}
