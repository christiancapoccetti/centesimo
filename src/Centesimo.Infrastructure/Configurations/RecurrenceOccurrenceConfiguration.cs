using Centesimo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Centesimo.Infrastructure;

public sealed class RecurrenceOccurrenceConfiguration : IEntityTypeConfiguration<RecurrenceOccurrence>
{
    public void Configure(EntityTypeBuilder<RecurrenceOccurrence> builder)
    {
        builder.ToTable("RecurrenceOccurrences");
        builder.HasKey(occurrence => new { occurrence.RecurringPaymentId, occurrence.DueOn });
        builder.HasIndex(occurrence => new { occurrence.RecurringPaymentId, occurrence.DueOn })
            .IsUnique();
    }
}
