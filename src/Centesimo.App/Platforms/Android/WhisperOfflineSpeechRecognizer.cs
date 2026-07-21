#if ANDROID
using Android.Media;
using Centesimo.Application;
using Microsoft.Maui.Storage;
using System.Runtime.InteropServices;
using Whisper.net;

namespace Centesimo.App;

public sealed class WhisperOfflineSpeechRecognizer : IOfflineSpeechRecognizer, IDisposable
{
    private const int SampleRate = SpeechRecordingLimits.SampleRate;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SemaphoreSlim _factoryGate = new(1, 1);
    private WhisperFactory? _factory;
    private int _disposeStarted;
    private bool _disposed;
    private RecordingSession? _session;

    public event EventHandler<string>? TranscriptionUpdated
    {
        add { }
        remove { }
    }
    public bool IsListening => Volatile.Read(ref _session) is not null;

    public async Task<Result> WarmUp(CancellationToken cancellationToken = default)
    {
        var modelPath = Path.Combine(FileSystem.AppDataDirectory, ItalianSpeechModelProvisioner.ModelName);
        if (!File.Exists(modelPath))
            return Result.Failure(new Error("Speech.ModelMissing", "Il modello italiano non è disponibile."));

        await _factoryGate.WaitAsync(cancellationToken);
        try
        {
            ThrowIfDisposed();
            _factory ??= await Task.Run(() => WhisperFactory.FromPath(modelPath), cancellationToken);
            return Result.Success();
        }
        catch (Exception)
        {
            return Result.Failure(new Error("Speech.WarmUpFailed", "Non riesco a preparare il riconoscimento vocale."));
        }
        finally
        {
            _factoryGate.Release();
        }
    }

    public async Task<Result> Start(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _disposeStarted) != 0)
            return Result.Failure(new Error("Speech.Unavailable", "Il riconoscimento vocale non è disponibile."));

        var modelPath = Path.Combine(FileSystem.AppDataDirectory, ItalianSpeechModelProvisioner.ModelName);
        if (!File.Exists(modelPath))
            return Result.Failure(new Error("Speech.ModelMissing", "Il modello italiano offline non è installato sul dispositivo."));

        if (await Permissions.CheckStatusAsync<Permissions.Microphone>() != PermissionStatus.Granted)
            return Result.Failure(new Error("Speech.MicrophoneDenied", "Consenti l'accesso al microfono per usare i comandi vocali."));

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (Volatile.Read(ref _disposeStarted) != 0)
                return Result.Failure(new Error("Speech.Unavailable", "Il riconoscimento vocale non è disponibile."));

            if (_session is not null)
                return Result.Failure(new Error("Speech.AlreadyListening", "La registrazione è già attiva."));

            var session = new RecordingSession();
            session.Start();
            _session = session;
            return Result.Success();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Result<string>> Stop(CancellationToken cancellationToken = default)
    {
        var session = await TakeSession(cancellationToken, false);
        if (session is null)
            return Result<string>.Failure(new Error("Speech.NotListening", "La registrazione non è attiva."));

        try
        {
            var audio = await session.Stop();
            if (session.IsLimitReached)
                return Result<string>.Failure(new Error("Speech.TooLong", "Il comando vocale è troppo lungo. Riprova con una frase più breve."));

            var transcription = await Transcribe(audio, cancellationToken);
            return string.IsNullOrWhiteSpace(transcription)
                ? Result<string>.Failure(new Error("Speech.Empty", "Non ho capito il comando. Riprova."))
                : Result<string>.Success(transcription);
        }
        catch (OperationCanceledException)
        {
            return Result<string>.Failure(new Error("Speech.Failed", "Non riesco a elaborare l'audio sul dispositivo."));
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
        var session = await TakeSession(CancellationToken.None, true);
        if (session is null)
            return;

        await session.CancelAndWait();
        session.Dispose();
    }

    private async Task<string> Transcribe(float[] audio, CancellationToken cancellationToken)
    {
        if (audio.Length == 0)
            return "";

        await _factoryGate.WaitAsync(cancellationToken);
        try
        {
            ThrowIfDisposed();
            _factory ??= WhisperFactory.FromPath(Path.Combine(FileSystem.AppDataDirectory, ItalianSpeechModelProvisioner.ModelName));
            await using var processor = _factory
                .CreateBuilder()
                .WithLanguage("it")
                .WithThreads(WhisperProcessingSettings.GetThreadCount(Environment.ProcessorCount))
                .WithSingleSegment()
                .Build();

            var segments = new List<string>();
            await foreach (var segment in processor.ProcessAsync(audio, cancellationToken))
                segments.Add(segment.Text);

            return string.Join(" ", segments).Trim();
        }
        finally
        {
            _factoryGate.Release();
        }
    }

    private async Task<RecordingSession?> TakeSession(CancellationToken cancellationToken, bool cancelImmediately)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var session = _session;
            _session = null;
            if (cancelImmediately)
                session?.Cancel();

            return session;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
            return;

        Cancel().GetAwaiter().GetResult();
        _factoryGate.Wait();
        try
        {
            if (_disposed)
                return;

            _disposed = true;
            _factory?.Dispose();
            _factory = null;
        }
        finally
        {
            _factoryGate.Release();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WhisperOfflineSpeechRecognizer));
    }

    private sealed class RecordingSession : IDisposable
    {
        private readonly CancellationTokenSource _cancellation = new();
        private readonly MemoryStream _audio = new();
        private AudioRecord? _audioRecord;
        private Task? _recording;
        public bool IsLimitReached { get; private set; }

        public void Start() => _recording = Task.Run(Record, CancellationToken.None);

        public async Task<float[]> Stop()
        {
            await CancelAndWait();

            var bytes = _audio.ToArray();
            var samples = new short[bytes.Length / sizeof(short)];
            Buffer.BlockCopy(bytes, 0, samples, 0, bytes.Length);
            return samples.Select(sample => sample / (float)short.MaxValue).ToArray();
        }

        public void Cancel()
        {
            _cancellation.Cancel();
            StopRecording(_audioRecord);
        }

        public async Task CancelAndWait()
        {
            Cancel();
            if (_recording is not null)
                await _recording;
        }

        private void Record()
        {
            var bufferSize = AudioRecord.GetMinBufferSize(SampleRate, ChannelIn.Mono, Encoding.Pcm16bit);
            var audioRecord = new AudioRecord(AudioSource.Mic, SampleRate, ChannelIn.Mono, Encoding.Pcm16bit, bufferSize * 2);
            _audioRecord = audioRecord;
            var buffer = new short[bufferSize];
            try
            {
                audioRecord.StartRecording();
                while (!_cancellation.IsCancellationRequested)
                {
                    var read = audioRecord.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        continue;

                    var bytes = MemoryMarshal.AsBytes(buffer.AsSpan(0, read));
                    if (SpeechRecordingLimits.WouldExceedMaximum((int)_audio.Length, bytes.Length))
                    {
                        IsLimitReached = true;
                        break;
                    }

                    _audio.Write(bytes);
                }
            }
            finally
            {
                StopRecording(audioRecord);
                audioRecord.Release();
                audioRecord.Dispose();
                _audioRecord = null;
                Array.Clear(buffer);
            }
        }

        public void Dispose()
        {
            Cancel();
            _audio.Dispose();
            _cancellation.Dispose();
        }

        private static void StopRecording(AudioRecord? audioRecord)
        {
            if (audioRecord is null)
                return;

            try
            {
                if (audioRecord.RecordingState == RecordState.Recording)
                    audioRecord.Stop();
            }
            catch (Java.Lang.IllegalStateException)
            {
            }
        }
    }
}
#endif
