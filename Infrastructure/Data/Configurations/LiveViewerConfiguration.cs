using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация <see cref="LiveViewer"/> (модуль эфиров). FK на эфир каскадом, на юзера — Restrict,
/// чтобы удаление аккаунта не уносило статистику просмотров (совпадает с политикой прочих связей юзера
/// в аналитике). Составной индекс <c>(LiveStreamId, UserId)</c> — поиск активного захода / уникальные зрители.
/// </summary>
public class LiveViewerConfiguration : IEntityTypeConfiguration<LiveViewer>
{
    public void Configure(EntityTypeBuilder<LiveViewer> builder)
    {
        builder.HasKey(v => v.Id);

        builder.HasOne(v => v.LiveStream)
            .WithMany(s => s.Viewers)
            .HasForeignKey(v => v.LiveStreamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => new { v.LiveStreamId, v.UserId });
    }
}
