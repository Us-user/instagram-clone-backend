using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>Конфигурация <see cref="PostFavorite"/>. Уникален на пару (Post, User).</summary>
public class PostFavoriteConfiguration : IEntityTypeConfiguration<PostFavorite>
{
    public void Configure(EntityTypeBuilder<PostFavorite> builder)
    {
        builder.HasKey(f => f.Id);

        builder.HasOne(f => f.Post)
            .WithMany(p => p.Favorites)
            .HasForeignKey(f => f.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.User)
            .WithMany(u => u.PostFavorites)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Пост в избранном пользователя только один раз.
        builder.HasIndex(f => new { f.PostId, f.UserId }).IsUnique();
    }
}
