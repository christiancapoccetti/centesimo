using Centesimo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Centesimo.Infrastructure;

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.HasKey(tag => tag.TagId);
        builder.Property(tag => tag.Name).HasMaxLength(80).IsRequired();
        builder.HasIndex(tag => new { tag.CategoryId, tag.Name }).IsUnique();
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(tag => tag.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
