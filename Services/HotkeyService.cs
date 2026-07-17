using System.ComponentModel;
using System.Runtime.InteropServices;
using MinecraftDoubleClicker.Interop;
using MinecraftDoubleClicker.Models;

namespace MinecraftDoubleClicker.Services;

internal sealed class WindowsHotkeyService : IHotkeyService
{
    private const int HotkeyId = 0x4D4344;

    private readonly Action _onHotkeyPressed;
    private readonly NativeMethods.WindowProc _windowProc;
    private readonly string _windowClassName = $"MinecraftDoubleClicker.Hotkey.{Environment.ProcessId}";

    private IntPtr _windowHandle;
    private IntPtr _moduleHandle;
    private ushort _windowClassAtom;
    private bool _isRegistered;
    private bool _isDisposed;

    public WindowsHotkeyService(Action onHotkeyPressed)
    {
        _onHotkeyPressed = onHotkeyPressed ?? throw new ArgumentNullException(nameof(onHotkeyPressed));
        _windowProc = WindowProc;
    }

    public void Attach(IntPtr windowHandle)
    {
        ThrowIfDisposed();

        if (_windowHandle != IntPtr.Zero)
        {
            return;
        }

        _moduleHandle = NativeMethods.GetModuleHandle(null);

        if (_moduleHandle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to resolve the application module.");
        }

        WNDCLASSEX windowClass = new()
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_windowProc),
            hInstance = _moduleHandle,
            lpszClassName = _windowClassName
        };
        _windowClassAtom = NativeMethods.RegisterClassEx(ref windowClass);

        if (_windowClassAtom == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to register the hotkey message window.");
        }

        _windowHandle = NativeMethods.CreateWindowEx(
            0,
            _windowClassName,
            null,
            0,
            0,
            0,
            0,
            0,
            new IntPtr(-3),
            IntPtr.Zero,
            _moduleHandle,
            IntPtr.Zero);

        if (_windowHandle == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            NativeMethods.UnregisterClass(_windowClassName, _moduleHandle);
            _windowClassAtom = 0;
            throw new Win32Exception(error, "Unable to create the hotkey message window.");
        }
    }

    public void Register(string hotkeyText)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(hotkeyText);

        if (_windowHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Hotkey service must be attached to a window before registration.");
        }

        HotkeyDefinition hotkey = HotkeyParser.Parse(hotkeyText);
        uint virtualKey = GetVirtualKey(hotkey.Key);
        uint modifiers = GetNativeModifiers(hotkey.Modifiers) | NativeConstants.MOD_NOREPEAT;

        UnregisterCurrentHotkey();

        if (!NativeMethods.RegisterHotKey(_windowHandle, HotkeyId, modifiers, virtualKey))
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                $"Windows could not register hotkey '{hotkeyText.Trim()}'. It may already be in use.");
        }

        _isRegistered = true;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        try
        {
            UnregisterCurrentHotkey();

            if (_windowHandle != IntPtr.Zero)
            {
                NativeMethods.DestroyWindow(_windowHandle);
            }

            if (_windowClassAtom != 0)
            {
                NativeMethods.UnregisterClass(_windowClassName, _moduleHandle);
            }
        }
        finally
        {
            _windowHandle = IntPtr.Zero;
            _moduleHandle = IntPtr.Zero;
            _windowClassAtom = 0;
            _isDisposed = true;
            GC.KeepAlive(_windowProc);
        }
    }

    private IntPtr WindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == NativeConstants.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            _onHotkeyPressed();
            return IntPtr.Zero;
        }

        return NativeMethods.DefWindowProc(hwnd, message, wParam, lParam);
    }

    private void UnregisterCurrentHotkey()
    {
        if (!_isRegistered || _windowHandle == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.UnregisterHotKey(_windowHandle, HotkeyId);
        _isRegistered = false;
    }

    private static uint GetNativeModifiers(HotkeyModifiers modifiers)
    {
        uint nativeModifiers = 0;

        if (modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            nativeModifiers |= NativeConstants.MOD_ALT;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Control))
        {
            nativeModifiers |= NativeConstants.MOD_CONTROL;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            nativeModifiers |= NativeConstants.MOD_SHIFT;
        }

        if (modifiers.HasFlag(HotkeyModifiers.Command))
        {
            nativeModifiers |= NativeConstants.MOD_WIN;
        }

        return nativeModifiers;
    }

    private static uint GetVirtualKey(string key)
    {
        if (key.Length == 1 && char.IsLetterOrDigit(key[0]))
        {
            return key[0];
        }

        if (key.StartsWith('F')
            && int.TryParse(key.AsSpan(1), out int functionNumber)
            && functionNumber is >= 1 and <= 24)
        {
            return (uint)(0x70 + functionNumber - 1);
        }

        return key switch
        {
            "BACKSPACE" => 0x08,
            "TAB" => 0x09,
            "ENTER" => 0x0D,
            "ESCAPE" => 0x1B,
            "SPACE" => 0x20,
            "PAGEUP" => 0x21,
            "PAGEDOWN" => 0x22,
            "END" => 0x23,
            "HOME" => 0x24,
            "LEFT" => 0x25,
            "UP" => 0x26,
            "RIGHT" => 0x27,
            "DOWN" => 0x28,
            "INSERT" => 0x2D,
            "DELETE" => 0x2E,
            _ => throw new ArgumentException($"Unsupported hotkey key '{key}'.")
        };
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }
}
