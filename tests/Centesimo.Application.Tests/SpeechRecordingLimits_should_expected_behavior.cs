using Centesimo.Application;

namespace Centesimo.Application.Tests;

public sealed class SpeechRecordingLimits_should_expected_behavior
{
    [Fact]
    public void allow_audio_that_fits_the_recording_budget()
    {
        var exceeds = SpeechRecordingLimits.WouldExceedMaximum(
            SpeechRecordingLimits.MaximumAudioBytes - sizeof(short),
            sizeof(short));

        Assert.False(exceeds);
    }

    [Fact]
    public void reject_audio_that_exceeds_the_recording_budget()
    {
        var exceeds = SpeechRecordingLimits.WouldExceedMaximum(
            SpeechRecordingLimits.MaximumAudioBytes,
            sizeof(short));

        Assert.True(exceeds);
    }
}
