using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>Конфигурация <see cref="Message"/>.</summary>
public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.MessageText).HasMaxLength(4000);
        builder.Property(m => m.FileName).HasMaxLength(512);
        builder.Property(m => m.Waveform).HasMaxLength(4000);

        builder.HasOne(m => m.Chat)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Самоссылка reply (§8) — на SetNull: удаление процитированного не уносит ответы,
        // лишь обнуляет ссылку на цитату (как у групповых сообщений).
        builder.HasOne(m => m.ReplyToMessage)
            .WithMany()
            .HasForeignKey(m => m.ReplyToMessageId)
            .OnDelete(DeleteBehavior.SetNull);

        // Выборка сообщений чата в хронологическом порядке.
        builder.HasIndex(m => new { m.ChatId, m.CreatedAt });
    }
}
