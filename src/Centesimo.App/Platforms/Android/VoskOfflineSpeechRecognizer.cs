#if ANDROID
using Android.Media;
using Centesimo.Application;
using Microsoft.Maui.Storage;
using Vosk;

namespace Centesimo.App;

public sealed class VoskOfflineSpeechRecognizer : IOfflineSpeechRecognizer, IDisposable
{
    private const int SampleRate = 16_000;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private RecordingSession? _session;
    private AudioRecord? _audioRecord;

    public event EventHandler<string>? TranscriptionUpdated;
    public bool IsListening => Volatile.Read(ref _session) is not null;

    public async Task<Result> Start(CancellationToken cancellationToken = default)
    {
        var modelPath = Path.Combine(FileSystem.AppDataDirectory, "vosk-model-small-it-0.22");
        if (!Directory.Exists(modelPath))
            return Result.Failure(new Error("Speech.ModelMissing", "Il modello italiano offline non è installato sul dispositivo."));

        if (await Permissions.CheckStatusAsync<Permissions.Microphone>() != PermissionStatus.Granted)
            return Result.Failure(new Error("Speech.MicrophoneDenied", "Consenti l'accesso al microfono per usare i comandi vocali."));

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_session is not null)
                return Result.Failure(new Error("Speech.AlreadyListening", "La registrazione è già attiva."));

            var recording = new CancellationTokenSource();
            var recognition = Task.Run(() => Recognize(modelPath, recording.Token), CancellationToken.None);
            _session = new RecordingSession(recording, recognition);
            return Result.Success();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Result<string>> Stop(CancellationToken cancellationToken = default)
    {
        var session = await TakeSession(cancellationToken);
        if (session is null)
            return Result<string>.Failure(new Error("Speech.NotListening", "La registrazione non è attiva."));

        try
        {
            var transcription = await session.Recognition;
            return string.IsNullOrWhiteSpace(transcription)
                ? Result<string>.Failure(new Error("Speech.Empty", "Non ho capito il comando. Riprova."))
                : Result<string>.Success(transcription);
        }
        catch (Exception)
        {
            return Result<string>.Failure(new Error("Speech.Failed", "Non riesco a elaborare l'audio sul dispositivo."));
        }
        finally
        {
            session.Dispose();
        }
    }

    public async Task Cancel()
    {
        var session = await TakeSession(CancellationToken.None);
        if (session is null)
            return;

        try
        {
            await session.Recognition;
        }
        catch (Exception)
        {
        }
        finally
        {
            session.Dispose();
        }
    }

    private async Task<RecordingSession?> TakeSession(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var session = _session;
            _session = null;
            if (session is not null)
            {
                session.Cancel();
                _audioRecord?.Stop();
            }

            return session;
        }
        finally
        {
            _gate.Release();
        }
    }

    private string Recognize(string modelPath, CancellationToken cancellationToken)
    {
        Vosk.Vosk.SetLogLevel(-1);
        using var model = new Model(modelPath);
        using var recognizer = new VoskRecognizer(model, SampleRate);
        var bufferSize = AudioRecord.GetMinBufferSize(SampleRate, ChannelIn.Mono, Encoding.Pcm16bit);
        var audioRecord = new AudioRecord(AudioSource.Mic, SampleRate, ChannelIn.Mono, Encoding.Pcm16bit, bufferSize * 2);
        var buffer = new short[bufferSize];
        _audioRecord = audioRecord;
        audioRecord.StartRecording();
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var read = audioRecord.Read(buffer, 0, buffer.Length);
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
            if (audioRecord.RecordingState == RecordState.Recording)
                audioRecord.Stop();
            audioRecord.Release();
            audioRecord.Dispose();
            if (ReferenceEquals(_audioRecord, audioRecord))
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

    public void Dispose()
    {
        _ = Cancel();
        _gate.Dispose();
    }

    private sealed class RecordingSession(CancellationTokenSource cancellation, Task<string> recognition) : IDisposable
    {
        public Task<string> Recognition { get; } = recognition;
        public void Cancel() => cancellation.Cancel();
        public void Dispose() => cancellation.Dispose();
    }
}
#endif
