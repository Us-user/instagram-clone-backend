using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация <see cref="PrivacySettings"/>: связь 1:1 с пользователем (уникальный
/// <c>UserId</c>), каскадное удаление вместе с пользователем. Enum-настройки хранятся как int.
/// </summary>
public class PrivacySettingsConfiguration : IEntityTypeConfiguration<PrivacySettings>
{
    public void Configure(EntityTypeBuilder<PrivacySettings> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasOne(p => p.User)
            .WithOne(u => u.PrivacySettings)
            .HasForeignKey<PrivacySettings>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.UserId).IsUnique();

        builder.Property(p => p.ShowOnlineStatus).HasDefaultValue(true);
    }
}
