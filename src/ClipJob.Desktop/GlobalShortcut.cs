using Avalonia.Input;

namespace ClipJob.Desktop;

public sealed record GlobalShortcut(Key Key, KeyModifiers Modifiers)
{
    public static GlobalShortcut Default { get; } = new(Key.V, KeyModifiers.Meta | KeyModifiers.Shift);

    public bool IsValid => Key is >= Key.A and <= Key.Z &&
                           Modifiers.HasFlag(KeyModifiers.Meta);

    public string DisplayText
    {
        get
        {
            var text = string.Empty;
            if (Modifiers.HasFlag(KeyModifiers.Control)) text += "⌃";
            if (Modifiers.HasFlag(KeyModifiers.Alt)) text += "⌥";
            if (Modifiers.HasFlag(KeyModifiers.Shift)) text += "⇧";
            if (Modifiers.HasFlag(KeyModifiers.Meta)) text += "⌘";
            return text + Key;
        }
    }
}
