using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PipelineEntity = DataFlowPlatform.Domain.Entities.Pipeline.Pipeline;

namespace DataFlowPlatform.Infrastructure.Persistence.Configurations.Pipeline;

public class PipelineConfiguration : IEntityTypeConfiguration<PipelineEntity>
{
    public void Configure(EntityTypeBuilder<PipelineEntity> builder)
    {
        builder.ToTable("pipelines");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        // CSV | Excel | API | Database
        builder.Property(p => p.SourceType)
            .HasColumnName("source_type")
            .HasMaxLength(100)
            .IsRequired();

        // Expresión CRON; null = solo ejecución manual
        builder.Property(p => p.Schedule)
            .HasColumnName("schedule")
            .HasMaxLength(100);

        // Active | Inactive | Archived
        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue("Active");

        builder.Property(p => p.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("SYSDATETIME()");

        builder.HasOne(p => p.Creator)
            .WithMany()
            .HasForeignKey(p => p.CreatedBy)
            .HasConstraintName("FK_pipelines_user")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Executions)
            .WithOne(e => e.Pipeline)
            .HasForeignKey(e => e.PipelineId)
            .HasConstraintName("FK_pe_pipeline")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.AuditLogs)
            .WithOne(a => a.Pipeline)
            .HasForeignKey(a => a.PipelineId)
            .HasConstraintName("FK_eal_pipeline")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
