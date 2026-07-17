using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация <see cref="LiveBan"/> (модуль эфиров). FK на эфир каскадом, на юзера — Restrict.
/// Уникальный индекс <c>(LiveStreamId, UserId)</c> — один бан на пользователя в эфире (идемпотентность).
/// </summary>
public class LiveBanConfiguration : IEntityTypeConfiguration<LiveBan>
{
    public void Configure(EntityTypeBuilder<LiveBan> builder)
    {
        builder.HasKey(b => b.Id);

        builder.HasOne(b => b.LiveStream)
            .WithMany(s => s.Bans)
            .HasForeignKey(b => b.LiveStreamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => new { b.LiveStreamId, b.UserId }).IsUnique();
    }
}
