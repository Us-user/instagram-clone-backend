using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>Конфигурация <see cref="SearchHistory"/> (история текстового поиска).</summary>
public class SearchHistoryConfiguration : IEntityTypeConfiguration<SearchHistory>
{
    public void Configure(EntityTypeBuilder<SearchHistory> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Text)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasOne(h => h.User)
            .WithMany(u => u.SearchHistories)
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => h.UserId);
    }
}
