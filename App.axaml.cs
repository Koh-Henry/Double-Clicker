using System.ComponentModel;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MinecraftDoubleClicker.Models;
using MinecraftDoubleClicker.Services;
using MinecraftDoubleClicker.ViewModels;

namespace MinecraftDoubleClicker;

public partial class App : Application
{
    private ClickEngine? _clickEngine;
    private IHotkeyService? _hotkeyService;
    private MainViewModel? _mainViewModel;
    private IMouseHookService? _mouseHookService;
    private string? _inputMonitoringError;
    private bool _isCleanedUp;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            InitializeDesktopApplication(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void InitializeDesktopApplication(IClassicDesktopStyleApplicationLifetime desktop)
    {
        SettingsService settingsService = new();
        AppSettings appSettings = settingsService.Load();
        _mainViewModel = new MainViewModel(appSettings, settingsService, () => desktop.Shutdown());

        try
        {
            _clickEngine = new ClickEngine(
                appSettings,
                PlatformServiceFactory.CreateInputInjector(),
                PlatformServiceFactory.CreateForegroundWindowService());
            _clickEngine.InjectionFailed += OnClickInjectionFailed;

            _hotkeyService = PlatformServiceFactory.CreateHotkeyService(
                () => Dispatcher.UIThread.Post(_mainViewModel.ToggleEnabledFromHotkey));
            _mouseHookService = PlatformServiceFactory.CreateMouseHookService(_clickEngine);
            _mouseHookService.Start();
        }
        catch (Exception ex)
        {
            _inputMonitoringError = $"Input monitoring unavailable: {ex.Message}";
            _mainViewModel.StatusText = _inputMonitoringError;
        }

        _mainViewModel.SettingsSaved += OnMainViewModelSettingsSaved;
        _mainViewModel.PropertyChanged += OnMainViewModelPropertyChanged;

        MainWindow window = new(_mainViewModel);
        window.Opened += OnMainWindowOpened;
        desktop.MainWindow = window;
        desktop.Exit += OnDesktopExit;
    }

    private void OnMainWindowOpened(object? sender, EventArgs e)
    {
        if (sender is not MainWindow window || _hotkeyService is null || _mainViewModel is null)
        {
            return;
        }

        try
        {
            IntPtr windowHandle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            _hotkeyService.Attach(windowHandle);
            _hotkeyService.Register(_mainViewModel.HotkeyText);
            _mainViewModel.StatusText = _inputMonitoringError
                ?? $"Ready. Toggle hotkey: {_mainViewModel.HotkeyText}.";
        }
        catch (Exception ex)
        {
            _mainViewModel.StatusText = $"Hotkey unavailable: {ex.Message}";
        }
    }

    private void OnMainViewModelSettingsSaved(object? sender, EventArgs e)
    {
        if (_hotkeyService is null || _mainViewModel is null)
        {
            return;
        }

        try
        {
            _hotkeyService.Register(_mainViewModel.HotkeyText);
            _mainViewModel.StatusText = _inputMonitoringError
                ?? $"Settings saved. Toggle hotkey: {_mainViewModel.HotkeyText}.";
        }
        catch (Exception ex)
        {
            _mainViewModel.StatusText = $"Settings saved, but hotkey failed: {ex.Message}";
        }
    }

    private void OnMainViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.IsEnabled)
            or nameof(MainViewModel.LeftClickEnabled)
            or nameof(MainViewModel.RightClickEnabled))
        {
            _clickEngine?.ClearPendingClicks();
        }
    }

    private void OnClickInjectionFailed(object? sender, ClickInjectionFailedEventArgs e)
    {
        if (_mainViewModel is null)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            string buttonName = e.Button == MouseButtonKind.Left ? "left" : "right";
            _mainViewModel.StatusText =
                $"Could not inject a {buttonName} click: {e.Exception.Message}";
        });
    }

    private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        Cleanup();
    }

    private void Cleanup()
    {
        if (_isCleanedUp)
        {
            return;
        }

        _isCleanedUp = true;

        if (_mainViewModel is not null)
        {
            _mainViewModel.PropertyChanged -= OnMainViewModelPropertyChanged;
            _mainViewModel.SettingsSaved -= OnMainViewModelSettingsSaved;
        }

        if (_clickEngine is not null)
        {
            _clickEngine.InjectionFailed -= OnClickInjectionFailed;
        }

        DisposeSafely(_hotkeyService);
        DisposeSafely(_mouseHookService);
        DisposeSafely(_clickEngine);

        _clickEngine = null;
        _hotkeyService = null;
        _mainViewModel = null;
        _mouseHookService = null;
        _inputMonitoringError = null;
    }

    private static void DisposeSafely(IDisposable? disposable)
    {
        try
        {
            disposable?.Dispose();
        }
        catch
        {
            // Shutdown must continue so the remaining native hooks and worker are released.
        }
    }
}
