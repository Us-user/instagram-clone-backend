using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>Конфигурация <see cref="UserProfile"/> (1:1 с <see cref="User"/>).</summary>
public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.About).HasMaxLength(1000);
        builder.Property(p => p.Image).HasMaxLength(512);

        // 1:1 User ↔ UserProfile; уникальность UserId гарантирует связь один-к-одному.
        builder.HasOne(p => p.User)
            .WithOne(u => u.UserProfile)
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.UserId).IsUnique();
    }
}
