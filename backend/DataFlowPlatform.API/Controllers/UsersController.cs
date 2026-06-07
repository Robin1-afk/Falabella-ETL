using DataFlowPlatform.API.DTOs.Users;
using DataFlowPlatform.Domain.Entities.Auth;
using DataFlowPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataFlowPlatform.API.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly DataFlowPlatformDbContext _context;

    public UsersController(DataFlowPlatformDbContext context)
        => _context = context;

    // ── GET /users ───────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _context.Users
            .AsNoTracking()
            .Select(u => new UserResponseDto
            {
                Id       = u.Id,
                Name     = u.Name,
                Email    = u.Email,
                RoleId   = u.RoleId,
                RoleName = u.Role.Name,
                IsActive = u.IsActive
            })
            .ToListAsync();

        return Ok(users);
    }

    // ── GET /users/{id} ──────────────────────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserResponseDto
            {
                Id       = u.Id,
                Name     = u.Name,
                Email    = u.Email,
                RoleId   = u.RoleId,
                RoleName = u.Role.Name,
                IsActive = u.IsActive
            })
            .FirstOrDefaultAsync();

        if (user is null)
            return NotFound(new { message = "Usuario no encontrado." });

        return Ok(user);
    }

    // ── POST /users ──────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserCreateDto dto)
    {
        // Verifica unicidad del correo antes de insertar
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return Conflict(new { message = "El correo ya está registrado." });

        var user = new User
        {
            Name     = dto.Name,
            Email    = dto.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password, 12),
            RoleId   = dto.RoleId,
            IsActive = dto.IsActive
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Carga el nombre del rol para el response
        var roleName = await _context.Roles
            .Where(r => r.Id == user.RoleId)
            .Select(r => r.Name)
            .FirstOrDefaultAsync() ?? string.Empty;

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new UserResponseDto
        {
            Id       = user.Id,
            Name     = user.Name,
            Email    = user.Email,
            RoleId   = user.RoleId,
            RoleName = roleName,
            IsActive = user.IsActive
        });
    }

    // ── PUT /users/{id} ──────────────────────────────────────────────────────
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UserUpdateDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null)
            return NotFound(new { message = "Usuario no encontrado." });

        // Verifica unicidad del correo excluyendo al propio usuario
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id))
            return Conflict(new { message = "El correo ya está registrado por otro usuario." });

        user.Name     = dto.Name;
        user.Email    = dto.Email;
        user.RoleId   = dto.RoleId;
        user.IsActive = dto.IsActive;

        // Solo actualiza el hash si se envió una nueva contraseña
        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password, 12);

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ── DELETE /users/{id} ───────────────────────────────────────────────────
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null)
            return NotFound(new { message = "Usuario no encontrado." });

        // Baja lógica: evita romper FK en pipelines y sesiones históricas
        user.IsActive = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
