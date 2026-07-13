using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация связки M:N <see cref="PostHashtag"/>. Обе FK каскадом: при удалении поста
/// или тега связки удаляются автоматически. Пара (PostId, HashtagId) уникальна.
/// </summary>
public class PostHashtagConfiguration : IEntityTypeConfiguration<PostHashtag>
{
    public void Configure(EntityTypeBuilder<PostHashtag> builder)
    {
        builder.HasKey(ph => ph.Id);

        builder.HasOne(ph => ph.Post)
            .WithMany(p => p.PostHashtags)
            .HasForeignKey(ph => ph.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ph => ph.Hashtag)
            .WithMany(h => h.PostHashtags)
            .HasForeignKey(ph => ph.HashtagId)
            .OnDelete(DeleteBehavior.Cascade);

        // Один тег на пост не дублируется; заодно быстрый поиск постов по тегу.
        builder.HasIndex(ph => new { ph.HashtagId, ph.PostId }).IsUnique();
    }
}
