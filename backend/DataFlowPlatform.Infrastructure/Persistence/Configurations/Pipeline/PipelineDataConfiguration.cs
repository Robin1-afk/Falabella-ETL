using DataFlowPlatform.Domain.Entities.Pipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataFlowPlatform.Infrastructure.Persistence.Configurations.Pipeline;

public class PipelineDataConfiguration : IEntityTypeConfiguration<PipelineData>
{
    public void Configure(EntityTypeBuilder<PipelineData> builder)
    {
        builder.ToTable("pipeline_data");

        builder.HasKey(pd => pd.Id);

        // BIGINT generado por la BD; admite millones de filas sin overflow
        builder.Property(pd => pd.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(pd => pd.PipelineId)
            .HasColumnName("pipeline_id")
            .IsRequired();

        // Sin límite de longitud: JSON de tamaño variable
        builder.Property(pd => pd.Data)
            .HasColumnName("data")
            .HasColumnType("NVARCHAR(MAX)")
            .IsRequired();

        builder.Property(pd => pd.LoadedAt)
            .HasColumnName("loaded_at")
            .HasDefaultValueSql("SYSDATETIME()");

        builder.HasOne(pd => pd.Pipeline)
            .WithMany()
            .HasForeignKey(pd => pd.PipelineId)
            .HasConstraintName("FK_pipeline_data_pipeline")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
