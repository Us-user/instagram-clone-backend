using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация <see cref="GroupChatMember"/> (§7). Пара (GroupChatId, UserId) уникальна —
/// один пользователь входит в группу один раз. FK на группу и на пользователя — каскадом
/// (PostgreSQL допускает несколько каскадных путей к таблице пользователей).
/// </summary>
public class GroupChatMemberConfiguration : IEntityTypeConfiguration<GroupChatMember>
{
    public void Configure(EntityTypeBuilder<GroupChatMember> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasOne(m => m.GroupChat)
            .WithMany(g => g.Members)
            .HasForeignKey(m => m.GroupChatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.GroupChatId, m.UserId }).IsUnique();
        builder.HasIndex(m => m.UserId);
    }
}
