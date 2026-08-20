using System.Runtime.InteropServices;

namespace ClipJob.Desktop;

public sealed class MacOSGlobalHotkeyService : IGlobalHotkeyService
{
    private const string CarbonFramework = "/System/Library/Frameworks/Carbon.framework/Carbon";
    private const uint CommandKey = 1 << 8;
    private const uint ShiftKey = 1 << 9;
    private const uint VKeyCode = 9;
    private const uint KeyboardEventClass = 0x6B657962; // 'keyb'
    private const uint HotKeyPressedEventKind = 6;

    private readonly EventHandlerDelegate _eventHandler;
    private IntPtr _eventHandlerReference;
    private IntPtr _hotKeyReference;
    private Action? _onPressed;

    public MacOSGlobalHotkeyService()
    {
        _eventHandler = HandleHotKey;
    }

    public void Register(Action onPressed)
    {
        ArgumentNullException.ThrowIfNull(onPressed);

        if (_hotKeyReference != IntPtr.Zero)
        {
            throw new InvalidOperationException("The global hotkey is already registered.");
        }

        _onPressed = onPressed;

        var eventType = new EventTypeSpec(KeyboardEventClass, HotKeyPressedEventKind);
        var status = InstallEventHandler(
            GetApplicationEventTarget(),
            _eventHandler,
            1,
            [eventType],
            IntPtr.Zero,
            out _eventHandlerReference);

        if (status != 0)
        {
            _onPressed = null;
            throw new InvalidOperationException(
                $"Could not install the macOS global hotkey event handler (OSStatus {status}).");
        }

        var hotKeyId = new EventHotKeyId(0x434A4F42, 1); // 'CJOB'
        status = RegisterEventHotKey(
            VKeyCode,
            CommandKey | ShiftKey,
            hotKeyId,
            GetApplicationEventTarget(),
            0,
            out _hotKeyReference);

        if (status != 0)
        {
            RemoveEventHandler(_eventHandlerReference);
            _eventHandlerReference = IntPtr.Zero;
            _onPressed = null;
            throw new InvalidOperationException(
                $"Could not register Command+Shift+V. Another application may already own it (OSStatus {status}).");
        }
    }

    public void Dispose()
    {
        if (_hotKeyReference != IntPtr.Zero)
        {
            UnregisterEventHotKey(_hotKeyReference);
            _hotKeyReference = IntPtr.Zero;
        }

        if (_eventHandlerReference != IntPtr.Zero)
        {
            RemoveEventHandler(_eventHandlerReference);
            _eventHandlerReference = IntPtr.Zero;
        }

        _onPressed = null;
    }

    private int HandleHotKey(IntPtr nextHandler, IntPtr eventReference, IntPtr userData)
    {
        _onPressed?.Invoke();
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct EventTypeSpec(uint eventClass, uint eventKind)
    {
        public readonly uint EventClass = eventClass;
        public readonly uint EventKind = eventKind;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct EventHotKeyId(uint signature, uint id)
    {
        public readonly uint Signature = signature;
        public readonly uint Id = id;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EventHandlerDelegate(IntPtr nextHandler, IntPtr eventReference, IntPtr userData);

    [DllImport(CarbonFramework)]
    private static extern IntPtr GetApplicationEventTarget();

    [DllImport(CarbonFramework)]
    private static extern int InstallEventHandler(
        IntPtr target,
        EventHandlerDelegate handler,
        uint eventTypeCount,
        EventTypeSpec[] eventTypes,
        IntPtr userData,
        out IntPtr eventHandlerReference);

    [DllImport(CarbonFramework)]
    private static extern int RemoveEventHandler(IntPtr eventHandlerReference);

    [DllImport(CarbonFramework)]
    private static extern int RegisterEventHotKey(
        uint keyCode,
        uint modifiers,
        EventHotKeyId hotKeyId,
        IntPtr target,
        uint options,
        out IntPtr hotKeyReference);

    [DllImport(CarbonFramework)]
    private static extern int UnregisterEventHotKey(IntPtr hotKeyReference);
}
