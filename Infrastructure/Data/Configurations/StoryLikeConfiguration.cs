using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>Конфигурация <see cref="StoryLike"/>. Уникален на пару (Story, User).</summary>
public class StoryLikeConfiguration : IEntityTypeConfiguration<StoryLike>
{
    public void Configure(EntityTypeBuilder<StoryLike> builder)
    {
        builder.HasKey(l => l.Id);

        builder.HasOne(l => l.Story)
            .WithMany(s => s.Likes)
            .HasForeignKey(l => l.StoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.User)
            .WithMany(u => u.StoryLikes)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => new { l.StoryId, l.UserId }).IsUnique();
    }
}
