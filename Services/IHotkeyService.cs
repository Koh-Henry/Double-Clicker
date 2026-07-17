namespace MinecraftDoubleClicker.Services;

internal interface IHotkeyService : IDisposable
{
    void Attach(IntPtr windowHandle);

    void Register(string hotkeyText);
}
