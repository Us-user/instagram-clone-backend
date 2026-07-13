using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация <see cref="Mention"/>. Две FK на пользователя (кого упомянули и кто упомянул) —
/// обе каскадом (PostgreSQL допускает несколько каскадных путей к таблице пользователей,
/// как у <see cref="Notification"/>/<see cref="Block"/>). Одно упоминание пользователя
/// на объект — пара (MentionedUserId, EntityType, EntityId) уникальна.
/// </summary>
public class MentionConfiguration : IEntityTypeConfiguration<Mention>
{
    public void Configure(EntityTypeBuilder<Mention> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasOne(m => m.MentionedUser)
            .WithMany()
            .HasForeignKey(m => m.MentionedUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.AuthorUser)
            .WithMany()
            .HasForeignKey(m => m.AuthorUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Дедупликация упоминаний одного юзера в одном объекте.
        builder.HasIndex(m => new { m.MentionedUserId, m.EntityType, m.EntityId }).IsUnique();

        // Быстрый доступ к упоминаниям по объекту (для сборки списка в DTO).
        builder.HasIndex(m => new { m.EntityType, m.EntityId });
    }
}
