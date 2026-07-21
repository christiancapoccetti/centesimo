using Centesimo.App.ViewModels;

namespace Centesimo.App;

public sealed class SpeechPreparationStatus : ObservableObject
{
    private bool _isPreparing;
    private bool _isReady;
    private double _progress;
    private string _message = "Comandi vocali non attivati.";

    public bool IsPreparing { get => _isPreparing; set => SetProperty(ref _isPreparing, value); }
    public bool IsReady { get => _isReady; set => SetProperty(ref _isReady, value); }
    public double Progress { get => _progress; set => SetProperty(ref _progress, value); }
    public string Message { get => _message; set => SetProperty(ref _message, value); }
}
