using Centesimo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Centesimo.Infrastructure;

public sealed class RecurringPaymentConfiguration : IEntityTypeConfiguration<RecurringPayment>
{
    public void Configure(EntityTypeBuilder<RecurringPayment> builder)
    {
        builder.ToTable("RecurringPayments");
        builder.HasKey(payment => payment.RecurringPaymentId);
        builder.Property(payment => payment.Amount)
            .HasConversion(money => money.Cents, cents => new Money(cents));
        builder.Property(payment => payment.Note).HasMaxLength(500).IsRequired();
        builder.HasIndex(payment => payment.NextDueOn);
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(payment => payment.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Tag>()
            .WithMany()
            .HasForeignKey(payment => payment.TagId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
