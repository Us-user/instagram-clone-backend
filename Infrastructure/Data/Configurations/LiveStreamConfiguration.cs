using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация <see cref="LiveStream"/> (модуль эфиров). FK на хоста каскадом. <c>RoomName</c>
/// уникален (имя комнаты провайдера). Индексы: по <c>UserId</c> (эфиры хоста / запрет второго
/// активного), по <c>Status</c> (выборка активных / фоновое автозавершение).
/// </summary>
public class LiveStreamConfiguration : IEntityTypeConfiguration<LiveStream>
{
    public void Configure(EntityTypeBuilder<LiveStream> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title).HasMaxLength(200);
        builder.Property(s => s.RoomName).IsRequired().HasMaxLength(128);
        builder.Property(s => s.RecordingUrl).HasMaxLength(512);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.RoomName).IsUnique();
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.Status);
    }
}
