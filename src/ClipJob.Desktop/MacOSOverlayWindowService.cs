using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace ClipJob.Desktop;

public sealed class MacOSOverlayWindowService : IMacOSOverlayWindowService
{
    private const string ObjectiveCLibrary = "/usr/lib/libobjc.A.dylib";
    private const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string CoreGraphicsLibrary = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string NSWindowHandleDescriptor = "NSWindow";

    private const uint OnScreenOnly = 1;
    private const uint ExcludeDesktopElements = 1 << 4;
    private const int SignedInt32NumberType = 3;
    private const int MaximumDisplayCount = 16;

    private const nuint CanJoinAllSpaces = 1 << 0;
    private const nuint MoveToActiveSpace = 1 << 1;
    private const nuint FullScreenPrimary = 1 << 7;
    private const nuint FullScreenAuxiliary = 1 << 8;
    private const nuint FullScreenNone = 1 << 9;
    private const nint FloatingWindowLevel = 3;

    private static readonly IntPtr CoreGraphicsHandle = NativeLibrary.Load(CoreGraphicsLibrary);
    private static readonly IntPtr WindowOwnerProcessIdentifierKey = GetCoreGraphicsConstant("kCGWindowOwnerPID");
    private static readonly IntPtr WindowLayerKey = GetCoreGraphicsConstant("kCGWindowLayer");
    private static readonly IntPtr WindowBoundsKey = GetCoreGraphicsConstant("kCGWindowBounds");

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
        var application = objc_msgSend(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));

        objc_msgSend(nativeWindow, sel_registerName("orderFrontRegardless"));
        objc_msgSend_void_bool(application, sel_registerName("activateIgnoringOtherApps:"), true);
        objc_msgSend_void_IntPtr(nativeWindow, sel_registerName("makeKeyAndOrderFront:"), IntPtr.Zero);
    }

    public void PositionForApplication(Window window, int processIdentifier)
    {
        var screen = FindScreenForApplication(processIdentifier);
        if (screen == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "macOS could not match the captured application's window to a display.");
        }

        var nativeWindow = GetNativeWindow(window);
        var visibleFrame = objc_msgSend_NSRect(screen, sel_registerName("visibleFrame"));
        var windowFrame = objc_msgSend_NSRect(nativeWindow, sel_registerName("frame"));
        var origin = new NSPoint(
            visibleFrame.X + Math.Max(0, (visibleFrame.Width - windowFrame.Width) / 2),
            visibleFrame.Y + Math.Max(0, (visibleFrame.Height - windowFrame.Height) / 2));

        objc_msgSend_void_NSPoint(nativeWindow, sel_registerName("setFrameOrigin:"), origin);
        Trace.TraceInformation(
            $"Positioned ClipJob on the active screen at ({origin.X:F0}, {origin.Y:F0}).");
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

    private static IntPtr FindScreenForApplication(int processIdentifier)
    {
        if (processIdentifier <= 0)
        {
            return IntPtr.Zero;
        }

        var displays = new uint[MaximumDisplayCount];
        if (CGGetActiveDisplayList((uint)displays.Length, displays, out var displayCount) != 0)
        {
            return IntPtr.Zero;
        }

        var windowList = CGWindowListCopyWindowInfo(OnScreenOnly | ExcludeDesktopElements, 0);
        if (windowList == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var bestDisplayBounds = default(NSRect);
        var largestOverlap = 0d;

        try
        {
            var windowCount = CFArrayGetCount(windowList);
            for (nint windowIndex = 0; windowIndex < windowCount; windowIndex++)
            {
                var window = CFArrayGetValueAtIndex(windowList, windowIndex);
                if (!TryGetInt32(window, WindowOwnerProcessIdentifierKey, out var ownerProcessIdentifier) ||
                    ownerProcessIdentifier != processIdentifier ||
                    !TryGetInt32(window, WindowLayerKey, out var layer) ||
                    layer != 0 ||
                    !CGRectMakeWithDictionaryRepresentation(
                        CFDictionaryGetValue(window, WindowBoundsKey),
                        out var windowBounds))
                {
                    continue;
                }

                for (var displayIndex = 0; displayIndex < (int)displayCount; displayIndex++)
                {
                    var displayBounds = CGDisplayBounds(displays[displayIndex]);
                    var overlap = IntersectionArea(windowBounds, displayBounds);
                    if (overlap > largestOverlap)
                    {
                        largestOverlap = overlap;
                        bestDisplayBounds = displayBounds;
                    }
                }
            }
        }
        finally
        {
            CFRelease(windowList);
        }

        return largestOverlap > 0
            ? FindAppKitScreen(bestDisplayBounds)
            : IntPtr.Zero;
    }

    private static IntPtr FindAppKitScreen(NSRect coreGraphicsBounds)
    {
        var primaryDisplayBounds = CGDisplayBounds(CGMainDisplayID());
        var expectedFrame = new NSRect(
            coreGraphicsBounds.X,
            primaryDisplayBounds.Height - coreGraphicsBounds.Y - coreGraphicsBounds.Height,
            coreGraphicsBounds.Width,
            coreGraphicsBounds.Height);
        var screens = objc_msgSend(objc_getClass("NSScreen"), sel_registerName("screens"));
        var screenCount = objc_msgSend_nuint(screens, sel_registerName("count"));

        for (nuint index = 0; index < screenCount; index++)
        {
            var screen = objc_msgSend_IntPtr_nuint(screens, sel_registerName("objectAtIndex:"), index);
            var frame = objc_msgSend_NSRect(screen, sel_registerName("frame"));
            if (ApproximatelyEqual(frame, expectedFrame))
            {
                return screen;
            }
        }

        return IntPtr.Zero;
    }

    private static bool TryGetInt32(IntPtr dictionary, IntPtr key, out int value)
    {
        value = 0;
        var number = CFDictionaryGetValue(dictionary, key);
        return number != IntPtr.Zero &&
               CFNumberGetValue(number, SignedInt32NumberType, out value);
    }

    private static double IntersectionArea(NSRect left, NSRect right)
    {
        var width = Math.Max(0, Math.Min(left.Right, right.Right) - Math.Max(left.X, right.X));
        var height = Math.Max(0, Math.Min(left.Bottom, right.Bottom) - Math.Max(left.Y, right.Y));
        return width * height;
    }

    private static bool ApproximatelyEqual(NSRect left, NSRect right) =>
        Math.Abs(left.X - right.X) < 1 &&
        Math.Abs(left.Y - right.Y) < 1 &&
        Math.Abs(left.Width - right.Width) < 1 &&
        Math.Abs(left.Height - right.Height) < 1;

    private static IntPtr GetCoreGraphicsConstant(string name) =>
        Marshal.ReadIntPtr(NativeLibrary.GetExport(CoreGraphicsHandle, name));

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
    private static extern IntPtr objc_msgSend_IntPtr_nuint(IntPtr receiver, IntPtr selector, nuint value);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern NSRect objc_msgSend_NSRect(IntPtr receiver, IntPtr selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool objc_msgSend_bool_IntPtr(IntPtr receiver, IntPtr selector, IntPtr value);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_void_nuint(IntPtr receiver, IntPtr selector, nuint value);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_void_nint(IntPtr receiver, IntPtr selector, nint value);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_void_bool(
        IntPtr receiver,
        IntPtr selector,
        [MarshalAs(UnmanagedType.I1)] bool value);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_void_IntPtr(IntPtr receiver, IntPtr selector, IntPtr value);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_void_NSPoint(IntPtr receiver, IntPtr selector, NSPoint value);

    [DllImport(CoreGraphicsLibrary)]
    private static extern IntPtr CGWindowListCopyWindowInfo(uint option, uint relativeToWindow);

    [DllImport(CoreGraphicsLibrary)]
    private static extern int CGGetActiveDisplayList(
        uint maximumDisplayCount,
        [Out] uint[] activeDisplays,
        out uint displayCount);

    [DllImport(CoreGraphicsLibrary)]
    private static extern uint CGMainDisplayID();

    [DllImport(CoreGraphicsLibrary)]
    private static extern NSRect CGDisplayBounds(uint display);

    [DllImport(CoreGraphicsLibrary)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool CGRectMakeWithDictionaryRepresentation(IntPtr dictionary, out NSRect rectangle);

    [DllImport(CoreFoundationLibrary)]
    private static extern nint CFArrayGetCount(IntPtr array);

    [DllImport(CoreFoundationLibrary)]
    private static extern IntPtr CFArrayGetValueAtIndex(IntPtr array, nint index);

    [DllImport(CoreFoundationLibrary)]
    private static extern IntPtr CFDictionaryGetValue(IntPtr dictionary, IntPtr key);

    [DllImport(CoreFoundationLibrary)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool CFNumberGetValue(IntPtr number, int numberType, out int value);

    [DllImport(CoreFoundationLibrary)]
    private static extern void CFRelease(IntPtr value);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NSPoint(double x, double y)
    {
        public readonly double X = x;
        public readonly double Y = y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NSRect
    {
        public NSRect(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public readonly double X;
        public readonly double Y;
        public readonly double Width;
        public readonly double Height;

        public double Right => X + Width;
        public double Bottom => Y + Height;
    }

}
