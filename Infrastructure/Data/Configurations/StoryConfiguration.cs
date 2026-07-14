using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>Конфигурация <see cref="Story"/>.</summary>
public class StoryConfiguration : IEntityTypeConfiguration<Story>
{
    public void Configure(EntityTypeBuilder<Story> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.FileName).HasMaxLength(512);

        builder.HasOne(s => s.User)
            .WithMany(u => u.Stories)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Пост-источник опционален; при удалении поста сторис остаётся, ссылка обнуляется.
        builder.HasOne(s => s.Post)
            .WithMany()
            .HasForeignKey(s => s.PostId)
            .OnDelete(DeleteBehavior.SetNull);

        // Репост поста в сторис (§9) опционален; при удалении оригинала ссылка обнуляется.
        builder.HasOne(s => s.SharedPost)
            .WithMany()
            .HasForeignKey(s => s.SharedPostId)
            .OnDelete(DeleteBehavior.SetNull);

        // Лента сторис фильтрует по автору и сроку жизни (< 24ч).
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.CreatedAt);
    }
}
