using Centesimo.Application;

namespace Centesimo.App;

public sealed class SpeechExpenseDraftService(
    IOfflineSpeechRecognizer recognizer,
    CategoryService categoryService,
    TagService tagService,
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

    public Task<Result> Start(CancellationToken cancellationToken = default) => recognizer.Start(cancellationToken);

    public async Task<Result> StopAndPrepare(CancellationToken cancellationToken = default)
    {
        var transcription = await recognizer.Stop(cancellationToken);
        if (transcription.IsFailure)
            return Result.Failure(transcription.Error);

        var command = parser.Parse(transcription.Value);
        if (command.IsFailure)
            return Result.Failure(command.Error);

        var categories = await categoryService.GetActive(cancellationToken);
        if (categories.IsFailure)
            return Result.Failure(categories.Error);

        var options = new List<SpeechCategory>();
        foreach (var category in categories.Value)
        {
            var tags = await tagService.GetActive(category.CategoryId, cancellationToken);
            if (tags.IsFailure)
                return Result.Failure(tags.Error);

            options.Add(new SpeechCategory(category.Name, tags.Value.Select(tag => new SpeechTag(tag.Name)).ToList()));
        }

        var draft = resolver.Resolve(command.Value, options);
        if (draft.IsFailure)
            return Result.Failure(draft.Error);

        pendingDraft.Set(draft.Value);
        return Result.Success();
    }
}
