using System.Runtime.InteropServices;

namespace MinecraftDoubleClicker.Interop;

internal static class MacNativeMethods
{
    internal const string ApplicationServices = "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";
    internal const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    internal const string ObjectiveC = "/usr/lib/libobjc.A.dylib";

    internal const uint SessionEventTap = 1;
    internal const uint HeadInsertEventTap = 0;
    internal const uint ListenOnlyEventTap = 1;
    internal const uint Utf8Encoding = 0x08000100;

    internal const int EventSourceUserDataField = 42;
    internal const int KeyboardEventAutorepeatField = 8;
    internal const int KeyboardEventKeycodeField = 9;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr EventTapCallback(
        IntPtr proxy,
        uint eventType,
        IntPtr eventRef,
        IntPtr userInfo);

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct CGPoint
    {
        internal readonly double X;
        internal readonly double Y;
    }

    [DllImport(ApplicationServices)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool CGPreflightListenEventAccess();

    [DllImport(ApplicationServices)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool CGRequestListenEventAccess();

    [DllImport(ApplicationServices)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool CGPreflightPostEventAccess();

    [DllImport(ApplicationServices)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool CGRequestPostEventAccess();

    [DllImport(ApplicationServices)]
    internal static extern IntPtr CGEventTapCreate(
        uint tap,
        uint place,
        uint options,
        ulong eventsOfInterest,
        EventTapCallback callback,
        IntPtr userInfo);

    [DllImport(ApplicationServices)]
    internal static extern void CGEventTapEnable(
        IntPtr tap,
        [MarshalAs(UnmanagedType.I1)] bool enable);

    [DllImport(ApplicationServices)]
    internal static extern long CGEventGetIntegerValueField(IntPtr eventRef, int field);

    [DllImport(ApplicationServices)]
    internal static extern void CGEventSetIntegerValueField(IntPtr eventRef, int field, long value);

    [DllImport(ApplicationServices)]
    internal static extern ulong CGEventGetFlags(IntPtr eventRef);

    [DllImport(ApplicationServices)]
    internal static extern IntPtr CGEventCreate(IntPtr source);

    [DllImport(ApplicationServices)]
    internal static extern CGPoint CGEventGetLocation(IntPtr eventRef);

    [DllImport(ApplicationServices)]
    internal static extern IntPtr CGEventCreateMouseEvent(
        IntPtr source,
        uint mouseType,
        CGPoint mouseCursorPosition,
        uint mouseButton);

    [DllImport(ApplicationServices)]
    internal static extern void CGEventPost(uint tap, IntPtr eventRef);

    [DllImport(CoreFoundation, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    internal static extern IntPtr CFMachPortCreateRunLoopSource(
        IntPtr allocator,
        IntPtr port,
        nint order);

    [DllImport(CoreFoundation)]
    internal static extern IntPtr CFRunLoopGetCurrent();

    [DllImport(CoreFoundation)]
    internal static extern void CFRunLoopAddSource(
        IntPtr runLoop,
        IntPtr source,
        IntPtr mode);

    [DllImport(CoreFoundation)]
    internal static extern void CFRunLoopStop(IntPtr runLoop);

    [DllImport(CoreFoundation)]
    internal static extern void CFRunLoopRun();

    [DllImport(CoreFoundation)]
    internal static extern IntPtr CFStringCreateWithCString(
        IntPtr allocator,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string value,
        uint encoding);

    [DllImport(CoreFoundation)]
    internal static extern void CFRelease(IntPtr value);

    [DllImport(ObjectiveC, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    internal static extern IntPtr objc_getClass(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjectiveC, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    internal static extern IntPtr sel_registerName(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjectiveC, EntryPoint = "objc_msgSend")]
    internal static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(ObjectiveC)]
    internal static extern IntPtr objc_autoreleasePoolPush();

    [DllImport(ObjectiveC)]
    internal static extern void objc_autoreleasePoolPop(IntPtr pool);
}
