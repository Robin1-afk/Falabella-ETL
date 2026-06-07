using DataFlowPlatform.Domain.Entities.Pipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataFlowPlatform.Infrastructure.Persistence.Configurations.Pipeline;

public class PipelineExecutionConfiguration : IEntityTypeConfiguration<PipelineExecution>
{
    public void Configure(EntityTypeBuilder<PipelineExecution> builder)
    {
        builder.ToTable("pipeline_executions");

        builder.HasKey(pe => pe.Id);

        builder.Property(pe => pe.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(pe => pe.PipelineId)
            .HasColumnName("pipeline_id")
            .IsRequired();

        // Pending | Running | Completed | Failed
        builder.Property(pe => pe.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(pe => pe.TotalRows)
            .HasColumnName("total_rows")
            .HasDefaultValue(0);

        builder.Property(pe => pe.SuccessRows)
            .HasColumnName("success_rows")
            .HasDefaultValue(0);

        builder.Property(pe => pe.ErrorRows)
            .HasColumnName("error_rows")
            .HasDefaultValue(0);

        builder.Property(pe => pe.ExecutedBy)
            .HasColumnName("executed_by")
            .IsRequired();

        builder.Property(pe => pe.ExecutedAt)
            .HasColumnName("executed_at")
            .HasDefaultValueSql("SYSDATETIME()");

        // Null mientras la ejecución está en curso
        builder.Property(pe => pe.FinishedAt)
            .HasColumnName("finished_at");

        builder.HasOne(pe => pe.Executor)
            .WithMany()
            .HasForeignKey(pe => pe.ExecutedBy)
            .HasConstraintName("FK_pe_user")
            .OnDelete(DeleteBehavior.Restrict);

        // Relación inversa configurada desde PipelineConfiguration
    }
}
