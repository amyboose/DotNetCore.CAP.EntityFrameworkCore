using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetCore.CAP.EntityFrameworkCore.Persistance.PostgreSQL.Configurations;
internal class PublishedEventConfiguration : IEntityTypeConfiguration<PublishedOutbox>
{
    private readonly string? _schema;

    public PublishedEventConfiguration(string? schema)
    {
        _schema = schema;
    }

    public void Configure(EntityTypeBuilder<PublishedOutbox> builder)
    {
        builder
            .ToTable("published", _schema);

        builder
            .HasKey(p => p.Id)
            .HasName("published_pkey");

        builder
            .Property(p => p.Version)
            .HasMaxLength(PublishedOutbox.MaxVersionPropertyLegth);

        builder
            .Property(p => p.Name)
            .HasMaxLength(PublishedOutbox.MaxNamePropertyLegth);

        builder
            .Property(p => p.StatusName)
            .HasMaxLength(PublishedOutbox.MaxStatusNamePropertyLength);

        builder
            .Property(p => p.Added)
            .HasColumnType("TIMESTAMP");

        builder
            .Property(p => p.ExpiresAt)
            .HasColumnType("TIMESTAMP");
    }
}
