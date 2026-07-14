using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация <see cref="MessageReaction"/> (§8). FK на пользователя — каскадом. FK на само
/// сообщение отсутствует намеренно: <c>MessageId</c> полиморфен (личное или групповое сообщение),
/// поэтому «один юзер = одна реакция» гарантируется уникальным индексом
/// <c>(MessageId, MessageContext, UserId)</c>, а очистка при удалении сообщения/чата делается вручную.
/// </summary>
public class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Emoji).HasMaxLength(16).IsRequired();

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Один юзер = одна реакция на конкретное сообщение конкретного контекста.
        builder.HasIndex(r => new { r.MessageId, r.MessageContext, r.UserId }).IsUnique();
    }
}
