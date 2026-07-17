namespace MinecraftDoubleClicker.Services;

internal static class PlatformServiceFactory
{
    public static IInputInjector CreateInputInjector()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsInputInjector();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacInputInjector();
        }

        throw UnsupportedPlatform();
    }

    public static IForegroundWindowService CreateForegroundWindowService()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsForegroundWindowService();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacForegroundWindowService();
        }

        throw UnsupportedPlatform();
    }

    public static IMouseHookService CreateMouseHookService(ClickEngine clickEngine)
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsMouseHookService(clickEngine);
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacMouseHookService(clickEngine);
        }

        throw UnsupportedPlatform();
    }

    public static IHotkeyService CreateHotkeyService(Action onHotkeyPressed)
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsHotkeyService(onHotkeyPressed);
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacHotkeyService(onHotkeyPressed);
        }

        throw UnsupportedPlatform();
    }

    private static PlatformNotSupportedException UnsupportedPlatform()
    {
        return new PlatformNotSupportedException(
            "Minecraft Double Clicker currently supports Windows and macOS.");
    }
}
