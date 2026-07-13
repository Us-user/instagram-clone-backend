using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>Конфигурация <see cref="PostImage"/>.</summary>
public class PostImageConfiguration : IEntityTypeConfiguration<PostImage>
{
    public void Configure(EntityTypeBuilder<PostImage> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.ImageName)
            .IsRequired()
            .HasMaxLength(512);

        builder.HasOne(i => i.Post)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
