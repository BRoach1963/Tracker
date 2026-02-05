using System.Threading.Tasks;
using Avalonia.Controls;
using ProCohere.Avalonia.Interfaces;
using ProCohere.Avalonia.ViewModels;
using ProCohere.Avalonia.Views;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Factory for creating help windows without MVVM violations.
/// Enables testable view creation and proper separation of concerns.
/// </summary>
public class HelpWindowFactory : IHelpWindowFactory
{
    private readonly Window? _parentWindow;
    
    public HelpWindowFactory(Window? parentWindow = null)
    {
        _parentWindow = parentWindow;
    }
    
    public async Task ShowHelpWindowAsync(string? initialTopicId = null)
    {
        var helpWindow = new HelpWindow
        {
            DataContext = new HelpWindowViewModel(initialTopicId ?? "overview")
        };
        
        if (_parentWindow != null)
        {
            await helpWindow.ShowDialog(_parentWindow);
        }
        else
        {
            helpWindow.Show();
        }
    }
}