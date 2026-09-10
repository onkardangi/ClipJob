using System.Runtime.InteropServices;
using Avalonia.Input;

namespace ClipJob.Desktop;

public sealed class MacOSGlobalHotkeyService : IGlobalHotkeyService
{
    private const string CarbonFramework = "/System/Library/Frameworks/Carbon.framework/Carbon";
    private const uint CommandKey = 1 << 8;
    private const uint ShiftKey = 1 << 9;
    private const uint OptionKey = 1 << 11;
    private const uint ControlKey = 1 << 12;
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

    public void Register(GlobalShortcut shortcut, Action onPressed)
    {
        ArgumentNullException.ThrowIfNull(shortcut);
        ArgumentNullException.ThrowIfNull(onPressed);

        if (!shortcut.IsValid)
        {
            throw new InvalidOperationException("The shortcut must include Command and a letter key.");
        }

        Unregister();

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
            GetMacKeyCode(shortcut.Key),
            GetMacModifiers(shortcut.Modifiers),
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
                $"Could not register {shortcut.DisplayText}. Another application may already own it (OSStatus {status}).");
        }
    }

    private void Unregister()
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

    public void Dispose() => Unregister();

    private static uint GetMacModifiers(KeyModifiers modifiers)
    {
        var result = 0u;
        if (modifiers.HasFlag(KeyModifiers.Meta)) result |= CommandKey;
        if (modifiers.HasFlag(KeyModifiers.Shift)) result |= ShiftKey;
        if (modifiers.HasFlag(KeyModifiers.Alt)) result |= OptionKey;
        if (modifiers.HasFlag(KeyModifiers.Control)) result |= ControlKey;
        return result;
    }

    private static uint GetMacKeyCode(Key key) => key switch
    {
        Key.A => 0, Key.S => 1, Key.D => 2, Key.F => 3, Key.H => 4, Key.G => 5,
        Key.Z => 6, Key.X => 7, Key.C => 8, Key.V => 9, Key.B => 11, Key.Q => 12,
        Key.W => 13, Key.E => 14, Key.R => 15, Key.Y => 16, Key.T => 17, Key.O => 31,
        Key.U => 32, Key.I => 34, Key.P => 35, Key.L => 37, Key.J => 38, Key.K => 40,
        Key.N => 45, Key.M => 46,
        _ => throw new InvalidOperationException("The shortcut key is not supported.")
    };

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
