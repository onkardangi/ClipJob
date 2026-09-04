using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace ClipJob.Desktop;

public sealed class MacOSOverlayWindowService : IMacOSOverlayWindowService
{
    private const string ObjectiveCLibrary = "/usr/lib/libobjc.A.dylib";
    private const string NSWindowHandleDescriptor = "NSWindow";

    private const nuint CanJoinAllSpaces = 1 << 0;
    private const nuint MoveToActiveSpace = 1 << 1;
    private const nuint FullScreenPrimary = 1 << 7;
    private const nuint FullScreenAuxiliary = 1 << 8;
    private const nuint FullScreenNone = 1 << 9;
    private const nint FloatingWindowLevel = 3;

    public void Configure(Window window)
    {
        var nativeWindow = GetNativeWindow(window);
        var runtimeClass = object_getClass(nativeWindow);
        var runtimeClassName = Marshal.PtrToStringUTF8(class_getName(runtimeClass)) ?? "unknown";
        var panelClass = objc_getClass("NSPanel");
        var isPanel = objc_msgSend_bool_IntPtr(nativeWindow, sel_registerName("isKindOfClass:"), panelClass);

        var collectionBehavior = objc_msgSend_nuint(nativeWindow, sel_registerName("collectionBehavior"));
        collectionBehavior &= ~(MoveToActiveSpace | FullScreenPrimary | FullScreenNone);
        collectionBehavior |= CanJoinAllSpaces | FullScreenAuxiliary;

        objc_msgSend_void_nuint(nativeWindow, sel_registerName("setCollectionBehavior:"), collectionBehavior);
        SetFloating(window, true);

        Trace.TraceInformation(
            $"Configured macOS overlay window. Native class: {runtimeClassName}; NSPanel: {isPanel}; " +
            $"level: {FloatingWindowLevel}; collection behavior: 0x{collectionBehavior:X}.");
    }

    public void OrderFront(Window window)
    {
        var nativeWindow = GetNativeWindow(window);
        objc_msgSend(nativeWindow, sel_registerName("orderFrontRegardless"));
        window.Activate();
    }

    public void SetFloating(Window window, bool floating)
    {
        var level = floating ? FloatingWindowLevel : 0;
        objc_msgSend_void_nint(GetNativeWindow(window), sel_registerName("setLevel:"), level);
    }

    private static IntPtr GetNativeWindow(Window window)
    {
        var handle = window.TryGetPlatformHandle();
        if (handle is null || handle.Handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Avalonia did not expose a native macOS window handle.");
        }

        if (!string.Equals(handle.HandleDescriptor, NSWindowHandleDescriptor, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected an {NSWindowHandleDescriptor} platform handle, but Avalonia returned " +
                $"'{handle.HandleDescriptor ?? "<none>"}'.");
        }

        return handle.Handle;
    }

    [DllImport(ObjectiveCLibrary)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjectiveCLibrary)]
    private static extern IntPtr object_getClass(IntPtr value);

    [DllImport(ObjectiveCLibrary)]
    private static extern IntPtr class_getName(IntPtr value);

    [DllImport(ObjectiveCLibrary)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern nuint objc_msgSend_nuint(IntPtr receiver, IntPtr selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool objc_msgSend_bool_IntPtr(IntPtr receiver, IntPtr selector, IntPtr value);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_void_nuint(IntPtr receiver, IntPtr selector, nuint value);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_void_nint(IntPtr receiver, IntPtr selector, nint value);

}
