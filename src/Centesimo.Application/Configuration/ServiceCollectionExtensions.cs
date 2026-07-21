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
        services.AddScoped<MonthlyOverviewService>();
        services.AddScoped<IInsightsService, InsightsService>();
        services.AddScoped<CategorySpendingService>();
        services.AddScoped<MoneyManagerImportService>();
        services.AddScoped<RecurringPaymentService>();
        services.AddScoped<UpcomingRecurringPaymentService>();
        services.AddSingleton<ExpenseSpeechCommandParser>();
        services.AddSingleton<ExpenseSpeechDraftResolver>();
        services.AddSingleton<IPendingExpenseSpeechDraft, PendingExpenseSpeechDraft>();
        return services;
    }
}
