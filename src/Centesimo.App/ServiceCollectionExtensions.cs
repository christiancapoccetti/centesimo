using Centesimo.App.Pages;
using Centesimo.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUi(this IServiceCollection services)
    {
        services.AddSingleton<TodayPage>();
        services.AddSingleton<CategoriesViewModel>();
        services.AddSingleton<CategoriesPage>();
        services.AddSingleton<RecurringPaymentsPage>();
        services.AddSingleton<AppShell>();
        services.AddTransient<CategoryEditorViewModel>();
        services.AddTransient<CategoryEditorPage>();
        return services;
    }
}
