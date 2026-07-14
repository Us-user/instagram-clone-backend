using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>Конфигурация <see cref="User"/> (расширяет таблицу AspNetUsers).</summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.Avatar)
            .HasMaxLength(512);

        builder.Property(u => u.IsVerified)
            .HasDefaultValue(false);

        builder.Property(u => u.IsPrivate)
            .HasDefaultValue(false);

        // Секрет TOTP (Base32) для 2FA (§11); nullable — задаётся при включении.
        builder.Property(u => u.TwoFactorSecret)
            .HasMaxLength(128);
    }
}
