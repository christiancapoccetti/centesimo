using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App.ViewModels;

public sealed class RecurringPaymentEditorViewModel : ObservableObject
{
    private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");
    private Guid? _paymentId;
    private string _amount = "";
    private ExpenseEditorViewModel.CategoryOption? _selectedCategory;
    private ExpenseEditorViewModel.TagOption? _selectedTag;
    private RecurrenceFrequency _frequency = RecurrenceFrequency.Monthly;
    private DateTime _nextDueOn = DateTime.Today;
    private DateTime? _endsOn;
    private bool _hasEndDate;
    private string _note = "";
    private string _errorMessage = "";
    private bool _isSaving;
    private readonly CategoryService _categoryService;
    private readonly TagService _tagService;
    private readonly RecurringPaymentService _paymentService;
    private readonly RecurringPaymentAutomation _automation;
    public event EventHandler? Saved;
    public ObservableCollection<ExpenseEditorViewModel.CategoryOption> Categories { get; } = [];
    public ObservableCollection<ExpenseEditorViewModel.TagOption> Tags { get; } = [];
    public IReadOnlyList<RecurrenceFrequency> Frequencies { get; } = [RecurrenceFrequency.Weekly, RecurrenceFrequency.Monthly, RecurrenceFrequency.Yearly];
    public ICommand SaveCommand { get; }
    public string Title => _paymentId.HasValue ? "Modifica pagamento" : "Nuovo pagamento";
    public string Amount { get => _amount; set => SetProperty(ref _amount, value); }
    public ExpenseEditorViewModel.CategoryOption? SelectedCategory { get => _selectedCategory; set { SetProperty(ref _selectedCategory, value); foreach (var item in Categories) item.IsSelected = item == value; } }
    public ExpenseEditorViewModel.TagOption? SelectedTag { get => _selectedTag; set => SetProperty(ref _selectedTag, value); }
    public RecurrenceFrequency Frequency { get => _frequency; set => SetProperty(ref _frequency, value); }
    public DateTime NextDueOn { get => _nextDueOn; set => SetProperty(ref _nextDueOn, value); }
    public DateTime? EndsOn { get => _endsOn; set => SetProperty(ref _endsOn, value); }
    public bool HasEndDate
    {
        get => _hasEndDate;
        set
        {
            if (!SetProperty(ref _hasEndDate, value))
                return;

            if (!value)
                EndsOn = null;
            else if (!EndsOn.HasValue)
                EndsOn = NextDueOn > DateTime.Today ? NextDueOn : DateTime.Today;

            OnPropertyChanged(nameof(CanSelectEndDate));
        }
    }
    public bool CanSelectEndDate => HasEndDate;
    public string Note { get => _note; set => SetProperty(ref _note, value); }
    public bool IsSaving { get => _isSaving; private set => SetProperty(ref _isSaving, value); }
    public bool HasTags => Tags.Count > 0;
    public string ErrorMessage { get => _errorMessage; private set { if (SetProperty(ref _errorMessage, value)) OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => ErrorMessage.HasValue();
    public RecurringPaymentEditorViewModel(CategoryService categoryService, TagService tagService,
        RecurringPaymentService paymentService, RecurringPaymentAutomation automation)
    {
        _categoryService = categoryService;
        _tagService = tagService;
        _paymentService = paymentService;
        _automation = automation;
        SaveCommand = new AsyncCommand(Save, () => !IsSaving);
    }
    public async Task Load(Guid? paymentId)
    {
        _paymentId = paymentId; ErrorMessage = ""; Categories.Clear(); Tags.Clear(); OnPropertyChanged(nameof(Title));
        var categories = await _categoryService.GetActive();
        if (categories.IsFailure) { ErrorMessage = categories.Error.Message; return; }
        foreach (var category in categories.Value) Categories.Add(new(category.CategoryId, category.Name, category.Icon, category.Color));
        if (!paymentId.HasValue) return;
        var found = await _paymentService.Get(paymentId.Value);
        if (found.IsFailure || found.Value is null) { ErrorMessage = found.IsFailure ? found.Error.Message : ApplicationErrors.RecurringPaymentNotFound.Message; return; }
        var payment = found.Value; Amount = payment.Amount.ToDecimal().ToString("0.00", ItalianCulture); Frequency = payment.Frequency; NextDueOn = payment.NextDueOn.ToDateTime(TimeOnly.MinValue); EndsOn = payment.EndsOn?.ToDateTime(TimeOnly.MinValue); HasEndDate = EndsOn.HasValue; Note = payment.Note; SelectedCategory = Categories.FirstOrDefault(c => c.CategoryId == payment.CategoryId); await LoadTags(); SelectedTag = Tags.FirstOrDefault(t => t.TagId == payment.TagId);
    }
    public async Task LoadTags()
    {
        Tags.Clear(); SelectedTag = null;
        if (SelectedCategory is null) { OnPropertyChanged(nameof(HasTags)); return; }
        var result = await _tagService.GetActive(SelectedCategory.CategoryId);
        if (result.IsFailure) ErrorMessage = result.Error.Message;
        else foreach (var tag in result.Value) Tags.Add(new(tag.TagId, tag.Name));
        OnPropertyChanged(nameof(HasTags));
    }
    private async Task Save()
    {
        var amount = MoneyInputParser.ParseOptional(Amount, ItalianCulture);
        if (amount.IsFailure || amount.Value is null || amount.Value.Value.Cents <= 0) { ErrorMessage = "Inserisci un importo maggiore di zero."; return; }
        if (SelectedCategory is null) { ErrorMessage = "Seleziona una categoria."; return; }
        IsSaving = true; ErrorMessage = "";
        var request = new SaveRecurringPaymentRequest(SelectedCategory.CategoryId, amount.Value.Value.Cents, Frequency, DateOnly.FromDateTime(NextDueOn), SelectedTag?.TagId, Note, EndsOn.HasValue ? DateOnly.FromDateTime(EndsOn.Value) : null);
        Result result;
        if (_paymentId.HasValue) result = await _paymentService.Update(_paymentId.Value, request);
        else { var created = await _paymentService.Create(request); result = created.IsSuccess ? Result.Success() : Result.Failure(created.Error); }
        IsSaving = false;
        if (result.IsFailure) { ErrorMessage = result.Error.Message; return; }
        await _automation.ProcessDue();
        Saved?.Invoke(this, EventArgs.Empty);
    }
}
