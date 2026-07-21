using Android.App;
using Android.Content.PM;
using Android.OS;

namespace Centesimo.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        RequestNotificationPermission();
    }

    private void RequestNotificationPermission()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            return;

        RequestAndroid13NotificationPermission();
    }

    [System.Runtime.Versioning.SupportedOSPlatform("android33.0")]
    private void RequestAndroid13NotificationPermission()
    {
        if (CheckSelfPermission(Android.Manifest.Permission.PostNotifications) == Permission.Granted)
            return;

        RequestPermissions([Android.Manifest.Permission.PostNotifications], 1);
    }
}
