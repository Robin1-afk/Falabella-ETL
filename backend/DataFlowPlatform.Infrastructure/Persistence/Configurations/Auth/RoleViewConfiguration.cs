using DataFlowPlatform.Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataFlowPlatform.Infrastructure.Persistence.Configurations.Auth;

public class RoleViewConfiguration : IEntityTypeConfiguration<RoleView>
{
    public void Configure(EntityTypeBuilder<RoleView> builder)
    {
        builder.ToTable("role_views");

        builder.HasKey(rv => rv.Id);

        builder.Property(rv => rv.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(rv => rv.RoleId)
            .HasColumnName("role_id")
            .IsRequired();

        builder.Property(rv => rv.ViewId)
            .HasColumnName("view_id")
            .IsRequired();

        builder.Property(rv => rv.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        // Evita duplicar la misma combinación rol-vista
        builder.HasIndex(rv => new { rv.RoleId, rv.ViewId })
            .IsUnique()
            .HasDatabaseName("UQ_role_views");

        builder.HasOne(rv => rv.Role)
            .WithMany(r => r.RoleViews)
            .HasForeignKey(rv => rv.RoleId)
            .HasConstraintName("FK_role_views_role")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(rv => rv.View)
            .WithMany(v => v.RoleViews)
            .HasForeignKey(rv => rv.ViewId)
            .HasConstraintName("FK_role_views_view")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
