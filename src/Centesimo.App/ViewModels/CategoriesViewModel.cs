using System.Collections.ObjectModel;
using System.Windows.Input;
using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App.ViewModels;

public sealed class CategoriesViewModel(IServiceScopeFactory scopeFactory) : ObservableObject
{
    private bool _isLoading;
    private bool _isArchivedView;
    private string _errorMessage = "";

    public ObservableCollection<CategoryItemViewModel> Categories { get; } = [];
    public ObservableCollection<CategoryItemViewModel> ArchivedCategories { get; } = [];
    public ICommand LoadCommand => new AsyncCommand(Load);
    public bool IsArchivedView
    {
        get => _isArchivedView;
        private set
        {
            if (!SetProperty(ref _isArchivedView, value))
                return;

            OnPropertyChanged(nameof(IsActiveView));
            OnPropertyChanged(nameof(DisplayedCategories));
            NotifyState();
        }
    }
    public bool IsActiveView => !IsArchivedView;
    public ObservableCollection<CategoryItemViewModel> DisplayedCategories =>
        IsArchivedView ? ArchivedCategories : Categories;
    public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }
    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
                OnPropertyChanged(nameof(HasError));
        }
    }
    public bool HasError => ErrorMessage.HasValue();
    public bool HasCategories => DisplayedCategories.Count > 0;
    public bool IsEmpty => !IsLoading && !HasError && !HasCategories;

    public async Task Load()
    {
        IsArchivedView = false;
        await LoadCategories(service => service.GetActive(), Categories);
    }

    public async Task LoadArchived()
    {
        IsArchivedView = true;
        await LoadCategories(service => service.GetArchived(), ArchivedCategories);
    }

    public Task ShowActive() => Load();

    public Task ShowArchived() => LoadArchived();

    private async Task LoadCategories(
        Func<CategoryService, Task<Result<IReadOnlyList<Category>>>> getCategories,
        ObservableCollection<CategoryItemViewModel> destination)
    {
        IsLoading = true;
        ErrorMessage = "";
        Result<IReadOnlyList<Category>> result;
        using (var scope = scopeFactory.CreateScope())
        {
            var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();
            result = await getCategories(categoryService);
        }
        destination.Clear();
        if (result.IsFailure)
            ErrorMessage = result.Error.Message;
        else
            foreach (var category in result.Value)
                destination.Add(CategoryItemViewModel.From(category));

        IsLoading = false;
        NotifyState();
    }

    public async Task<Result> Archive(Guid categoryId)
    {
        Result result;
        using (var scope = scopeFactory.CreateScope())
        {
            var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();
            result = await categoryService.Archive(categoryId);
        }
        if (result.IsSuccess)
            await LoadCategories(service => service.GetActive(), Categories);
        else
            ErrorMessage = result.Error.Message;

        return result;
    }

    public async Task<Result> Restore(Guid categoryId)
    {
        Result result;
        using (var scope = scopeFactory.CreateScope())
        {
            var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();
            result = await categoryService.Restore(categoryId);
        }
        if (result.IsSuccess)
            await LoadCategories(service => service.GetArchived(), ArchivedCategories);
        else
            ErrorMessage = result.Error.Message;

        return result;
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(HasCategories));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasError));
    }
}

public sealed record CategoryItemViewModel(
    Guid CategoryId,
    string Name,
    string Icon,
    string Color,
    string BudgetText,
    string BudgetInput)
{
    public static CategoryItemViewModel From(Category category) => new(
        category.CategoryId,
        category.Name,
        category.Icon,
        category.Color,
        category.MonthlyBudget is null
            ? "Nessun budget"
            : $"Budget: {category.MonthlyBudget.Value.ToDecimal().ToString("C", System.Globalization.CultureInfo.GetCultureInfo("it-IT"))}",
        category.MonthlyBudget?.ToDecimal().ToString("0.00", System.Globalization.CultureInfo.GetCultureInfo("it-IT")) ?? "");
}
