using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация <see cref="Notification"/>. Две FK на пользователя (получатель и инициатор) —
/// обе каскадом (PostgreSQL допускает несколько каскадных путей к таблице пользователей,
/// как у <see cref="FollowingRelationShip"/>/<see cref="Chat"/>).
/// </summary>
public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.HasOne(n => n.Recipient)
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Actor)
            .WithMany()
            .HasForeignKey(n => n.ActorUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Выборка ленты получателя (свежие сверху) и подсчёт непрочитанных.
        builder.HasIndex(n => new { n.RecipientUserId, n.CreatedAt });
        builder.HasIndex(n => new { n.RecipientUserId, n.IsRead });

        // Группировка одинаковых уведомлений на один объект.
        builder.HasIndex(n => new { n.RecipientUserId, n.Type, n.EntityType, n.EntityId });
    }
}
