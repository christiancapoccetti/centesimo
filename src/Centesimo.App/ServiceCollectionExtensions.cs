using Centesimo.App.Pages;
using Centesimo.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUi(this IServiceCollection services)
    {
        services.AddSingleton<TodayViewModel>();
        services.AddSingleton<TodayPage>();
        services.AddSingleton<CategoriesViewModel>();
        services.AddSingleton<CategoriesPage>();
        services.AddSingleton<RecurringPaymentsPage>();
        services.AddSingleton<AppShell>();
        services.AddSingleton<Func<AppShell>>(provider =>
            () => provider.GetRequiredService<AppShell>());
        services.AddTransient<CategoryEditorViewModel>();
        services.AddTransient<CategoryEditorPage>();
        services.AddTransient<ExpenseEditorViewModel>();
        services.AddTransient<ExpenseEditorPage>();
        return services;
    }
}
