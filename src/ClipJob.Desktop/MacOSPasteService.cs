using System.Runtime.InteropServices;

namespace ClipJob.Desktop;

public sealed class MacOSPasteService : IPasteService
{
    private const string ApplicationServicesFramework =
        "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";
    private const string CoreFoundationFramework =
        "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const ushort VKeyCode = 9;
    private const ulong CommandFlag = 1UL << 20;
    private const uint HidEventTap = 0;

    public void Paste()
    {
        if (!AXIsProcessTrusted())
        {
            throw new InvalidOperationException(
                "ClipJob needs Accessibility permission to paste. Grant it in System Settings > Privacy & Security > Accessibility, then relaunch ClipJob.");
        }

        var keyDown = CGEventCreateKeyboardEvent(IntPtr.Zero, VKeyCode, true);
        if (keyDown == IntPtr.Zero)
        {
            throw new InvalidOperationException("CoreGraphics could not create the Command+V key-down event.");
        }

        var keyUp = CGEventCreateKeyboardEvent(IntPtr.Zero, VKeyCode, false);
        if (keyUp == IntPtr.Zero)
        {
            CFRelease(keyDown);
            throw new InvalidOperationException("CoreGraphics could not create the Command+V key-up event.");
        }

        try
        {
            CGEventSetFlags(keyDown, CommandFlag);
            CGEventSetFlags(keyUp, CommandFlag);
            CGEventPost(HidEventTap, keyDown);
            CGEventPost(HidEventTap, keyUp);
        }
        finally
        {
            CFRelease(keyUp);
            CFRelease(keyDown);
        }
    }

    [DllImport(ApplicationServicesFramework)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool AXIsProcessTrusted();

    [DllImport(ApplicationServicesFramework)]
    private static extern IntPtr CGEventCreateKeyboardEvent(
        IntPtr source,
        ushort virtualKey,
        [MarshalAs(UnmanagedType.I1)] bool keyDown);

    [DllImport(ApplicationServicesFramework)]
    private static extern void CGEventSetFlags(IntPtr keyboardEvent, ulong flags);

    [DllImport(ApplicationServicesFramework)]
    private static extern void CGEventPost(uint tap, IntPtr keyboardEvent);

    [DllImport(CoreFoundationFramework)]
    private static extern void CFRelease(IntPtr value);
}
