using DataFlowPlatform.Domain.Entities.Pipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataFlowPlatform.Infrastructure.Persistence.Configurations.Pipeline;

public class EtlAuditLogConfiguration : IEntityTypeConfiguration<EtlAuditLog>
{
    public void Configure(EntityTypeBuilder<EtlAuditLog> builder)
    {
        builder.ToTable("etl_audit_logs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.PipelineId)
            .HasColumnName("pipeline_id")
            .IsRequired();

        builder.Property(e => e.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.TotalRows)
            .HasColumnName("total_rows")
            .HasDefaultValue(0);

        builder.Property(e => e.SuccessRows)
            .HasColumnName("success_rows")
            .HasDefaultValue(0);

        builder.Property(e => e.ErrorRows)
            .HasColumnName("error_rows")
            .HasDefaultValue(0);

        // JSON libre sin límite de longitud (NVARCHAR MAX); nullable
        builder.Property(e => e.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("NVARCHAR(MAX)");

        builder.Property(e => e.ExecutedBy)
            .HasColumnName("executed_by")
            .IsRequired();

        builder.Property(e => e.ExecutedAt)
            .HasColumnName("executed_at")
            .HasDefaultValueSql("SYSDATETIME()");

        builder.HasOne(e => e.Executor)
            .WithMany()
            .HasForeignKey(e => e.ExecutedBy)
            .HasConstraintName("FK_eal_user")
            .OnDelete(DeleteBehavior.Restrict);

        // Relación inversa configurada desde PipelineConfiguration
    }
}
