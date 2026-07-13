using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>Конфигурация <see cref="PostView"/>. Уникален на пару (Post, User).</summary>
public class PostViewConfiguration : IEntityTypeConfiguration<PostView>
{
    public void Configure(EntityTypeBuilder<PostView> builder)
    {
        builder.HasKey(v => v.Id);

        builder.HasOne(v => v.Post)
            .WithMany(p => p.Views)
            .HasForeignKey(v => v.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.User)
            .WithMany(u => u.PostViews)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Уникальный просмотр на пользователя.
        builder.HasIndex(v => new { v.PostId, v.UserId }).IsUnique();
    }
}
