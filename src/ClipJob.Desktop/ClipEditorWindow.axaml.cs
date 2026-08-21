using Avalonia.Controls;

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
        Opened += (_, _) => LabelTextBox.Focus();
    }

    private void Cancel_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(null);

    private void Save_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var label = LabelTextBox.Text ?? string.Empty;
        var content = ContentTextBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(label))
        {
            ValidationMessage.Text = "Label is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            ValidationMessage.Text = "Content is required.";
            return;
        }

        Close(new ClipEditorResult(label, content));
    }
}
