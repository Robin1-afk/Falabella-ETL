using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DataFlowPlatform.API.Middleware;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string          _secret;

    public JwtMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next   = next;
        _secret = config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret no está configurado.");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extrae el token del header "Authorization: Bearer <token>"
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
        {
            var token = authHeader["Bearer ".Length..].Trim();
            AttachClaims(context, token);
        }

        await _next(context);
    }

    private void AttachClaims(HttpContext context, string token)
    {
        try
        {
            var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var handler = new JwtSecurityTokenHandler();

            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = key,
                ValidateIssuer           = false,
                ValidateAudience         = false,
                ClockSkew                = TimeSpan.Zero
            }, out var validated);

            var jwt = (JwtSecurityToken)validated;

            // Puebla HttpContext.Items para acceso directo en los controllers
            context.Items["userId"] = int.Parse(jwt.Claims.First(c => c.Type == "userId").Value);
            context.Items["roleId"] = int.Parse(jwt.Claims.First(c => c.Type == "roleId").Value);
        }
        catch
        {
            // Token inválido o expirado; los endpoints con [Authorize] devolverán 401
        }
    }
}
