using System.Collections.ObjectModel;
using System.Globalization;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App.ViewModels;

public sealed class RecurringPaymentsViewModel(RecurringPaymentService service,
    RecurringPaymentAutomation automation) : ObservableObject
{
    private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");
    private bool _isLoading;
    private string _errorMessage = "";

    public ObservableCollection<RecurringPaymentItemViewModel> Payments { get; } = [];
    public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }
    public string ErrorMessage { get => _errorMessage; private set { if (SetProperty(ref _errorMessage, value)) OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => ErrorMessage.HasValue();
    public bool IsEmpty => !IsLoading && !HasError && Payments.Count == 0;

    public async Task Load()
    {
        IsLoading = true;
        ErrorMessage = "";
        Payments.Clear();
        var result = await service.GetAll();
        if (result.IsFailure)
            ErrorMessage = result.Error.Message;
        else
            foreach (var payment in result.Value)
                Payments.Add(RecurringPaymentItemViewModel.From(payment));
        IsLoading = false;
        OnPropertyChanged(nameof(IsEmpty));
    }

    public async Task<Result> SuspendOrResume(RecurringPaymentItemViewModel payment)
    {
        var result = payment.IsSuspended
            ? await service.Resume(payment.RecurringPaymentId)
            : await service.Suspend(payment.RecurringPaymentId);
        if (result.IsSuccess)
        {
            await automation.ProcessDue();
            await Load();
        }
        return result;
    }

    public async Task<Result> End(RecurringPaymentItemViewModel payment)
    {
        var result = await service.End(payment.RecurringPaymentId, payment.NextDueOn);
        if (result.IsSuccess)
        {
            await automation.ProcessDue();
            await Load();
        }
        return result;
    }

    public sealed record RecurringPaymentItemViewModel(Guid RecurringPaymentId, string Amount,
        string Schedule, DateOnly NextDueOn, string NextDue, string Status, bool IsSuspended, bool IsEnded)
    {
        public string SuspendResumeText => IsSuspended ? "Riprendi" : "Sospendi";
        public bool CanChangeState => !IsEnded;
        public static RecurringPaymentItemViewModel From(RecurringPayment payment) => new(
            payment.RecurringPaymentId,
            payment.Amount.ToDecimal().ToString("C", ItalianCulture),
            payment.Frequency switch { RecurrenceFrequency.Weekly => "Ogni settimana", RecurrenceFrequency.Monthly => "Ogni mese", _ => "Ogni anno" },
            payment.NextDueOn,
            payment.EndsOn.HasValue && payment.NextDueOn > payment.EndsOn.Value
                ? "Pagamento terminato"
                : $"Prossima scadenza: {payment.NextDueOn:dd/MM/yyyy}",
            payment.EndsOn.HasValue && payment.NextDueOn > payment.EndsOn.Value
                ? "Terminato"
                : payment.IsSuspended ? "Sospeso" : "Attivo", payment.IsSuspended,
            payment.EndsOn.HasValue && payment.NextDueOn > payment.EndsOn.Value);
    }
}
