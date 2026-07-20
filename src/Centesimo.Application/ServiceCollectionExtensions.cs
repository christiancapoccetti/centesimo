using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<CategoryService>();
        services.AddScoped<TagService>();
        services.AddScoped<ExpenseService>();
        services.AddScoped<BudgetOverviewService>();
        services.AddScoped<RecurringPaymentService>();
        return services;
    }
}
