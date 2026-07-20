using System.Collections.ObjectModel;
using System.Windows.Input;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App.ViewModels;

public sealed class CategoriesViewModel(CategoryService categoryService) : ObservableObject
{
    private bool _isLoading;
    private string _errorMessage = "";

    public ObservableCollection<CategoryItemViewModel> Categories { get; } = [];
    public ICommand LoadCommand => new AsyncCommand(Load);
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
    public bool HasCategories => Categories.Count > 0;
    public bool IsEmpty => !IsLoading && !HasError && !HasCategories;

    public async Task Load()
    {
        IsLoading = true;
        ErrorMessage = "";
        var result = await categoryService.GetActive();
        Categories.Clear();
        if (result.IsFailure)
            ErrorMessage = result.Error.Message;
        else
            foreach (var category in result.Value)
                Categories.Add(CategoryItemViewModel.From(category));

        IsLoading = false;
        NotifyState();
    }

    public async Task<Result> Archive(Guid categoryId)
    {
        var result = await categoryService.Archive(categoryId);
        if (result.IsSuccess)
            await Load();
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
