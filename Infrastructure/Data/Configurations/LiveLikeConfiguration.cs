using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация <see cref="LiveLike"/> (модуль эфиров). Поток «сердечек» (не тумблер), FK на эфир
/// каскадом, на юзера — Restrict. Индекс по <c>LiveStreamId</c>.
/// </summary>
public class LiveLikeConfiguration : IEntityTypeConfiguration<LiveLike>
{
    public void Configure(EntityTypeBuilder<LiveLike> builder)
    {
        builder.HasKey(l => l.Id);

        builder.HasOne(l => l.LiveStream)
            .WithMany(s => s.Likes)
            .HasForeignKey(l => l.LiveStreamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.LiveStreamId);
    }
}
