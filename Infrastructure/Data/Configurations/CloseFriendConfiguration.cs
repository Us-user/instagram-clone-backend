using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация <see cref="CloseFriend"/> (§9). Две FK на пользователя (владелец списка и
/// добавленный) — обе каскадом (PostgreSQL допускает несколько каскадных путей к таблице
/// пользователей, как у <see cref="Block"/>). Пара уникальна: один пользователь добавляется
/// в близкие не более одного раза.
/// </summary>
public class CloseFriendConfiguration : IEntityTypeConfiguration<CloseFriend>
{
    public void Configure(EntityTypeBuilder<CloseFriend> builder)
    {
        builder.HasKey(cf => cf.Id);

        builder.HasOne(cf => cf.User)
            .WithMany()
            .HasForeignKey(cf => cf.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cf => cf.Friend)
            .WithMany()
            .HasForeignKey(cf => cf.FriendUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Один пользователь в близких у владельца не более одного раза.
        builder.HasIndex(cf => new { cf.UserId, cf.FriendUserId }).IsUnique();
        // Обратный поиск: «кто добавил меня в близкие» (фильтрация close-friends-сторис).
        builder.HasIndex(cf => cf.FriendUserId);
    }
}
