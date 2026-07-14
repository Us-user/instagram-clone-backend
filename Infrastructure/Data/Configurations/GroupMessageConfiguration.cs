using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация <see cref="GroupMessage"/> (§7). FK на группу — каскадом; FK на отправителя —
/// каскадом (nullable: у служебных сообщений отправителя нет). Самоссылка reply — на
/// <see cref="DeleteBehavior.SetNull"/>: удаление процитированного сообщения не уносит ответы,
/// а лишь обнуляет ссылку на цитату.
/// </summary>
public class GroupMessageConfiguration : IEntityTypeConfiguration<GroupMessage>
{
    public void Configure(EntityTypeBuilder<GroupMessage> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.MessageText).HasMaxLength(4000);
        builder.Property(m => m.FileName).HasMaxLength(512);
        builder.Property(m => m.Waveform).HasMaxLength(4000);

        builder.HasOne(m => m.GroupChat)
            .WithMany(g => g.Messages)
            .HasForeignKey(m => m.GroupChatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.ReplyToMessage)
            .WithMany()
            .HasForeignKey(m => m.ReplyToMessageId)
            .OnDelete(DeleteBehavior.SetNull);

        // Выборка сообщений группы в хронологическом порядке.
        builder.HasIndex(m => new { m.GroupChatId, m.CreatedAt });
    }
}
