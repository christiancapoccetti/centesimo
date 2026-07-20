using Centesimo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Centesimo.Infrastructure;

public sealed class CentesimoDbContext(DbContextOptions<CentesimoDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<RecurringPayment> RecurringPayments => Set<RecurringPayment>();
    public DbSet<RecurrenceOccurrence> RecurrenceOccurrences => Set<RecurrenceOccurrence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CentesimoDbContext).Assembly);
}
