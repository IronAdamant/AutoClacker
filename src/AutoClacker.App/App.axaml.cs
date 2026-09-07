using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AutoClacker.App.ViewModels;
using AutoClacker.Core.Abstractions;

namespace AutoClacker.App;

public partial class App : Application
{
    private IPlatformServices? _platform;
    private ILog? _log;

    public void Configure(IPlatformServices platform, ILog? log = null)
    {
        _platform = platform;
        _log = log;
    }

    public override void Initialize()
    {
        Name = "AutoClacker";
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (_platform is null)
                throw new InvalidOperationException("App.Configure(platform) must be called before start.");

            Dispatcher.UIThread.UnhandledException += (_, e) =>
                _log?.Info($"UI UnhandledException: {e.Exception}");

            var vm = new MainViewModel(_platform, log: _log);
            desktop.MainWindow = new MainWindow(vm);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
