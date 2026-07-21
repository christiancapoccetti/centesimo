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

    [Fact]
    public void Accept_model_archive_root_and_required_markers()
    {
        var entries = new[]
        {
            "vosk-model-small-it-0.22/",
            "vosk-model-small-it-0.22/am/final.mdl",
            "vosk-model-small-it-0.22/conf/model.conf"
        };

        Assert.All(entries, entry => Assert.True(ZipEntryPathSafety.IsSafe(entry)));
        Assert.True(ZipEntryPathSafety.HasExpectedModelLayout(entries, "vosk-model-small-it-0.22"));
    }
}
