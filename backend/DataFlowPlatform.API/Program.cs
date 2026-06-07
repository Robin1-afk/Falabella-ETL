using DataFlowPlatform.API.Middleware;
using DataFlowPlatform.Infrastructure.Persistence;
using DataFlowPlatform.Infrastructure.Security;
using DataFlowPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Base de datos ────────────────────────────────────────────────────────────
builder.Services.AddDbContext<DataFlowPlatformDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Autenticación JWT Bearer ──────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret no está configurado en appsettings.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Valida la firma del token con la clave secreta simétrica
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),

            // Emisor y audiencia no se validan; el sistema es el único consumidor
            ValidateIssuer   = false,
            ValidateAudience = false,

            // Sin tolerancia de desfase de reloj para mayor seguridad
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── Servicios de aplicación ──────────────────────────────────────────────────
// JwtService: genera access tokens (JWT) y refresh tokens (opacos)
builder.Services.AddScoped<IJwtService, JwtService>();
// EtlService: orquesta lectura CSV → validación → transformación → carga en BD
builder.Services.AddScoped<IEtlService, EtlService>();
// BigQueryService: envía filas procesadas a BigQuery (usa Application Default Credentials)
builder.Services.AddScoped<BigQueryService>();

// ── Controladores y OpenAPI ──────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// JwtMiddleware: lee el token del header y puebla HttpContext.Items["userId"/"roleId"]
app.UseMiddleware<JwtMiddleware>();

// Orden obligatorio: primero autenticación, luego autorización
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
