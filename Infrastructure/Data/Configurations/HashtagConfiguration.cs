using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>Конфигурация <see cref="Hashtag"/>: тег уникален и нормализован (lowercase).</summary>
public class HashtagConfiguration : IEntityTypeConfiguration<Hashtag>
{
    public void Configure(EntityTypeBuilder<Hashtag> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Tag).IsRequired().HasMaxLength(100);

        // Один тег — одна запись; ускоряет upsert и поиск по префиксу.
        builder.HasIndex(h => h.Tag).IsUnique();

        // Сортировка по популярности (search/trending).
        builder.HasIndex(h => h.PostsCount);
    }
}
