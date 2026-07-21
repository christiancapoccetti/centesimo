using System.IO.Compression;
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
    public const string ModelName = "vosk-model-small-it-0.22";
    private string ModelPath => Path.Combine(FileSystem.AppDataDirectory, ModelName);
    public bool IsAvailable => Directory.Exists(ModelPath);

    public async Task<Result> Prepare(IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        if (IsAvailable)
            return Result.Success();

        var temporary = Path.Combine(FileSystem.CacheDirectory, $"{ModelName}-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(temporary);
            await using var asset = await FileSystem.OpenAppPackageFileAsync($"{ModelName}.zip");
            using var archive = new ZipArchive(asset, ZipArchiveMode.Read);
            var entries = archive.Entries.Where(entry => !string.IsNullOrEmpty(entry.Name)).ToList();
            if (entries.Count == 0 || archive.Entries.Any(entry => !ZipEntryPathSafety.IsSafe(entry.FullName)) ||
                !ZipEntryPathSafety.HasExpectedModelLayout(archive.Entries.Select(entry => entry.FullName), ModelName))
                return Result.Failure(new Error("Speech.ModelInvalid", "Il modello incluso non è valido."));

            for (var index = 0; index < entries.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = entries[index];
                var destination = Path.Combine(temporary, entry.FullName);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                await using var source = entry.Open();
                await using var target = File.Create(destination);
                await source.CopyToAsync(target, cancellationToken);
                progress?.Report((double)(index + 1) / entries.Count);
            }

            var extracted = Path.Combine(temporary, ModelName);
            if (!File.Exists(Path.Combine(extracted, "am", "final.mdl")) ||
                !File.Exists(Path.Combine(extracted, "conf", "model.conf")))
                return Result.Failure(new Error("Speech.ModelInvalid", "Il modello incluso non è valido."));

            Directory.CreateDirectory(FileSystem.AppDataDirectory);
            Directory.Move(extracted, ModelPath);
            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(new Error("Speech.ModelPreparationCanceled", "Preparazione del modello annullata."));
        }
        catch (Exception)
        {
            return Result.Failure(new Error("Speech.ModelPreparationFailed", "Non riesco a preparare il modello italiano."));
        }
        finally
        {
            if (Directory.Exists(ModelPath) && !File.Exists(Path.Combine(ModelPath, "am", "final.mdl")))
                Directory.Delete(ModelPath, true);
            if (Directory.Exists(temporary))
                Directory.Delete(temporary, true);
        }
    }
}
