using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>Конфигурация <see cref="Chat"/> (личный чат двух пользователей).</summary>
public class ChatConfiguration : IEntityTypeConfiguration<Chat>
{
    public void Configure(EntityTypeBuilder<Chat> builder)
    {
        builder.HasKey(c => c.Id);

        builder.HasOne(c => c.User1)
            .WithMany()
            .HasForeignKey(c => c.User1Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.User2)
            .WithMany()
            .HasForeignKey(c => c.User2Id)
            .OnDelete(DeleteBehavior.Cascade);

        // Дедупликация: одна пара (User1, User2) → один чат.
        // Сервис (Phase 8) нормализует порядок участников перед созданием.
        builder.HasIndex(c => new { c.User1Id, c.User2Id }).IsUnique();
    }
}
