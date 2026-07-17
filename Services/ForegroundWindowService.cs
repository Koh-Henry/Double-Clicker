using System;
using System.Diagnostics;
using MinecraftDoubleClicker.Interop;

namespace MinecraftDoubleClicker.Services;

internal sealed class WindowsForegroundWindowService : IForegroundWindowService
{
    private readonly object _sync = new();

    private IntPtr _lastForegroundWindow = IntPtr.Zero;
    private uint _lastProcessId;
    private string? _lastProcessName;

    public bool IsMinecraftFocused()
    {
        string? processName = GetForegroundProcessName();

        if (string.IsNullOrWhiteSpace(processName))
        {
            return false;
        }

        return processName.Equals("javaw", StringComparison.OrdinalIgnoreCase)
            || processName.Equals("java", StringComparison.OrdinalIgnoreCase);
    }

    public string? GetForegroundProcessName()
    {
        IntPtr foregroundWindow = NativeMethods.GetForegroundWindow();

        if (foregroundWindow == IntPtr.Zero)
        {
            return null;
        }

        uint threadId = NativeMethods.GetWindowThreadProcessId(foregroundWindow, out uint processId);

        if (threadId == 0 || processId == 0)
        {
            return null;
        }

        lock (_sync)
        {
            if (foregroundWindow == _lastForegroundWindow && processId == _lastProcessId)
            {
                return _lastProcessName;
            }
        }

        string? processName;

        try
        {
            using Process process = Process.GetProcessById((int)processId);
            processName = process.ProcessName;
        }
        catch (ArgumentException)
        {
            processName = null;
        }
        catch (InvalidOperationException)
        {
            processName = null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            processName = null;
        }
        catch (NotSupportedException)
        {
            processName = null;
        }

        lock (_sync)
        {
            _lastForegroundWindow = foregroundWindow;
            _lastProcessId = processId;
            _lastProcessName = processName;
        }

        return processName;
    }
}
