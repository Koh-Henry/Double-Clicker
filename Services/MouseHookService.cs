using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MinecraftDoubleClicker.Interop;
using MinecraftDoubleClicker.Models;

namespace MinecraftDoubleClicker.Services;

internal sealed class WindowsMouseHookService : IMouseHookService
{
    private readonly ClickEngine _clickEngine;
    private readonly NativeMethods.HookProc _hookProc;

    private IntPtr _hookHandle = IntPtr.Zero;
    private bool _isDisposed;

    public WindowsMouseHookService(ClickEngine clickEngine)
    {
        _clickEngine = clickEngine ?? throw new ArgumentNullException(nameof(clickEngine));
        _hookProc = HookCallback;
    }

    public void Start()
    {
        ThrowIfDisposed();

        if (_hookHandle != IntPtr.Zero)
        {
            return;
        }

        _hookHandle = InstallHook();

        if (_hookHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to install low-level mouse hook.");
        }
    }

    public void Stop()
    {
        if (_hookHandle == IntPtr.Zero)
        {
            return;
        }

        bool success = NativeMethods.UnhookWindowsHookEx(_hookHandle);
        _hookHandle = IntPtr.Zero;

        if (!success)
        {
            throw new InvalidOperationException("Failed to remove low-level mouse hook.");
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        try
        {
            Stop();
        }
        finally
        {
            _isDisposed = true;
        }
    }

    private IntPtr InstallHook()
    {
        using Process currentProcess = Process.GetCurrentProcess();
        using ProcessModule? currentModule = currentProcess.MainModule;

        if (currentModule?.ModuleName is null)
        {
            throw new InvalidOperationException("Unable to resolve current process module.");
        }

        IntPtr moduleHandle = NativeMethods.GetModuleHandle(currentModule.ModuleName);

        if (moduleHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to resolve module handle for mouse hook.");
        }

        return NativeMethods.SetWindowsHookEx(
            NativeConstants.WH_MOUSE_LL,
            _hookProc,
            moduleHandle,
            0
        );
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
        {
            return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        MSLLHOOKSTRUCT hookData = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
        bool isInjected = (hookData.flags & NativeConstants.LLMHF_INJECTED) != 0;

        if (!isInjected)
        {
            int message = wParam.ToInt32();

            if (message == NativeConstants.WM_LBUTTONDOWN)
            {
                _clickEngine.HandleButtonDown(MouseButtonKind.Left);
            }
            else if (message == NativeConstants.WM_LBUTTONUP)
            {
                _clickEngine.HandleButtonUp(MouseButtonKind.Left);
            }
            else if (message == NativeConstants.WM_RBUTTONDOWN)
            {
                _clickEngine.HandleButtonDown(MouseButtonKind.Right);
            }
            else if (message == NativeConstants.WM_RBUTTONUP)
            {
                _clickEngine.HandleButtonUp(MouseButtonKind.Right);
            }
        }
        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }
}
