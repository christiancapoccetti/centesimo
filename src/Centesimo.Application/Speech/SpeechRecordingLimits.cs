namespace Centesimo.Application;

public static class SpeechRecordingLimits
{
    public const int SampleRate = 16_000;
    public const int MaximumDurationSeconds = 20;
    public const int MaximumAudioBytes = SampleRate * sizeof(short) * MaximumDurationSeconds;

    public static bool WouldExceedMaximum(int currentBytes, int additionalBytes) =>
        currentBytes < 0 || additionalBytes < 0 || currentBytes > MaximumAudioBytes - additionalBytes;
}
