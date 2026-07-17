namespace MinecraftDoubleClicker.Models;

[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Command = 8
}

public readonly record struct HotkeyDefinition(HotkeyModifiers Modifiers, string Key);
