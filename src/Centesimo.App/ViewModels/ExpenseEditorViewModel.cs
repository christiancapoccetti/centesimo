using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App.ViewModels;

public sealed class ExpenseEditorViewModel : ObservableObject
{
    private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");
    private readonly CategoryService _categoryService;
    private readonly TagService _tagService;
    private readonly ExpenseService _expenseService;
    private Guid? _expenseId;
    private string _amount = "";
    private CategoryOption? _selectedCategory;
    private TagOption? _selectedTag;
    private DateTime _occurredOn = DateTime.Today;
    private string _note = "";
    private string _errorMessage = "";
    private bool _isLoading;
    private bool _isSaving;

    public event EventHandler? Saved;

    public ObservableCollection<CategoryOption> Categories { get; } = [];
    public ObservableCollection<TagOption> Tags { get; } = [];
    public ICommand SaveCommand { get; }
    public string Title => _expenseId.HasValue ? "Modifica spesa" : "Nuova spesa";
    public string Amount { get => _amount; set => SetProperty(ref _amount, value); }
    public CategoryOption? SelectedCategory { get => _selectedCategory; set => SetProperty(ref _selectedCategory, value); }
    public TagOption? SelectedTag { get => _selectedTag; set => SetProperty(ref _selectedTag, value); }
    public DateTime OccurredOn { get => _occurredOn; set => SetProperty(ref _occurredOn, value); }
    public string Note { get => _note; set => SetProperty(ref _note, value); }
    public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }
    public bool IsSaving { get => _isSaving; private set => SetProperty(ref _isSaving, value); }
    public bool HasTags => Tags.Count > 0;
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
    public bool IsEditMode => _expenseId.HasValue;

    public ExpenseEditorViewModel(
        CategoryService categoryService,
        TagService tagService,
        ExpenseService expenseService)
    {
        _categoryService = categoryService;
        _tagService = tagService;
        _expenseService = expenseService;
        SaveCommand = new AsyncCommand(Save, () => !IsSaving);
    }

    public async Task Load(Guid? expenseId = null)
    {
        Reset();
        _expenseId = expenseId;
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(IsEditMode));
        IsLoading = true;
        var result = await _categoryService.GetActive();
        if (result.IsFailure)
            ErrorMessage = result.Error.Message;
        else
            foreach (var category in result.Value)
                Categories.Add(new CategoryOption(category.CategoryId, category.Name));

        if (_expenseId.HasValue && !HasError)
            await LoadExpense(_expenseId.Value);

        IsLoading = false;
    }

    public async Task LoadTags()
    {
        Tags.Clear();
        SelectedTag = null;
        if (SelectedCategory is null)
        {
            OnPropertyChanged(nameof(HasTags));
            return;
        }

        var result = await _tagService.GetActive(SelectedCategory.CategoryId);
        if (result.IsFailure)
            ErrorMessage = result.Error.Message;
        else
            foreach (var tag in result.Value)
                Tags.Add(new TagOption(tag.TagId, tag.Name));

        OnPropertyChanged(nameof(HasTags));
    }

    private async Task LoadExpense(Guid expenseId)
    {
        var result = await _expenseService.Get(expenseId);
        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        if (result.Value is null)
        {
            ErrorMessage = ApplicationErrors.ExpenseNotFound.Message;
            return;
        }

        var expense = result.Value;
        Amount = expense.Amount.ToDecimal().ToString("0.00", ItalianCulture);
        OccurredOn = expense.OccurredOn.ToDateTime(TimeOnly.MinValue);
        Note = expense.Note;
        SelectedCategory = Categories.FirstOrDefault(category => category.CategoryId == expense.CategoryId);
        if (SelectedCategory is null)
        {
            ErrorMessage = "La categoria originale non è più attiva. Seleziona una nuova categoria.";
            return;
        }

        await LoadTags();
        SelectedTag = Tags.FirstOrDefault(tag => tag.TagId == expense.TagId);
        if (expense.TagId.HasValue && SelectedTag is null)
            ErrorMessage = "Il tag originale non è più attivo. Scegli un tag disponibile o salva senza tag.";
    }

    public async Task<Result> Delete()
    {
        if (!_expenseId.HasValue)
            return Result.Failure(ApplicationErrors.ExpenseNotFound);

        IsSaving = true;
        ErrorMessage = "";
        var result = await _expenseService.Delete(_expenseId.Value);
        IsSaving = false;
        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return result;
        }

        Saved?.Invoke(this, EventArgs.Empty);
        return result;
    }
    private async Task Save()
    {
        var amountResult = MoneyInputParser.ParseOptional(Amount, ItalianCulture);
        if (amountResult.IsFailure || amountResult.Value is null || amountResult.Value.Value.Cents <= 0)
        {
            ErrorMessage = "Inserisci un importo maggiore di zero.";
            return;
        }

        if (SelectedCategory is null)
        {
            ErrorMessage = "Seleziona una categoria.";
            return;
        }

        IsSaving = true;
        ErrorMessage = "";
        var request = new SaveExpenseRequest(
            SelectedCategory.CategoryId,
            amountResult.Value.Value.Cents,
            DateOnly.FromDateTime(OccurredOn),
            SelectedTag?.TagId,
            Note);
        Result result;
        if (_expenseId.HasValue)
            result = await _expenseService.Update(_expenseId.Value, request);
        else
        {
            var created = await _expenseService.Create(request);
            result = created.IsSuccess ? Result.Success() : Result.Failure(created.Error);
        }
        IsSaving = false;
        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        Saved?.Invoke(this, EventArgs.Empty);
    }

    private void Reset()
    {
        Amount = "";
        SelectedCategory = null;
        SelectedTag = null;
        OccurredOn = DateTime.Today;
        Note = "";
        ErrorMessage = "";
        Categories.Clear();
        Tags.Clear();
        OnPropertyChanged(nameof(HasTags));
    }

    public sealed record CategoryOption(Guid CategoryId, string Name);
    public sealed record TagOption(Guid TagId, string Name);
}
