using System.Security.Cryptography;
using Centesimo.Application;
using Microsoft.Maui.Storage;

namespace Centesimo.App;

public interface IItalianSpeechModelProvisioner
{
    bool IsAvailable { get; }
    Task<Result> Prepare(IProgress<double>? progress = null, CancellationToken cancellationToken = default);
}

public sealed class ItalianSpeechModelProvisioner : IItalianSpeechModelProvisioner
{
    public const string ModelName = "ggml-small-q5_1.bin";
    private const string ModelUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small-q5_1.bin";
    private const string ModelSha1 = "6fe57ddcfdd1c6b07cdcc73aaf620810ce5fc771";
    private static readonly HttpClient Client = new();
    private bool? _isAvailable;
    private string ModelPath => Path.Combine(FileSystem.AppDataDirectory, ModelName);
    public bool IsAvailable => _isAvailable ??= IsValid(ModelPath);

    public async Task<Result> Prepare(IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        if (IsAvailable)
            return Result.Success();

        var temporary = Path.Combine(FileSystem.CacheDirectory, $"{ModelName}-{Guid.NewGuid():N}");
        try
        {
            using var response = await Client.GetAsync(ModelUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result.Failure(new Error("Speech.ModelDownloadFailed", "Non riesco a scaricare il modello italiano."));

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var target = File.Create(temporary);
            var buffer = new byte[81_920];
            var copied = 0L;
            while (true)
            {
                var read = await source.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                    break;

                await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                copied += read;
                if (response.Content.Headers.ContentLength is { } length && length > 0)
                    progress?.Report((double)copied / length);
            }

            await target.FlushAsync(cancellationToken);
            if (!IsValid(temporary))
                return Result.Failure(new Error("Speech.ModelIntegrityFailed", "Il modello scaricato non è valido. Riprova."));

            Directory.CreateDirectory(FileSystem.AppDataDirectory);
            File.Move(temporary, ModelPath, true);
            _isAvailable = true;
            progress?.Report(1);
            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(new Error("Speech.ModelPreparationCanceled", "Download del modello annullato."));
        }
        catch (HttpRequestException)
        {
            return Result.Failure(new Error("Speech.ModelDownloadFailed", "Non riesco a scaricare il modello italiano. Controlla la connessione."));
        }
        catch (Exception)
        {
            return Result.Failure(new Error("Speech.ModelPreparationFailed", "Non riesco a preparare il modello italiano."));
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    internal static bool IsValid(string path)
    {
        if (!File.Exists(path) || new FileInfo(path).Length == 0)
            return false;

        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA1.HashData(stream)).Equals(ModelSha1, StringComparison.OrdinalIgnoreCase);
    }
}
