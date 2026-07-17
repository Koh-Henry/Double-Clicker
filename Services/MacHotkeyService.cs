using MinecraftDoubleClicker.Interop;
using MinecraftDoubleClicker.Models;

namespace MinecraftDoubleClicker.Services;

internal sealed class MacHotkeyService : IHotkeyService
{
    private const uint KeyDownEvent = 10;
    private const ulong ShiftFlag = 0x00020000;
    private const ulong ControlFlag = 0x00040000;
    private const ulong AltFlag = 0x00080000;
    private const ulong CommandFlag = 0x00100000;
    private const ulong RelevantFlags = ShiftFlag | ControlFlag | AltFlag | CommandFlag;

    private readonly Action _onHotkeyPressed;
    private readonly object _sync = new();
    private readonly MacEventTap _eventTap;

    private HotkeyDefinition _hotkey;
    private ushort _keyCode;
    private bool _isRegistered;
    private bool _isStarted;

    public MacHotkeyService(Action onHotkeyPressed)
    {
        _onHotkeyPressed = onHotkeyPressed ?? throw new ArgumentNullException(nameof(onHotkeyPressed));
        _eventTap = new MacEventTap(1UL << (int)KeyDownEvent, OnKeyEvent);
    }

    public void Attach(IntPtr windowHandle)
    {
        // Quartz event taps are process-wide and do not need a window handle.
    }

    public void Register(string hotkeyText)
    {
        HotkeyDefinition hotkey = HotkeyParser.Parse(hotkeyText);
        ushort keyCode = GetKeyCode(hotkey.Key);

        lock (_sync)
        {
            _hotkey = hotkey;
            _keyCode = keyCode;
            _isRegistered = true;
        }

        if (!_isStarted)
        {
            try
            {
                _eventTap.Start();
                _isStarted = true;
            }
            catch
            {
                lock (_sync)
                {
                    _isRegistered = false;
                }

                throw;
            }
        }
    }

    public void Dispose() => _eventTap.Dispose();

    private IntPtr OnKeyEvent(IntPtr proxy, uint eventType, IntPtr eventRef, IntPtr userInfo)
    {
        if (eventType != KeyDownEvent
            || MacNativeMethods.CGEventGetIntegerValueField(
                eventRef,
                MacNativeMethods.KeyboardEventAutorepeatField) != 0)
        {
            return eventRef;
        }

        HotkeyDefinition hotkey;
        ushort keyCode;

        lock (_sync)
        {
            if (!_isRegistered)
            {
                return eventRef;
            }

            hotkey = _hotkey;
            keyCode = _keyCode;
        }

        ushort eventKeyCode = (ushort)MacNativeMethods.CGEventGetIntegerValueField(
            eventRef,
            MacNativeMethods.KeyboardEventKeycodeField);
        ulong eventFlags = MacNativeMethods.CGEventGetFlags(eventRef) & RelevantFlags;

        if (eventKeyCode == keyCode && eventFlags == GetNativeFlags(hotkey.Modifiers))
        {
            _onHotkeyPressed();
        }

        return eventRef;
    }

    private static ulong GetNativeFlags(HotkeyModifiers modifiers)
    {
        ulong flags = 0;

        if (modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            flags |= ShiftFlag;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Control))
        {
            flags |= ControlFlag;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            flags |= AltFlag;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Command))
        {
            flags |= CommandFlag;
        }

        return flags;
    }

    private static ushort GetKeyCode(string key)
    {
        if (MacKeyCodes.TryGetValue(key, out ushort keyCode))
        {
            return keyCode;
        }

        throw new ArgumentException($"Unsupported macOS hotkey key '{key}'.");
    }

    private static readonly Dictionary<string, ushort> MacKeyCodes =
        new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = 0,
            ["S"] = 1,
            ["D"] = 2,
            ["F"] = 3,
            ["H"] = 4,
            ["G"] = 5,
            ["Z"] = 6,
            ["X"] = 7,
            ["C"] = 8,
            ["V"] = 9,
            ["B"] = 11,
            ["Q"] = 12,
            ["W"] = 13,
            ["E"] = 14,
            ["R"] = 15,
            ["Y"] = 16,
            ["T"] = 17,
            ["1"] = 18,
            ["2"] = 19,
            ["3"] = 20,
            ["4"] = 21,
            ["6"] = 22,
            ["5"] = 23,
            ["9"] = 25,
            ["7"] = 26,
            ["8"] = 28,
            ["0"] = 29,
            ["O"] = 31,
            ["U"] = 32,
            ["I"] = 34,
            ["P"] = 35,
            ["ENTER"] = 36,
            ["L"] = 37,
            ["J"] = 38,
            ["K"] = 40,
            ["N"] = 45,
            ["M"] = 46,
            ["TAB"] = 48,
            ["SPACE"] = 49,
            ["BACKSPACE"] = 51,
            ["ESCAPE"] = 53,
            ["F1"] = 122,
            ["F2"] = 120,
            ["F3"] = 99,
            ["F4"] = 118,
            ["F5"] = 96,
            ["F6"] = 97,
            ["F7"] = 98,
            ["F8"] = 100,
            ["F9"] = 101,
            ["F10"] = 109,
            ["F11"] = 103,
            ["F12"] = 111,
            ["F13"] = 105,
            ["F14"] = 107,
            ["F15"] = 113,
            ["F16"] = 106,
            ["F17"] = 64,
            ["F18"] = 79,
            ["F19"] = 80,
            ["F20"] = 90,
            ["HOME"] = 115,
            ["PAGEUP"] = 116,
            ["DELETE"] = 117,
            ["END"] = 119,
            ["PAGEDOWN"] = 121,
            ["LEFT"] = 123,
            ["RIGHT"] = 124,
            ["DOWN"] = 125,
            ["UP"] = 126
        };
}
