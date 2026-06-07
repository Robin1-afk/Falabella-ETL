using DataFlowPlatform.API.DTOs.Auth;
using DataFlowPlatform.Domain.Entities.Auth;
using DataFlowPlatform.Infrastructure.Persistence;
using DataFlowPlatform.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataFlowPlatform.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly DataFlowPlatformDbContext _context;
    private readonly IJwtService               _jwt;
    private readonly IConfiguration            _config;

    public AuthController(
        DataFlowPlatformDbContext context,
        IJwtService               jwt,
        IConfiguration            config)
    {
        _context = context;
        _jwt     = jwt;
        _config  = config;
    }

    // ── POST auth/login ──────────────────────────────────────────────────────
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        // Busca el usuario activo por email e incluye el rol para el response
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email && u.IsActive);

        // Verifica existencia y contraseña (BCrypt compara texto plano vs hash)
        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
            return Unauthorized(new { message = "Credenciales inválidas." });

        var accessToken  = _jwt.GenerateAccessToken(user.Id, user.Email, user.RoleId);
        var refreshToken = _jwt.GenerateRefreshToken();

        // Persiste la sesión; token_hash se calcula automáticamente en SQL Server
        _context.UserSessions.Add(new UserSession
        {
            UserId    = user.Id,
            Token     = refreshToken,
            IsActive  = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return Ok(new LoginResponseDto
        {
            AccessToken  = accessToken,
            RefreshToken = refreshToken,
            UserId       = user.Id,
            Name         = user.Name,
            Email        = user.Email,
            RoleId       = user.RoleId,
            RoleName     = user.Role.Name
        });
    }

    // ── POST auth/refresh ────────────────────────────────────────────────────
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto dto)
    {
        // Calcula el umbral de expiración del refresh token
        var expirationMs = _config.GetValue<long>("Jwt:ExpirationRefresh");
        var cutoff        = DateTime.UtcNow.AddMilliseconds(-expirationMs);

        // Busca sesión activa cuyo created_at esté dentro del período de vida
        var session = await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s =>
                s.Token     == dto.RefreshToken &&
                s.IsActive  == true             &&
                s.CreatedAt >  cutoff);

        if (session is null)
            return Unauthorized(new { message = "Refresh token inválido o expirado." });

        // Emite nuevo access token con los datos del usuario de la sesión
        var accessToken = _jwt.GenerateAccessToken(
            session.User.Id,
            session.User.Email,
            session.User.RoleId);

        return Ok(new RefreshResponseDto { AccessToken = accessToken });
    }

    // ── POST auth/logout ─────────────────────────────────────────────────────
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDto dto)
    {
        // Invalida la sesión; operación idempotente (sin error si ya estaba inactiva)
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.Token == dto.RefreshToken && s.IsActive);

        if (session is not null)
        {
            session.IsActive = false;
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Sesión cerrada correctamente." });
    }
}
