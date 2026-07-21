namespace Centesimo.Application.Tests;

public sealed class ZipEntryPathSafety_should_expected_behavior
{
    [Theory]
    [InlineData("vosk-model-small-it-0.22/am/final.mdl")]
    [InlineData("../outside.txt")]
    [InlineData("/outside.txt")]
    [InlineData("folder/../../outside.txt")]
    public void Identify_safe_archive_entry_paths(string path)
    {
        var safe = ZipEntryPathSafety.IsSafe(path);

        Assert.Equal(path == "vosk-model-small-it-0.22/am/final.mdl", safe);
    }
}
