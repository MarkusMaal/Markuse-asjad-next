using Android.App;
using Android.Content.PM;
using Android.Renderscripts;
using Avalonia;
using Avalonia.Android;
using Avalonia.ReactiveUI;
using System;

namespace UniplatformTest.Android
{
    [Activity(
        Label = "UniplatformTest.Android",
        Theme = "@style/MyTheme.NoActionBar",
        Icon = "@drawable/icon",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class MainActivity : AvaloniaMainActivity<App>
    {
        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            return base.CustomizeAppBuilder(builder)
                .WithInterFont()
                .UseReactiveUI();
        }
    }
}
