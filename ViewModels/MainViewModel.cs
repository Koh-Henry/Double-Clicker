using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MinecraftDoubleClicker.Models;
using MinecraftDoubleClicker.Services;

namespace MinecraftDoubleClicker.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly Action _exitAction;
    private string _statusText = "Ready.";

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? SettingsSaved;

    public MainViewModel()
        : this(CreateDefaultSettingsService())
    {
    }

    private MainViewModel(SettingsService settingsService)
        : this(settingsService.Load(), settingsService, static () => { })
    {
    }

    public MainViewModel(AppSettings settings, SettingsService settingsService, Action exitAction)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _exitAction = exitAction ?? throw new ArgumentNullException(nameof(exitAction));
        SaveCommand = new RelayCommand(_ => Save());
        ExitCommand = new RelayCommand(_ => Exit());

        StatusText = File.Exists(_settingsService.SettingsFilePath)
            ? "Settings loaded."
            : "Ready.";
    }

    public bool IsEnabled
    {
        get => _settings.IsEnabled;
        set => SetSettingsField(_settings.IsEnabled, value, static (settings, newValue) => settings.IsEnabled = newValue);
    }

    public bool LeftClickEnabled
    {
        get => _settings.LeftClickEnabled;
        set => SetSettingsField(_settings.LeftClickEnabled, value, static (settings, newValue) => settings.LeftClickEnabled = newValue);
    }

    public bool RightClickEnabled
    {
        get => _settings.RightClickEnabled;
        set => SetSettingsField(_settings.RightClickEnabled, value, static (settings, newValue) => settings.RightClickEnabled = newValue);
    }

    public int TapMaxDurationMs
    {
        get => _settings.TapMaxDurationMs;
        set => SetSettingsField(_settings.TapMaxDurationMs, value, static (settings, newValue) => settings.TapMaxDurationMs = newValue);
    }

    public int ExtraClickDelayMs
    {
        get => _settings.ExtraClickDelayMs;
        set => SetSettingsField(_settings.ExtraClickDelayMs, value, static (settings, newValue) => settings.ExtraClickDelayMs = newValue);
    }

    public bool MinecraftOnly
    {
        get => _settings.MinecraftOnly;
        set => SetSettingsField(_settings.MinecraftOnly, value, static (settings, newValue) => settings.MinecraftOnly = newValue);
    }

    public string HotkeyText
    {
        get => _settings.HotkeyText;
        set => SetSettingsField(_settings.HotkeyText, value, static (settings, newValue) => settings.HotkeyText = newValue);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    public ICommand SaveCommand { get; }

    public ICommand ExitCommand { get; }

    private void Save()
    {
        try
        {
            _settingsService.Save(_settings);
            StatusText = $"Settings saved to {_settingsService.SettingsFilePath}.";
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to save settings: {ex.Message}";
        }
    }

    private void Exit()
    {
        _exitAction();
    }

    public void ToggleEnabledFromHotkey()
    {
        IsEnabled = !IsEnabled;
        StatusText = IsEnabled
            ? "Click doubling enabled via hotkey."
            : "Click doubling disabled via hotkey.";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private bool SetSettingsField<T>(
        T currentValue,
        T newValue,
        Action<AppSettings, T> setter,
        [CallerMemberName] string? propertyName = null)
    {
        if (Equals(currentValue, newValue))
        {
            return false;
        }

        setter(_settings, newValue);
        OnPropertyChanged(propertyName);
        return true;
    }

    private static SettingsService CreateDefaultSettingsService()
    {
        return new SettingsService();
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
