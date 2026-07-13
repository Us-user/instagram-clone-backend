using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>Конфигурация <see cref="StoryView"/>. Уникален на пару (Story, ViewUser).</summary>
public class StoryViewConfiguration : IEntityTypeConfiguration<StoryView>
{
    public void Configure(EntityTypeBuilder<StoryView> builder)
    {
        builder.HasKey(v => v.Id);

        builder.HasOne(v => v.Story)
            .WithMany(s => s.Views)
            .HasForeignKey(v => v.StoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.ViewUser)
            .WithMany(u => u.StoryViews)
            .HasForeignKey(v => v.ViewUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Один просмотр на пользователя для сторис.
        builder.HasIndex(v => new { v.StoryId, v.ViewUserId }).IsUnique();
    }
}
