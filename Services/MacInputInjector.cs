using MinecraftDoubleClicker.Interop;
using MinecraftDoubleClicker.Models;

namespace MinecraftDoubleClicker.Services;

internal sealed class MacInputInjector : IInputInjector
{
    internal const long InjectedEventMarker = 0x4D4344434C49434B;

    private bool _permissionRequested;

    public void Click(MouseButtonKind button)
    {
        EnsurePostPermission();

        IntPtr positionEvent = MacNativeMethods.CGEventCreate(IntPtr.Zero);

        if (positionEvent == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to determine the mouse position.");
        }

        MacNativeMethods.CGPoint position;

        try
        {
            position = MacNativeMethods.CGEventGetLocation(positionEvent);
        }
        finally
        {
            MacNativeMethods.CFRelease(positionEvent);
        }

        (uint downType, uint upType, uint nativeButton) = button switch
        {
            MouseButtonKind.Left => (1U, 2U, 0U),
            MouseButtonKind.Right => (3U, 4U, 1U),
            _ => throw new ArgumentOutOfRangeException(nameof(button), button, null)
        };

        PostMouseEvent(downType, nativeButton, position);
        PostMouseEvent(upType, nativeButton, position);
    }

    private void EnsurePostPermission()
    {
        if (MacNativeMethods.CGPreflightPostEventAccess())
        {
            return;
        }

        if (!_permissionRequested)
        {
            _permissionRequested = true;

            if (MacNativeMethods.CGRequestPostEventAccess())
            {
                return;
            }
        }

        throw new InvalidOperationException(
            "macOS denied Accessibility control. Enable Minecraft Double Clicker in System Settings > Privacy & Security > Accessibility.");
    }

    private static void PostMouseEvent(
        uint eventType,
        uint button,
        MacNativeMethods.CGPoint position)
    {
        IntPtr mouseEvent = MacNativeMethods.CGEventCreateMouseEvent(
            IntPtr.Zero,
            eventType,
            position,
            button);

        if (mouseEvent == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to create a macOS mouse event.");
        }

        try
        {
            MacNativeMethods.CGEventSetIntegerValueField(
                mouseEvent,
                MacNativeMethods.EventSourceUserDataField,
                InjectedEventMarker);
            MacNativeMethods.CGEventPost(MacNativeMethods.SessionEventTap, mouseEvent);
        }
        finally
        {
            MacNativeMethods.CFRelease(mouseEvent);
        }
    }
}
