using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
    }

    protected override void OnStop()
    {
        CancelVoiceRecognition();
        base.OnStop();
    }

    private void CancelVoiceRecognition() => _ = CancelVoiceRecognitionSafely();

    private static async Task CancelVoiceRecognitionSafely()
    {
        try
        {
            var platformApplication = IPlatformApplication.Current;
            if (platformApplication is null)
                return;

            var recognizer = platformApplication.Services.GetRequiredService<IOfflineSpeechRecognizer>();
            await recognizer.Cancel();
        }
        catch (Exception)
        {
        }
    }

}
