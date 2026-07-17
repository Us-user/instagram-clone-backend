using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация <see cref="LiveComment"/> (модуль эфиров). FK на эфир каскадом, на юзера — Restrict.
/// Индекс по <c>LiveStreamId</c> — история комментов эфира с пагинацией.
/// </summary>
public class LiveCommentConfiguration : IEntityTypeConfiguration<LiveComment>
{
    public void Configure(EntityTypeBuilder<LiveComment> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Text).IsRequired().HasMaxLength(1000);

        builder.HasOne(c => c.LiveStream)
            .WithMany(s => s.Comments)
            .HasForeignKey(c => c.LiveStreamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.LiveStreamId);
    }
}
