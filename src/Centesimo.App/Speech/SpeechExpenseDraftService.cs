using Centesimo.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App;

public sealed class SpeechExpenseDraftService(
    IOfflineSpeechRecognizer recognizer,
    IServiceScopeFactory scopeFactory,
    ExpenseSpeechCommandParser parser,
    ExpenseSpeechDraftResolver resolver,
    IPendingExpenseSpeechDraft pendingDraft)
{
    public event EventHandler<string>? TranscriptionUpdated
    {
        add => recognizer.TranscriptionUpdated += value;
        remove => recognizer.TranscriptionUpdated -= value;
    }

    public bool IsListening => recognizer.IsListening;
    public string LastTranscription { get; private set; } = "";

    public Task<Result> Start(CancellationToken cancellationToken = default)
    {
        LastTranscription = "";
        return recognizer.Start(cancellationToken);
    }
    public Task Cancel() => recognizer.Cancel();

    public async Task<Result> StopAndPrepare(CancellationToken cancellationToken = default)
    {
        var transcription = await recognizer.Stop(cancellationToken);
        if (transcription.IsFailure)
        {
            LastTranscription = "";
            return Result.Failure(transcription.Error);
        }

        LastTranscription = transcription.Value;

        var command = parser.Parse(transcription.Value);
        if (command.IsFailure)
            return Result.Failure(command.Error);

        using var scope = scopeFactory.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();
        var tagService = scope.ServiceProvider.GetRequiredService<TagService>();
        var categories = await categoryService.GetActive(cancellationToken);
        if (categories.IsFailure)
            return Result.Failure(categories.Error);

        var options = new List<SpeechCategory>();
        foreach (var category in categories.Value)
        {
            var tags = await tagService.GetActive(category.CategoryId, cancellationToken);
            if (tags.IsFailure)
                return Result.Failure(tags.Error);

            options.Add(new SpeechCategory(category.CategoryId, category.Name,
                tags.Value.Select(tag => new SpeechTag(tag.TagId, tag.Name)).ToList()));
        }

        var draft = resolver.Resolve(command.Value, options);
        if (draft.IsFailure)
            return Result.Failure(draft.Error);

        pendingDraft.Set(draft.Value);
        return Result.Success();
    }
}
