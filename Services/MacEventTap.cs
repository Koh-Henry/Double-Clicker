using MinecraftDoubleClicker.Interop;

namespace MinecraftDoubleClicker.Services;

internal sealed class MacEventTap : IDisposable
{
    private const uint TapDisabledByTimeout = 0xFFFFFFFE;
    private const uint TapDisabledByUserInput = 0xFFFFFFFF;

    private readonly ulong _eventMask;
    private readonly MacNativeMethods.EventTapCallback _eventCallback;
    private readonly MacNativeMethods.EventTapCallback _nativeCallback;
    private readonly ManualResetEventSlim _started = new(false);

    private Thread? _thread;
    private Exception? _startError;
    private IntPtr _eventTap;
    private IntPtr _runLoop;
    private bool _isDisposed;

    public MacEventTap(ulong eventMask, MacNativeMethods.EventTapCallback eventCallback)
    {
        _eventMask = eventMask;
        _eventCallback = eventCallback ?? throw new ArgumentNullException(nameof(eventCallback));
        _nativeCallback = OnEvent;
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_thread is not null)
        {
            return;
        }

        if (!MacNativeMethods.CGPreflightListenEventAccess()
            && !MacNativeMethods.CGRequestListenEventAccess())
        {
            throw new InvalidOperationException(
                "macOS denied Input Monitoring. Enable Minecraft Double Clicker in System Settings > Privacy & Security > Input Monitoring, then restart the app.");
        }

        _thread = new Thread(RunEventTap)
        {
            IsBackground = true,
            Name = "MacEventTap"
        };
        _thread.Start();
        _started.Wait();

        if (_startError is not null)
        {
            Exception error = _startError;
            _thread.Join();
            _thread = null;
            throw new InvalidOperationException("Failed to create the macOS event tap.", error);
        }
    }

    public void Stop()
    {
        Thread? thread = _thread;

        if (thread is null)
        {
            return;
        }

        IntPtr runLoop = Volatile.Read(ref _runLoop);

        if (runLoop != IntPtr.Zero)
        {
            MacNativeMethods.CFRunLoopStop(runLoop);
        }

        thread.Join();
        _thread = null;
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
            _started.Dispose();
        }
    }

    private void RunEventTap()
    {
        IntPtr source = IntPtr.Zero;
        IntPtr mode = IntPtr.Zero;

        try
        {
            _eventTap = MacNativeMethods.CGEventTapCreate(
                MacNativeMethods.SessionEventTap,
                MacNativeMethods.HeadInsertEventTap,
                MacNativeMethods.ListenOnlyEventTap,
                _eventMask,
                _nativeCallback,
                IntPtr.Zero);

            if (_eventTap == IntPtr.Zero)
            {
                throw new InvalidOperationException("CGEventTapCreate returned null.");
            }

            source = MacNativeMethods.CFMachPortCreateRunLoopSource(IntPtr.Zero, _eventTap, 0);
            mode = MacNativeMethods.CFStringCreateWithCString(
                IntPtr.Zero,
                "kCFRunLoopCommonModes",
                MacNativeMethods.Utf8Encoding);

            if (source == IntPtr.Zero || mode == IntPtr.Zero)
            {
                throw new InvalidOperationException("Unable to create the macOS event-tap run loop source.");
            }

            _runLoop = MacNativeMethods.CFRunLoopGetCurrent();
            MacNativeMethods.CFRunLoopAddSource(_runLoop, source, mode);
            MacNativeMethods.CGEventTapEnable(_eventTap, true);
            _started.Set();
            MacNativeMethods.CFRunLoopRun();
        }
        catch (Exception ex)
        {
            _startError = ex;
            _started.Set();
        }
        finally
        {
            _runLoop = IntPtr.Zero;

            if (mode != IntPtr.Zero)
            {
                MacNativeMethods.CFRelease(mode);
            }

            if (source != IntPtr.Zero)
            {
                MacNativeMethods.CFRelease(source);
            }

            if (_eventTap != IntPtr.Zero)
            {
                MacNativeMethods.CFRelease(_eventTap);
                _eventTap = IntPtr.Zero;
            }

            GC.KeepAlive(_nativeCallback);
        }
    }

    private IntPtr OnEvent(IntPtr proxy, uint eventType, IntPtr eventRef, IntPtr userInfo)
    {
        if (eventType is TapDisabledByTimeout or TapDisabledByUserInput)
        {
            if (_eventTap != IntPtr.Zero)
            {
                MacNativeMethods.CGEventTapEnable(_eventTap, true);
            }

            return eventRef;
        }

        try
        {
            return _eventCallback(proxy, eventType, eventRef, userInfo);
        }
        catch
        {
            return eventRef;
        }
    }
}
