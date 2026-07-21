namespace Centesimo.Application;

public static class WhisperProcessingSettings
{
    public static int GetThreadCount(int processorCount) => Math.Clamp(processorCount / 2, 2, 6);
}
