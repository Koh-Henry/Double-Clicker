using MinecraftDoubleClicker.Models;

namespace MinecraftDoubleClicker.Services;

public static class HotkeyParser
{
    public static HotkeyDefinition Parse(string hotkeyText)
    {
        if (string.IsNullOrWhiteSpace(hotkeyText))
        {
            throw new ArgumentException("Hotkey cannot be empty.", nameof(hotkeyText));
        }

        string[] tokens = hotkeyText.Split(
            '+',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        HotkeyModifiers modifiers = HotkeyModifiers.None;
        string? key = null;

        foreach (string token in tokens)
        {
            switch (token.ToUpperInvariant())
            {
                case "CTRL":
                case "CONTROL":
                    modifiers |= HotkeyModifiers.Control;
                    break;
                case "ALT":
                case "OPTION":
                    modifiers |= HotkeyModifiers.Alt;
                    break;
                case "SHIFT":
                    modifiers |= HotkeyModifiers.Shift;
                    break;
                case "WIN":
                case "WINDOWS":
                case "CMD":
                case "COMMAND":
                    modifiers |= HotkeyModifiers.Command;
                    break;
                default:
                    if (key is not null)
                    {
                        throw new ArgumentException(
                            "Hotkey must contain only one non-modifier key.",
                            nameof(hotkeyText));
                    }

                    key = NormalizeKey(token);
                    break;
            }
        }

        if (key is null)
        {
            throw new ArgumentException(
                "Hotkey must include a key such as F8 or Ctrl+F8.",
                nameof(hotkeyText));
        }

        return new HotkeyDefinition(modifiers, key);
    }

    private static string NormalizeKey(string token)
    {
        string key = token.Trim().ToUpperInvariant();

        if (key.Length == 1 && char.IsLetterOrDigit(key[0]))
        {
            return key;
        }

        if (key.StartsWith('F')
            && int.TryParse(key.AsSpan(1), out int functionNumber)
            && functionNumber is >= 1 and <= 24)
        {
            return $"F{functionNumber}";
        }

        return key switch
        {
            "ESC" => "ESCAPE",
            "RETURN" => "ENTER",
            "DEL" => "DELETE",
            "PGUP" => "PAGEUP",
            "PGDN" => "PAGEDOWN",
            "LEFTARROW" => "LEFT",
            "RIGHTARROW" => "RIGHT",
            "UPARROW" => "UP",
            "DOWNARROW" => "DOWN",
            "SPACEBAR" => "SPACE",
            _ => key
        };
    }
}
