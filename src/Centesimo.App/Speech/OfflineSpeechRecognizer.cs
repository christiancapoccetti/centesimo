using Centesimo.Application;

namespace Centesimo.App;

public interface IOfflineSpeechRecognizer
{
    event EventHandler<string>? TranscriptionUpdated;
    bool IsListening { get; }
    Task<Result> Start(CancellationToken cancellationToken = default);
    Task<Result<string>> Stop(CancellationToken cancellationToken = default);
}

public sealed class UnavailableOfflineSpeechRecognizer : IOfflineSpeechRecognizer
{
    public event EventHandler<string>? TranscriptionUpdated
    {
        add { }
        remove { }
    }
    public bool IsListening => false;

    public Task<Result> Start(CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure(new Error("Speech.Unsupported", "Il riconoscimento vocale è disponibile solo su Android.")));

    public Task<Result<string>> Stop(CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<string>.Failure(new Error("Speech.NotListening", "La registrazione non è attiva.")));
}
