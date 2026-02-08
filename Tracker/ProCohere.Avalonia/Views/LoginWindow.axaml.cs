using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ProCohere.Avalonia.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Allow dragging the window from anywhere (except interactive controls)
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void Help_Click(object? sender, RoutedEventArgs e)
    {
        OpenUrl("https://procohere.com/help");
    }

    private void Privacy_Click(object? sender, RoutedEventArgs e)
    {
        OpenUrl("https://procohere.com/privacy");
    }

    private void Terms_Click(object? sender, RoutedEventArgs e)
    {
        OpenUrl("https://procohere.com/terms");
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Silently fail if browser can't be opened
        }
    }
}
