using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация <see cref="LiveGuestRequest"/> (модуль эфиров). FK на эфир каскадом, на юзера — Restrict.
/// Составной индекс <c>(LiveStreamId, Status)</c> — очередь Pending-заявок / список активных гостей.
/// </summary>
public class LiveGuestRequestConfiguration : IEntityTypeConfiguration<LiveGuestRequest>
{
    public void Configure(EntityTypeBuilder<LiveGuestRequest> builder)
    {
        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.LiveStream)
            .WithMany(s => s.GuestRequests)
            .HasForeignKey(r => r.LiveStreamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.LiveStreamId, r.Status });
    }
}
