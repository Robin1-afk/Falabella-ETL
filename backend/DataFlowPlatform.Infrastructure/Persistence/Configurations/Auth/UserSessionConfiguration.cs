using DataFlowPlatform.Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataFlowPlatform.Infrastructure.Persistence.Configurations.Auth;

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(s => s.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(s => s.Token)
            .HasColumnName("token")
            .HasMaxLength(1000)
            .IsRequired();

        // Columna computada PERSISTED en SQL Server; EF Core la lee pero nunca la escribe
        builder.Property(s => s.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(64)
            .HasComputedColumnSql(
                "CONVERT(NVARCHAR(64), HASHBYTES('SHA2_256', token), 2)",
                stored: true);

        builder.Property(s => s.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("SYSDATETIME()");

        // Índice único sobre el hash para validar tokens sin superar el límite de 1700 bytes
        builder.HasIndex(s => s.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_user_sessions_token_hash");

        builder.HasOne(s => s.User)
            .WithMany(u => u.Sessions)
            .HasForeignKey(s => s.UserId)
            .HasConstraintName("FK_user_sessions_user")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
