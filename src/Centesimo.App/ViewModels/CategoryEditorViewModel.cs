using System.Globalization;
using System.Windows.Input;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App.ViewModels;

public sealed class CategoryEditorViewModel : ObservableObject
{
    private readonly CategoryService _categoryService;
    private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");
    private Guid? _categoryId;
    private string _name = "";
    private string _selectedIcon = "cart";
    private string _selectedColor = "#176B5B";
    private string _budget = "";
    private string _errorMessage = "";
    private bool _isSaving;

    public event EventHandler? Saved;

    public IReadOnlyList<string> Icons { get; } = ["cart", "home", "car", "heart", "more"];
    public IReadOnlyList<string> Colors { get; } = ["#176B5B", "#8B4A5D", "#725C00", "#4F5F7A", "#6A4F88"];
    public ICommand SaveCommand { get; }
    public ICommand ClearBudgetCommand { get; }
    public string Title => _categoryId.HasValue ? "Modifica categoria" : "Nuova categoria";
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string SelectedIcon { get => _selectedIcon; set => SetProperty(ref _selectedIcon, value); }
    public string SelectedColor { get => _selectedColor; set => SetProperty(ref _selectedColor, value); }
    public string Budget { get => _budget; set => SetProperty(ref _budget, value); }
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
    public bool IsSaving { get => _isSaving; private set => SetProperty(ref _isSaving, value); }

    public CategoryEditorViewModel(CategoryService categoryService)
    {
        _categoryService = categoryService;
        SaveCommand = new AsyncCommand(Save, () => !IsSaving);
        ClearBudgetCommand = new RelayCommand(() => Budget = "");
    }

    public void OpenNew()
    {
        _categoryId = null;
        Name = "";
        SelectedIcon = Icons[0];
        SelectedColor = Colors[0];
        Budget = "";
        ErrorMessage = "";
        OnPropertyChanged(nameof(Title));
    }

    public void OpenEdit(CategoryItemViewModel category)
    {
        _categoryId = category.CategoryId;
        Name = category.Name;
        SelectedIcon = category.Icon;
        SelectedColor = category.Color;
        Budget = category.BudgetInput;
        ErrorMessage = "";
        OnPropertyChanged(nameof(Title));
    }

    private async Task Save()
    {
        if (Name.IsEmpty())
        {
            ErrorMessage = "Il nome è obbligatorio.";
            return;
        }

        var budgetResult = MoneyInputParser.ParseOptional(Budget, ItalianCulture);
        if (budgetResult.IsFailure)
        {
            ErrorMessage = budgetResult.Error.Message;
            return;
        }

        IsSaving = true;
        var result = _categoryId.HasValue
            ? await _categoryService.Update(_categoryId.Value, Name, SelectedIcon, SelectedColor, budgetResult.Value)
            : await Create(budgetResult.Value);
        IsSaving = false;
        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        Saved?.Invoke(this, EventArgs.Empty);
    }

    private async Task<Result> Create(Money? budget)
    {
        var result = await _categoryService.Create(Name, SelectedIcon, SelectedColor, budget);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }
}
