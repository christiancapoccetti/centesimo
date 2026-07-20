using Centesimo.App.Pages;

namespace Centesimo.App;

public partial class AppShell : Shell
{
    public AppShell(TodayPage todayPage, CategoriesPage categoriesPage, SettingsPage settingsPage)
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(ExpenseEditorPage), typeof(ExpenseEditorPage));
        Routing.RegisterRoute(nameof(CategorySpendingPage), typeof(CategorySpendingPage));
        Routing.RegisterRoute(nameof(TagExpensesPage), typeof(TagExpensesPage));
        Routing.RegisterRoute(nameof(RecurringPaymentsPage), typeof(RecurringPaymentsPage));
        Routing.RegisterRoute(nameof(RecurringPaymentEditorPage), typeof(RecurringPaymentEditorPage));
        Items.Add(new TabBar
        {
            Items =
            {
                CreateTab("Oggi", "TodayPage", "⌂", todayPage),
                CreateTab("Categorie", "CategoriesPage", "▦", categoriesPage),
                CreateTab("Impostazioni", "SettingsPage", "⚙", settingsPage)
            }
        });
    }

    private static ShellContent CreateTab(string title, string route, string glyph, Page page) => new()
    {
        Title = title,
        Route = route,
        Icon = new FontImageSource { Glyph = glyph, FontFamily = "OpenSansSemibold", Size = 24 },
        Content = page
    };
}
