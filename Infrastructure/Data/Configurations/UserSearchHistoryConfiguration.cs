using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация <see cref="UserSearchHistory"/> (история просмотренных профилей).
/// Две ссылки на пользователя — обе каскадные.
/// </summary>
public class UserSearchHistoryConfiguration : IEntityTypeConfiguration<UserSearchHistory>
{
    public void Configure(EntityTypeBuilder<UserSearchHistory> builder)
    {
        builder.HasKey(h => h.Id);

        builder.HasOne(h => h.User)
            .WithMany()
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.SearchedUser)
            .WithMany()
            .HasForeignKey(h => h.SearchedUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => h.UserId);
    }
}
