using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace SQLite.Framework.Avalonia;

[Activity(
    Label = "SQLite.Framework Avalonia",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
}
