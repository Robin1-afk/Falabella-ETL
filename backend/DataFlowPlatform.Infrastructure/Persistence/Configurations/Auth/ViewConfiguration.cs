using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using View = DataFlowPlatform.Domain.Entities.Auth.View;

namespace DataFlowPlatform.Infrastructure.Persistence.Configurations.Auth;

public class ViewConfiguration : IEntityTypeConfiguration<View>
{
    public void Configure(EntityTypeBuilder<View> builder)
    {
        builder.ToTable("views");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(v => v.Name)
            .HasColumnName("name")
            .HasMaxLength(150)
            .IsRequired();

        // Nullable en la base de datos
        builder.Property(v => v.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(v => v.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.HasIndex(v => v.Name)
            .IsUnique()
            .HasDatabaseName("UQ_views_name");
    }
}
