using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ClipJob.Desktop;

public sealed class MacOSForegroundApplicationService : IForegroundApplicationService
{
    private const string ObjectiveCLibrary = "/usr/lib/libobjc.A.dylib";
    private const nuint ActivateIgnoringOtherApps = 1 << 1;

    private readonly IntPtr _sharedWorkspace;
    private IntPtr _capturedApplication;

    public MacOSForegroundApplicationService()
    {
        var workspaceClass = objc_getClass("NSWorkspace");
        _sharedWorkspace = objc_msgSend(workspaceClass, sel_registerName("sharedWorkspace"));
    }

    public void CaptureCurrentApplication()
    {
        var application = objc_msgSend(
            _sharedWorkspace,
            sel_registerName("frontmostApplication"));

        if (application == IntPtr.Zero)
        {
            return;
        }

        var processIdentifier = objc_msgSend_nint(
            application,
            sel_registerName("processIdentifier"));

        if (processIdentifier == Environment.ProcessId)
        {
            return;
        }

        objc_retain(application);
        ReleaseCapturedApplication();
        _capturedApplication = application;
    }

    public void RestoreCapturedApplication()
    {
        if (_capturedApplication == IntPtr.Zero)
        {
            return;
        }

        var activated = objc_msgSend_bool_nuint(
            _capturedApplication,
            sel_registerName("activateWithOptions:"),
            ActivateIgnoringOtherApps);

        if (!activated)
        {
            Trace.TraceWarning("macOS did not activate the previously captured application.");
        }
    }

    public void Dispose() => ReleaseCapturedApplication();

    private void ReleaseCapturedApplication()
    {
        if (_capturedApplication == IntPtr.Zero)
        {
            return;
        }

        objc_release(_capturedApplication);
        _capturedApplication = IntPtr.Zero;
    }

    [DllImport(ObjectiveCLibrary)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjectiveCLibrary)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern nint objc_msgSend_nint(IntPtr receiver, IntPtr selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool objc_msgSend_bool_nuint(
        IntPtr receiver,
        IntPtr selector,
        nuint options);

    [DllImport(ObjectiveCLibrary)]
    private static extern IntPtr objc_retain(IntPtr value);

    [DllImport(ObjectiveCLibrary)]
    private static extern void objc_release(IntPtr value);
}
