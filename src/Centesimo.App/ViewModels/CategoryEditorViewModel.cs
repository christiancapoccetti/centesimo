using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App.ViewModels;

public sealed class CategoryEditorViewModel : ObservableObject
{
    private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");
    private readonly CategoryService _categoryService;
    private readonly TagService _tagService;
    private Guid? _categoryId;
    private string _name = "";
    private string _selectedIcon = "cart";
    private string _selectedColor = "#176B5B";
    private string _budget = "";
    private string _errorMessage = "";
    private string _newTagName = "";
    private bool _isSaving;
    private bool _isTagBusy;

    public event EventHandler? Saved;

    public IReadOnlyList<CategoryIconOption> Icons { get; } =
    [
        new("cart"), new("home"), new("car"), new("heart"), new("more"), new("sport"), new("shopping-bag"), new("food"),
        new("gift"), new("beach"), new("tech"), new("school"), new("family"), new("gym"), new("gear")
    ];
    public IReadOnlyList<CategoryColorOption> Colors { get; } =
    [
        new("#176B5B"), new("#8B4A5D"), new("#725C00"), new("#4F5F7A"), new("#6A4F88")
    ];
    public ObservableCollection<CategoryTagItemViewModel> Tags { get; } = [];
    public ICommand SaveCommand { get; }
    public ICommand ClearBudgetCommand { get; }
    public ICommand AddTagCommand { get; }
    public string Title => _categoryId.HasValue ? "Modifica categoria" : "Nuova categoria";
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string SelectedIcon
    {
        get => _selectedIcon;
        set
        {
            SetProperty(ref _selectedIcon, value);
            foreach (var icon in Icons)
                icon.IsSelected = icon.Icon == value;
        }
    }
    public string SelectedColor
    {
        get => _selectedColor;
        set
        {
            SetProperty(ref _selectedColor, value);
            foreach (var color in Colors)
                color.IsSelected = color.Color == value;
        }
    }
    public string Budget { get => _budget; set => SetProperty(ref _budget, value); }
    public string NewTagName { get => _newTagName; set => SetProperty(ref _newTagName, value); }
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
    public bool IsTagBusy { get => _isTagBusy; private set => SetProperty(ref _isTagBusy, value); }
    public bool CanManageTags => _categoryId.HasValue;
    public bool CannotManageTags => !CanManageTags;
    public bool HasTags => Tags.Count > 0;
    public bool IsTagEmpty => CanManageTags && !IsTagBusy && !HasTags;

    public CategoryEditorViewModel(CategoryService categoryService, TagService tagService)
    {
        _categoryService = categoryService;
        _tagService = tagService;
        SaveCommand = new AsyncCommand(Save, () => !IsSaving);
        ClearBudgetCommand = new RelayCommand(() => Budget = "");
        AddTagCommand = new AsyncCommand(AddTag, () => !IsTagBusy);
    }

    public void OpenNew()
    {
        _categoryId = null;
        Name = "";
        SelectedIcon = Icons[0].Icon;
        SelectedColor = Colors[0].Color;
        Budget = "";
        ErrorMessage = "";
        ResetTags();
        OnPropertyChanged(nameof(Title));
        NotifyTagState();
    }

    public void OpenEdit(CategoryItemViewModel category)
    {
        _categoryId = category.CategoryId;
        Name = category.Name;
        SelectedIcon = category.Icon;
        SelectedColor = category.Color;
        Budget = category.BudgetInput;
        ErrorMessage = "";
        ResetTags();
        OnPropertyChanged(nameof(Title));
        NotifyTagState();
    }

    public async Task LoadTags()
    {
        Tags.Clear();
        if (!_categoryId.HasValue)
        {
            NotifyTagState();
            return;
        }

        IsTagBusy = true;
        var result = await _tagService.GetActive(_categoryId.Value);
        if (result.IsFailure)
            ErrorMessage = result.Error.Message;
        else
            foreach (var tag in result.Value)
                Tags.Add(new CategoryTagItemViewModel(tag.TagId, tag.Name));

        IsTagBusy = false;
        NotifyTagState();
    }

    public async Task<Result> ArchiveTag(Guid tagId)
    {
        IsTagBusy = true;
        var result = await _tagService.Archive(tagId);
        IsTagBusy = false;
        if (result.IsSuccess)
            await LoadTags();
        else
            ErrorMessage = result.Error.Message;

        return result;
    }

    private async Task AddTag()
    {
        if (!_categoryId.HasValue)
        {
            ErrorMessage = "Salva prima la categoria per aggiungere tag.";
            return;
        }

        if (NewTagName.IsEmpty())
        {
            ErrorMessage = "Inserisci il nome del tag.";
            return;
        }

        IsTagBusy = true;
        var result = await _tagService.Create(_categoryId.Value, NewTagName);
        IsTagBusy = false;
        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        NewTagName = "";
        await LoadTags();
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

    private void ResetTags()
    {
        Tags.Clear();
        NewTagName = "";
        IsTagBusy = false;
    }

    private void NotifyTagState()
    {
        OnPropertyChanged(nameof(CanManageTags));
        OnPropertyChanged(nameof(CannotManageTags));
        OnPropertyChanged(nameof(HasTags));
        OnPropertyChanged(nameof(IsTagEmpty));
    }
}

public sealed record CategoryTagItemViewModel(Guid TagId, string Name);

public sealed class CategoryIconOption(string icon) : ObservableObject
{
    private bool _isSelected;

    public string Icon { get; } = icon;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
}

public sealed class CategoryColorOption(string color) : ObservableObject
{
    private bool _isSelected;

    public string Color { get; } = color;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
}
