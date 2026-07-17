using MinecraftDoubleClicker.Models;

namespace MinecraftDoubleClicker.Services;

public sealed class ClickInjectionFailedEventArgs : EventArgs
{
    public ClickInjectionFailedEventArgs(MouseButtonKind button, Exception exception)
    {
        Button = button;
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }

    public MouseButtonKind Button { get; }

    public Exception Exception { get; }
}
