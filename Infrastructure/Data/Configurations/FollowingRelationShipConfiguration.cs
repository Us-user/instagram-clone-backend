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

        // Status всегда пишется сервисом явно (Pending/Accepted), поэтому НЕ задаём
        // model-level default: с ним EF считал бы CLR-default (Pending=0) «неустановленным»
        // и подменял его на store-default при вставке запроса на подписку. Существующим
        // строкам базы значение Accepted проставляется в самой миграции (backfill).

        // Нельзя подписаться на одного и того же пользователя дважды.
        builder.HasIndex(f => new { f.UserId, f.FollowingUserId }).IsUnique();

        // Быстрый разбор входящих запросов на подписку у владельца приватного аккаунта.
        builder.HasIndex(f => new { f.FollowingUserId, f.Status });
    }
}
