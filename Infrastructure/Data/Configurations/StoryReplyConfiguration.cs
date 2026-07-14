using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация <see cref="StoryReply"/> (§9). Ответ на сторис привязан к самой сторис, автору
/// ответа и созданному личному сообщению. Все FK — каскадом (удаление сторис/сообщения/автора
/// уносит запись-связку); PostgreSQL допускает несколько каскадных путей к пользователям.
/// </summary>
public class StoryReplyConfiguration : IEntityTypeConfiguration<StoryReply>
{
    public void Configure(EntityTypeBuilder<StoryReply> builder)
    {
        builder.HasKey(sr => sr.Id);

        builder.HasOne(sr => sr.Story)
            .WithMany()
            .HasForeignKey(sr => sr.StoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sr => sr.FromUser)
            .WithMany()
            .HasForeignKey(sr => sr.FromUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sr => sr.Message)
            .WithMany()
            .HasForeignKey(sr => sr.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(sr => sr.StoryId);
    }
}
