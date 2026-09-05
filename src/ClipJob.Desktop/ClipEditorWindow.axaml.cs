using Avalonia.Controls;
using Avalonia.Input;

namespace ClipJob.Desktop;

public sealed record ClipEditorResult(string Label, string Content);

public sealed partial class ClipEditorWindow : Window
{
    public ClipEditorWindow()
        : this("Clip")
    {
    }

    public ClipEditorWindow(string title, string label = "", string content = "")
    {
        InitializeComponent();
        Title = title;
        Heading.Text = title;
        LabelTextBox.Text = label;
        ContentTextBox.Text = content;
        SaveButton.Content = string.IsNullOrEmpty(label) ? "Create clip" : "Save changes";
        Opened += (_, _) =>
        {
            if (string.IsNullOrEmpty(label))
            {
                LabelTextBox.Focus();
            }
            else
            {
                ContentTextBox.Focus();
                ContentTextBox.CaretIndex = ContentTextBox.Text?.Length ?? 0;
            }
        };
    }

    private void Cancel_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(null);

    private void Save_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Save();
    }

    private void Window_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close(null);
        }
        else if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Meta))
        {
            e.Handled = true;
            Save();
        }
    }

    private void Save()
    {
        var label = LabelTextBox.Text ?? string.Empty;
        var content = ContentTextBox.Text ?? string.Empty;
        LabelValidationMessage.Text = string.Empty;
        ContentValidationMessage.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(label))
        {
            LabelValidationMessage.Text = "Enter a label for this clip.";
            LabelTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            ContentValidationMessage.Text = "Enter the text this clip should paste.";
            ContentTextBox.Focus();
            return;
        }

        Close(new ClipEditorResult(label, content));
    }
}
