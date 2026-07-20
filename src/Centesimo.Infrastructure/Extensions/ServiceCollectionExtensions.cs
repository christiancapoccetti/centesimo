using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string databasePath)
    {
        if (databasePath.IsEmpty())
            throw new ArgumentException("Database path is required.", nameof(databasePath));

        services.AddDbContext<CentesimoDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IMoneyManagerBackupReader, MoneyManagerBackupReader>();
        services.AddScoped<IMoneyManagerImportRepository, MoneyManagerImportRepository>();
        services.AddScoped<IRecurringPaymentRepository, RecurringPaymentRepository>();
        services.AddScoped<IRecurringOccurrenceProcessor, RecurringOccurrenceProcessor>();
        return services;
    }
}
