#if ANDROID
using Android.Media;
using Centesimo.Application;
using Microsoft.Maui.Storage;
using Vosk;

namespace Centesimo.App;

public sealed class VoskOfflineSpeechRecognizer : IOfflineSpeechRecognizer, IDisposable
{
    private const int SampleRate = 16_000;
    private readonly object _sync = new();
    private CancellationTokenSource? _recording;
    private Task<string>? _recognition;
    private AudioRecord? _audioRecord;

    public event EventHandler<string>? TranscriptionUpdated;
    public bool IsListening { get; private set; }

    public Task<Result> Start(CancellationToken cancellationToken = default)
    {
        var modelPath = Path.Combine(FileSystem.AppDataDirectory, "vosk-model-small-it-0.22");
        if (!Directory.Exists(modelPath))
            return Task.FromResult(Result.Failure(new Error("Speech.ModelMissing", "Il modello italiano offline non è installato sul dispositivo.")));

        if (IsListening)
            return Task.FromResult(Result.Failure(new Error("Speech.AlreadyListening", "La registrazione è già attiva.")));

        var permission = Permissions.CheckStatusAsync<Permissions.Microphone>().GetAwaiter().GetResult();
        if (permission != PermissionStatus.Granted)
            return Task.FromResult(Result.Failure(new Error("Speech.MicrophoneDenied", "Consenti l'accesso al microfono per usare i comandi vocali.")));

        _recording = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        IsListening = true;
        _recognition = Task.Run(() => Recognize(modelPath, _recording.Token), CancellationToken.None);
        return Task.FromResult(Result.Success());
    }

    public async Task<Result<string>> Stop(CancellationToken cancellationToken = default)
    {
        if (!IsListening || _recording is null || _recognition is null)
            return Result<string>.Failure(new Error("Speech.NotListening", "La registrazione non è attiva."));

        _recording.Cancel();
        try
        {
            var transcription = await _recognition.WaitAsync(cancellationToken);
            return !string.IsNullOrWhiteSpace(transcription)
                ? Result<string>.Success(transcription)
                : Result<string>.Failure(new Error("Speech.Empty", "Non ho capito il comando. Riprova."));
        }
        catch (Exception)
        {
            return Result<string>.Failure(new Error("Speech.Failed", "Non riesco a elaborare l'audio sul dispositivo."));
        }
        finally
        {
            IsListening = false;
            _recording.Dispose();
            _recording = null;
            _recognition = null;
        }
    }

    private string Recognize(string modelPath, CancellationToken cancellationToken)
    {
        Vosk.Vosk.SetLogLevel(-1);
        using var model = new Model(modelPath);
        using var recognizer = new VoskRecognizer(model, SampleRate);
        var minimumBuffer = AudioRecord.GetMinBufferSize(SampleRate, ChannelIn.Mono, Encoding.Pcm16bit);
        _audioRecord = new AudioRecord(AudioSource.Mic, SampleRate, ChannelIn.Mono, Encoding.Pcm16bit, minimumBuffer * 2);
        var buffer = new short[minimumBuffer];
        _audioRecord.StartRecording();
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var read = _audioRecord.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                    continue;

                recognizer.AcceptWaveform(buffer, read);
                var partial = ExtractText(recognizer.PartialResult());
                if (!string.IsNullOrWhiteSpace(partial))
                    TranscriptionUpdated?.Invoke(this, partial);
            }

            return ExtractText(recognizer.FinalResult());
        }
        finally
        {
            _audioRecord.Stop();
            _audioRecord.Release();
            _audioRecord.Dispose();
            _audioRecord = null;
            Array.Clear(buffer);
        }
    }

    private static string ExtractText(string json)
    {
        using var document = System.Text.Json.JsonDocument.Parse(json);
        return document.RootElement.TryGetProperty("text", out var text)
            ? text.GetString() ?? ""
            : document.RootElement.TryGetProperty("partial", out var partial) ? partial.GetString() ?? "" : "";
    }

    public void Dispose() => _recording?.Cancel();
}
#endif
