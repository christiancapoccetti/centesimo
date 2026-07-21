using Centesimo.Application;

namespace Centesimo.Application.Tests;

public sealed class WhisperProcessingSettings_should_expected_behavior
{
    [Theory]
    [InlineData(1, 2)]
    [InlineData(4, 2)]
    [InlineData(8, 4)]
    [InlineData(32, 6)]
    public void return_half_the_available_processors_with_safe_bounds(int processorCount, int expected)
    {
        var threads = WhisperProcessingSettings.GetThreadCount(processorCount);

        Assert.Equal(expected, threads);
    }
}
