using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>Конфигурация <see cref="GroupChat"/> (§7).</summary>
public class GroupChatConfiguration : IEntityTypeConfiguration<GroupChat>
{
    public void Configure(EntityTypeBuilder<GroupChat> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name).IsRequired().HasMaxLength(100);
        builder.Property(g => g.Avatar).HasMaxLength(512);

        builder.HasOne(g => g.Creator)
            .WithMany()
            .HasForeignKey(g => g.CreatorUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
