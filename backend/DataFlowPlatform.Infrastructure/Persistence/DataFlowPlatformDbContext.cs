using DataFlowPlatform.Domain.Entities.Auth;
using DataFlowPlatform.Domain.Entities.Pipeline;
using Microsoft.EntityFrameworkCore;
using PipelineEntity = DataFlowPlatform.Domain.Entities.Pipeline.Pipeline;

namespace DataFlowPlatform.Infrastructure.Persistence;

public class DataFlowPlatformDbContext : DbContext
{
    public DataFlowPlatformDbContext(DbContextOptions<DataFlowPlatformDbContext> options)
        : base(options) { }

    // --- Auth ---
    public DbSet<Role>        Roles        => Set<Role>();
    public DbSet<User>        Users        => Set<User>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<View>        Views        => Set<View>();
    public DbSet<RoleView>    RoleViews    => Set<RoleView>();

    // --- Pipeline ---
    public DbSet<PipelineEntity>    Pipelines          => Set<PipelineEntity>();
    public DbSet<PipelineExecution> PipelineExecutions => Set<PipelineExecution>();
    public DbSet<EtlAuditLog>       EtlAuditLogs       => Set<EtlAuditLog>();
    public DbSet<PipelineData>      PipelineData       => Set<PipelineData>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Auto-descubre todos los IEntityTypeConfiguration<T> definidos en este ensamblado
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataFlowPlatformDbContext).Assembly);
    }
}
