using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация <see cref="Block"/>. Две FK на пользователя (кто блокирует и кого) —
/// обе каскадом (PostgreSQL допускает несколько каскадных путей к таблице пользователей,
/// как у <see cref="Notification"/>/<see cref="FollowingRelationShip"/>). Пара уникальна:
/// один блок на направление.
/// </summary>
public class BlockConfiguration : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> builder)
    {
        builder.HasKey(b => b.Id);

        builder.HasOne(b => b.BlockerUser)
            .WithMany()
            .HasForeignKey(b => b.BlockerUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.BlockedUser)
            .WithMany()
            .HasForeignKey(b => b.BlockedUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Один блок на направление; ускоряет проверки блокировок в обе стороны.
        builder.HasIndex(b => new { b.BlockerUserId, b.BlockedUserId }).IsUnique();
        builder.HasIndex(b => b.BlockedUserId);
    }
}
