using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация <see cref="UserSession"/> (модуль сессий). FK на пользователя каскадом
/// (удаление юзера удаляет его сессии). Индексы: по <c>UserId</c> (список сессий юзера),
/// по <c>RefreshTokenHash</c> и <c>PreviousRefreshTokenHash</c> (поиск/reuse-detection при refresh),
/// по <c>ExpiresAt</c> (фоновая очистка).
/// </summary>
public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.RefreshTokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(s => s.PreviousRefreshTokenHash)
            .HasMaxLength(128);

        builder.Property(s => s.DeviceName).HasMaxLength(256);
        builder.Property(s => s.Browser).HasMaxLength(128);
        builder.Property(s => s.OS).HasMaxLength(128);
        builder.Property(s => s.IpAddress).HasMaxLength(64);
        builder.Property(s => s.Location).HasMaxLength(256);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.RefreshTokenHash);
        builder.HasIndex(s => s.PreviousRefreshTokenHash);
        builder.HasIndex(s => s.ExpiresAt);
    }
}
