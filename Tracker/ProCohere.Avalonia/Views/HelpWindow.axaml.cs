using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ProCohere.Avalonia.Views;

/// <summary>
/// Window for displaying help content and search functionality.
/// </summary>
public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        InitializeComponent();
        
        // Set window properties
        Width = 800;
        Height = 600;
        Title = "ProCohere Help";
        Icon = this.FindResource("AppIcon") as WindowIcon;
        
        // Center window on screen
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}