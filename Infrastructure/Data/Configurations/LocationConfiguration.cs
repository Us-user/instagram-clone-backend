using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>Конфигурация <see cref="Location"/>.</summary>
public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.City).IsRequired().HasMaxLength(128);
        builder.Property(l => l.State).IsRequired().HasMaxLength(128);
        builder.Property(l => l.ZipCode).IsRequired().HasMaxLength(32);
        builder.Property(l => l.Country).IsRequired().HasMaxLength(128);

        // Фильтрация справочника по городу/стране.
        builder.HasIndex(l => l.City);
        builder.HasIndex(l => l.Country);
    }
}
