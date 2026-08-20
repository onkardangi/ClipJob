using System.Collections.Generic;
using Avalonia.Controls;

namespace ClipJob.Desktop;

public sealed partial class MainWindow : Window
{
    public IReadOnlyList<Clip> Clips { get; } =
    [
        new("email", "test@example.com"),
        new("linkedin", "https://linkedin.com/in/test"),
        new("experience", "Built high-throughput REST APIs...")
    ];

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }
}
