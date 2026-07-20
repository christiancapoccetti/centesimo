using Centesimo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Centesimo.Infrastructure;

public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");
        builder.HasKey(expense => expense.ExpenseId);
        builder.Property(expense => expense.Amount)
            .HasConversion(money => money.Cents, cents => new Money(cents));
        builder.Property(expense => expense.Note).HasMaxLength(500).IsRequired();
        builder.Property(expense => expense.PhotoPath).HasMaxLength(260);
        builder.Property(expense => expense.RecurringPaymentId);
        builder.HasIndex(expense => expense.OccurredOn);
        builder.HasIndex(expense => new { expense.CategoryId, expense.OccurredOn });
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(expense => expense.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Tag>()
            .WithMany()
            .HasForeignKey(expense => expense.TagId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

