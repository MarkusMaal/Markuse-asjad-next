﻿using Avalonia;
using Avalonia.Browser;
using Avalonia.ReactiveUI;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using UniplatformTest;

[assembly: SupportedOSPlatform("browser")]

internal sealed partial class Program
{
    private static Task Main(string[] args) => BuildAvaloniaApp()
            .WithInterFont()
            .UseReactiveUI()
            .StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}