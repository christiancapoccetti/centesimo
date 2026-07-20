using Centesimo.App.Pages;

namespace Centesimo.App;

public partial class AppShell : Shell
{
    public AppShell(
        TodayPage todayPage,
        CategoriesPage categoriesPage,
        RecurringPaymentsPage recurringPaymentsPage)
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(ExpenseEditorPage), typeof(ExpenseEditorPage));
        var tabs = new TabBar
        {
            Items =
            {
                CreateTab("Oggi", "TodayPage", todayPage),
                CreateTab("Categorie", "CategoriesPage", categoriesPage),
                CreateTab("Regolari", "RecurringPaymentsPage", recurringPaymentsPage)
            }
        };
        Items.Add(tabs);
    }

    private static ShellContent CreateTab(string title, string route, Page page) => new()
    {
        Title = title,
        Route = route,
        Content = page
    };
}
