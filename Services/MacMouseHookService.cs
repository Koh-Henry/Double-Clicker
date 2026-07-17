using MinecraftDoubleClicker.Interop;
using MinecraftDoubleClicker.Models;

namespace MinecraftDoubleClicker.Services;

internal sealed class MacMouseHookService : IMouseHookService
{
    private const uint LeftMouseDown = 1;
    private const uint LeftMouseUp = 2;
    private const uint RightMouseDown = 3;
    private const uint RightMouseUp = 4;

    private readonly ClickEngine _clickEngine;
    private readonly MacEventTap _eventTap;

    public MacMouseHookService(ClickEngine clickEngine)
    {
        _clickEngine = clickEngine ?? throw new ArgumentNullException(nameof(clickEngine));
        ulong eventMask = MaskFor(LeftMouseDown)
            | MaskFor(LeftMouseUp)
            | MaskFor(RightMouseDown)
            | MaskFor(RightMouseUp);
        _eventTap = new MacEventTap(eventMask, OnMouseEvent);
    }

    public void Start() => _eventTap.Start();

    public void Stop() => _eventTap.Stop();

    public void Dispose() => _eventTap.Dispose();

    private IntPtr OnMouseEvent(IntPtr proxy, uint eventType, IntPtr eventRef, IntPtr userInfo)
    {
        long marker = MacNativeMethods.CGEventGetIntegerValueField(
            eventRef,
            MacNativeMethods.EventSourceUserDataField);

        if (marker == MacInputInjector.InjectedEventMarker)
        {
            return eventRef;
        }

        switch (eventType)
        {
            case LeftMouseDown:
                _clickEngine.HandleButtonDown(MouseButtonKind.Left);
                break;
            case LeftMouseUp:
                _clickEngine.HandleButtonUp(MouseButtonKind.Left);
                break;
            case RightMouseDown:
                _clickEngine.HandleButtonDown(MouseButtonKind.Right);
                break;
            case RightMouseUp:
                _clickEngine.HandleButtonUp(MouseButtonKind.Right);
                break;
        }

        return eventRef;
    }

    private static ulong MaskFor(uint eventType) => 1UL << (int)eventType;
}
