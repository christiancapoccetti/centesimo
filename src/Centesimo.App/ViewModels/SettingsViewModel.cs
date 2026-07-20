using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App.ViewModels;

public sealed class SettingsViewModel(MoneyManagerImportService importService) : ObservableObject
{
    private bool _isBusy;
    private string _message = "";
    private bool _hasError;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                OnPropertyChanged(nameof(CanImport));
        }
    }
    public bool CanImport => !IsBusy;
    public string Message { get => _message; private set => SetProperty(ref _message, value); }
    public bool HasMessage => Message.HasValue();
    public bool HasError { get => _hasError; private set => SetProperty(ref _hasError, value); }

    public async Task Import(Stream backup, CancellationToken cancellationToken = default)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        ClearMessage();
        try
        {
            var result = await importService.Import(backup, cancellationToken);
            if (result.IsFailure)
            {
                ShowError(result.Error.Message);
                return;
            }

            ShowMessage($"Importazione completata: {result.Value.ExpensesAdded} spese, " +
                $"{result.Value.CategoriesAdded} categorie e {result.Value.TagsAdded} tag aggiunti. " +
                $"Ignorati: {result.Value.IgnoredCount}; senza categoria: {result.Value.UncategorizedCount}.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ShowError("Importazione annullata.");
        }
        catch
        {
            ShowError("Non è stato possibile importare il backup.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ShowError(string message)
    {
        HasError = true;
        Message = message;
        OnPropertyChanged(nameof(HasMessage));
    }

    private void ShowMessage(string message)
    {
        HasError = false;
        Message = message;
        OnPropertyChanged(nameof(HasMessage));
    }

    private void ClearMessage()
    {
        HasError = false;
        Message = "";
        OnPropertyChanged(nameof(HasMessage));
    }
}