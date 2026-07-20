using Centesimo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Centesimo.Infrastructure;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        var budgetConverter = new ValueConverter<Money?, long?>(
            money => money.HasValue ? money.Value.Cents : null,
            cents => cents.HasValue ? new Money(cents.Value) : null);
        builder.ToTable("Categories");
        builder.HasKey(category => category.CategoryId);
        builder.Property(category => category.Name).HasMaxLength(80).IsRequired();
        builder.Property(category => category.Icon).HasMaxLength(40).IsRequired();
        builder.Property(category => category.Color).HasMaxLength(9).IsRequired();
        builder.Property(category => category.MonthlyBudget)
            .HasConversion(budgetConverter);
        builder.HasIndex(category => category.Name).IsUnique();
    }
}


