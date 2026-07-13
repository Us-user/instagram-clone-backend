using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация <see cref="CommentLike"/> (Phase 14). FK на комментарий и на пользователя —
/// обе каскадом (PostgreSQL допускает несколько каскадных путей к таблице пользователей,
/// как у <see cref="PostLike"/>). Один лайк на пользователя для комментария — пара
/// (CommentId, UserId) уникальна.
/// </summary>
public class CommentLikeConfiguration : IEntityTypeConfiguration<CommentLike>
{
    public void Configure(EntityTypeBuilder<CommentLike> builder)
    {
        builder.HasKey(l => l.Id);

        builder.HasOne(l => l.Comment)
            .WithMany(c => c.CommentLikes)
            .HasForeignKey(l => l.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => new { l.CommentId, l.UserId }).IsUnique();
    }
}
