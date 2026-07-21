using Centesimo.Application;
using Centesimo.App;
using Centesimo.Domain;

namespace Centesimo.App.ViewModels;

public sealed class SettingsViewModel(MoneyManagerImportService importService, SpeechPreparationStatus speechPreparation) : ObservableObject
{
    private bool _isBusy;
    private string _message = "";
    private bool _hasError;
    private MoneyManagerImportPreview? _preview;

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
    public MoneyManagerImportPreview? PreviewResult
    {
        get => _preview;
        private set
        {
            if (SetProperty(ref _preview, value))
                OnPropertyChanged(nameof(HasPreview));
        }
    }
    public bool HasPreview => PreviewResult is not null;
    public SpeechPreparationStatus SpeechPreparation => speechPreparation;

    public async Task<Result<MoneyManagerImportPreview>> Preview(Stream backup,
        CancellationToken cancellationToken = default)
    {
        if (IsBusy)
            return Result<MoneyManagerImportPreview>.Failure(MoneyManagerImportErrors.InvalidBackup);

        IsBusy = true;
        ClearPreview();
        ClearMessage();
        try
        {
            var result = await importService.Preview(backup, cancellationToken);
            if (result.IsFailure)
            {
                ClearPreview();
                ShowError(result.Error.Message);
            }
            else
                PreviewResult = result.Value;

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ShowError("Importazione annullata.");
            throw;
        }
        catch
        {
            ShowError("Non è stato possibile leggere il backup.");
            return Result<MoneyManagerImportPreview>.Failure(MoneyManagerImportErrors.InvalidBackup);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task Import(MoneyManagerImportPreview preview, CancellationToken cancellationToken = default)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        ClearMessage();
        try
        {
            var result = await importService.Import(preview, cancellationToken);
            if (result.IsFailure)
            {
                ShowError(result.Error.Message);
                return;
            }

            ShowMessage($"Importazione completata: {result.Value.ExpensesAdded} spese, " +
                $"{result.Value.RecurringPaymentsAdded} pagamenti regolari, " +
                $"{result.Value.CategoriesAdded} categorie e {result.Value.TagsAdded} tag aggiunti. " +
                $"Ignorati: {result.Value.IgnoredCount}; senza categoria: {result.Value.UncategorizedCount}.");
            ClearPreview();
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
    public Task ImportPreview(CancellationToken cancellationToken = default) =>
        PreviewResult is null ? Task.CompletedTask : Import(PreviewResult, cancellationToken);
    public void ClearPreview() => PreviewResult = null;
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
