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
        services.AddSingleton<AppShell>();
        services.AddSingleton<Func<AppShell>>(provider =>
            () => provider.GetRequiredService<AppShell>());
        services.AddSingleton<CategoryEditorViewModel>();
        services.AddSingleton<CategoryEditorPage>();
        services.AddTransient<ExpenseEditorViewModel>();
        services.AddTransient<ExpenseEditorPage>();
        services.AddTransient<ExpenseHistoryViewModel>();
        services.AddTransient<ExpenseHistoryPage>();
        return services;
    }
}
