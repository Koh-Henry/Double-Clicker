using System.Runtime.InteropServices;
using MinecraftDoubleClicker.Interop;

namespace MinecraftDoubleClicker.Services;

internal sealed class MacForegroundWindowService : IForegroundWindowService
{
    private static readonly IntPtr SharedWorkspaceSelector = MacNativeMethods.sel_registerName("sharedWorkspace");
    private static readonly IntPtr FrontmostApplicationSelector = MacNativeMethods.sel_registerName("frontmostApplication");
    private static readonly IntPtr LocalizedNameSelector = MacNativeMethods.sel_registerName("localizedName");
    private static readonly IntPtr BundleIdentifierSelector = MacNativeMethods.sel_registerName("bundleIdentifier");
    private static readonly IntPtr Utf8StringSelector = MacNativeMethods.sel_registerName("UTF8String");

    public bool IsMinecraftFocused()
    {
        IntPtr autoreleasePool = MacNativeMethods.objc_autoreleasePoolPush();

        try
        {
            IntPtr workspaceClass = MacNativeMethods.objc_getClass("NSWorkspace");
            IntPtr workspace = Send(workspaceClass, SharedWorkspaceSelector);
            IntPtr application = Send(workspace, FrontmostApplicationSelector);

            string? name = GetString(Send(application, LocalizedNameSelector));
            string? bundleIdentifier = GetString(Send(application, BundleIdentifierSelector));

            return ContainsMinecraftProcessName(name)
                || ContainsMinecraftProcessName(bundleIdentifier);
        }
        finally
        {
            MacNativeMethods.objc_autoreleasePoolPop(autoreleasePool);
        }
    }

    private static bool ContainsMinecraftProcessName(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && (value.Contains("minecraft", StringComparison.OrdinalIgnoreCase)
                || value.Equals("java", StringComparison.OrdinalIgnoreCase)
                || value.Equals("javaw", StringComparison.OrdinalIgnoreCase));
    }

    private static IntPtr Send(IntPtr receiver, IntPtr selector)
    {
        return receiver == IntPtr.Zero
            ? IntPtr.Zero
            : MacNativeMethods.IntPtr_objc_msgSend(receiver, selector);
    }

    private static string? GetString(IntPtr nativeString)
    {
        IntPtr utf8String = Send(nativeString, Utf8StringSelector);
        return utf8String == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(utf8String);
    }
}
