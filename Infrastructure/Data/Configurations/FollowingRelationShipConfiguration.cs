using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация <see cref="FollowingRelationShip"/>: <c>UserId</c> (подписчик) →
/// <c>FollowingUserId</c> (на кого). Уникальна на пару, обе стороны — каскад
/// (PostgreSQL допускает несколько каскадных путей к таблице пользователей).
/// </summary>
public class FollowingRelationShipConfiguration : IEntityTypeConfiguration<FollowingRelationShip>
{
    public void Configure(EntityTypeBuilder<FollowingRelationShip> builder)
    {
        builder.HasKey(f => f.Id);

        builder.HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.FollowingUser)
            .WithMany()
            .HasForeignKey(f => f.FollowingUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Нельзя подписаться на одного и того же пользователя дважды.
        builder.HasIndex(f => new { f.UserId, f.FollowingUserId }).IsUnique();
    }
}
