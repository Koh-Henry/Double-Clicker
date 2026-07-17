namespace MinecraftDoubleClicker.Models;

public sealed class AppSettings
{
    private readonly object _sync = new();
    private bool _isEnabled = true;
    private bool _leftClickEnabled = true;
    private bool _rightClickEnabled;
    private int _tapMaxDurationMs = 140;
    private int _extraClickDelayMs = 12;
    private bool _minecraftOnly = true;
    private string _hotkeyText = "F8";

    public bool IsEnabled
    {
        get { lock (_sync) { return _isEnabled; } }
        set { lock (_sync) { _isEnabled = value; } }
    }

    public bool LeftClickEnabled
    {
        get { lock (_sync) { return _leftClickEnabled; } }
        set { lock (_sync) { _leftClickEnabled = value; } }
    }

    public bool RightClickEnabled
    {
        get { lock (_sync) { return _rightClickEnabled; } }
        set { lock (_sync) { _rightClickEnabled = value; } }
    }

    public int TapMaxDurationMs
    {
        get { lock (_sync) { return _tapMaxDurationMs; } }
        set { lock (_sync) { _tapMaxDurationMs = value; } }
    }

    public int ExtraClickDelayMs
    {
        get { lock (_sync) { return _extraClickDelayMs; } }
        set { lock (_sync) { _extraClickDelayMs = value; } }
    }

    public bool MinecraftOnly
    {
        get { lock (_sync) { return _minecraftOnly; } }
        set { lock (_sync) { _minecraftOnly = value; } }
    }

    public string HotkeyText
    {
        get { lock (_sync) { return _hotkeyText; } }
        set { lock (_sync) { _hotkeyText = value; } }
    }

    public ClickSettingsSnapshot GetClickSettingsSnapshot()
    {
        lock (_sync)
        {
            return new ClickSettingsSnapshot(
                _isEnabled,
                _leftClickEnabled,
                _rightClickEnabled,
                _tapMaxDurationMs,
                _extraClickDelayMs,
                _minecraftOnly);
        }
    }
}

public readonly record struct ClickSettingsSnapshot(
    bool IsEnabled,
    bool LeftClickEnabled,
    bool RightClickEnabled,
    int TapMaxDurationMs,
    int ExtraClickDelayMs,
    bool MinecraftOnly)
{
    public bool IsButtonEnabled(MouseButtonKind button)
    {
        return button switch
        {
            MouseButtonKind.Left => LeftClickEnabled,
            MouseButtonKind.Right => RightClickEnabled,
            _ => false
        };
    }
}
