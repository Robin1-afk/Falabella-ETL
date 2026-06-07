namespace DataFlowPlatform.Infrastructure.Security;

public interface IJwtService
{
    // Genera un JWT firmado con los claims del usuario (userId, email, roleId)
    string GenerateAccessToken(int userId, string email, int roleId);

    // Genera un refresh token opaco (64 bytes aleatorios en Base64); no es JWT
    string GenerateRefreshToken();
}
