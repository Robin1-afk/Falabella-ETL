using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DataFlowPlatform.Infrastructure.Security;

public class JwtService : IJwtService
{
    private readonly string _secret;
    private readonly long   _expirationAccessMs;

    public JwtService(IConfiguration config)
    {
        _secret = config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret no está configurado.");

        // Lee la expiración en milisegundos desde appsettings (ej. 900 000 ms = 15 min)
        _expirationAccessMs = long.Parse(config["Jwt:ExpirationAccess"]
            ?? throw new InvalidOperationException("Jwt:ExpirationAccess no está configurado."));
    }

    public string GenerateAccessToken(int userId, string email, int roleId)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Claims idénticos a los del JwtProvider del proyecto Spring Boot de referencia
        var claims = new[]
        {
            new Claim("userId",          userId.ToString()),
            new Claim(ClaimTypes.Email,  email),
            new Claim("roleId",          roleId.ToString())
        };

        var token = new JwtSecurityToken(
            claims:             claims,
            expires:            DateTime.UtcNow.AddMilliseconds(_expirationAccessMs),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // 64 bytes criptográficamente seguros → ~88 caracteres Base64; cabe en NVARCHAR(1000)
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
