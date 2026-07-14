using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация <see cref="BackupCode"/> (§11). FK на пользователя каскадом (при удалении юзера
/// коды уходят). Индекс по <c>(UserId, IsUsed)</c> — быстрый поиск неиспользованного кода при входе.
/// </summary>
public class BackupCodeConfiguration : IEntityTypeConfiguration<BackupCode>
{
    public void Configure(EntityTypeBuilder<BackupCode> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.CodeHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => new { b.UserId, b.IsUsed });
    }
}
