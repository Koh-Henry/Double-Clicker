namespace MinecraftDoubleClicker.Services;

internal interface IMouseHookService : IDisposable
{
    void Start();

    void Stop();
}
