using System.ComponentModel;
using System.Runtime.InteropServices;
using MinecraftDoubleClicker.Interop;
using MinecraftDoubleClicker.Models;

namespace MinecraftDoubleClicker.Services;

internal sealed class WindowsInputInjector : IInputInjector
{
    public void Click(MouseButtonKind button)
    {
        (uint downFlag, uint upFlag) = button switch
        {
            MouseButtonKind.Left => (NativeConstants.MOUSEEVENTF_LEFTDOWN, NativeConstants.MOUSEEVENTF_LEFTUP),
            MouseButtonKind.Right => (NativeConstants.MOUSEEVENTF_RIGHTDOWN, NativeConstants.MOUSEEVENTF_RIGHTUP),
            _ => throw new ArgumentOutOfRangeException(nameof(button), button, null)
        };

        INPUT[] inputs =
        [
            CreateMouseInput(downFlag),
            CreateMouseInput(upFlag)
        ];

        uint inserted = NativeMethods.SendInput(
            (uint)inputs.Length,
            inputs,
            Marshal.SizeOf<INPUT>());

        if (inserted != inputs.Length)
        {
            string buttonName = button == MouseButtonKind.Left ? "left" : "right";
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                $"Failed to inject {buttonName} click input.");
        }
    }

    private static INPUT CreateMouseInput(uint flags)
    {
        return new INPUT
        {
            type = NativeConstants.INPUT_MOUSE,
            U = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dwFlags = flags
                }
            }
        };
    }
}
