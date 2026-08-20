using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ClipJob.Desktop;

public sealed class MacOSForegroundApplicationService : IForegroundApplicationService
{
    private const string ObjectiveCLibrary = "/usr/lib/libobjc.A.dylib";
    private const nuint ActivateIgnoringOtherApps = 1 << 1;

    private readonly IntPtr _sharedWorkspace;
    private IntPtr _capturedApplication;
    private nint _capturedProcessIdentifier;

    public bool HasCapturedApplication => _capturedApplication != IntPtr.Zero;

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
        _capturedProcessIdentifier = processIdentifier;
    }

    public async Task<bool> RestoreCapturedApplicationAsync()
    {
        if (_capturedApplication == IntPtr.Zero)
        {
            Trace.TraceWarning("No external application was captured for paste-back.");
            return false;
        }

        var activated = objc_msgSend_bool_nuint(
            _capturedApplication,
            sel_registerName("activateWithOptions:"),
            ActivateIgnoringOtherApps);

        if (!activated)
        {
            Trace.TraceWarning("macOS did not activate the previously captured application.");
            return false;
        }

        // Activation completes asynchronously. Polling the frontmost process keeps the
        // wait bounded without guessing a delay that may paste into ClipJob.
        var timeout = TimeSpan.FromMilliseconds(750);
        var startedAt = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(startedAt) < timeout)
        {
            if (GetFrontmostProcessIdentifier() == _capturedProcessIdentifier)
            {
                return true;
            }

            await Task.Delay(20);
        }

        Trace.TraceWarning("The captured application did not become active before the paste timeout.");
        return false;
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
        _capturedProcessIdentifier = 0;
    }

    private nint GetFrontmostProcessIdentifier()
    {
        var application = objc_msgSend(
            _sharedWorkspace,
            sel_registerName("frontmostApplication"));

        return application == IntPtr.Zero
            ? 0
            : objc_msgSend_nint(application, sel_registerName("processIdentifier"));
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
