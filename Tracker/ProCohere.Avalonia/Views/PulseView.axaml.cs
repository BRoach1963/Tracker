using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using ProCohere.Avalonia.ViewModels;

namespace ProCohere.Avalonia.Views;

/// <summary>
/// Code-behind for PulseView - the synthesis feed with quick access strip.
/// 
/// MVVM: 
/// - ViewModel is provided by MainWindowViewModel, not created here.
/// - Navigation is handled by ViewModel events (SignalNavigationRequested).
/// - View only handles data loading trigger on DataContext change.
/// </summary>
public partial class PulseView : UserControl
{
    private PulseViewModel? _viewModel;
    private bool _isInitialized;

    public PulseView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Log("[PulseView] Constructor");
    }
    
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _viewModel = DataContext as PulseViewModel;
        
        // Load data on first initialization
        if (_viewModel != null && !_isInitialized)
        {
            _isInitialized = true;
            Log("[PulseView] ViewModel bound, loading data");
            _ = _viewModel.RefreshDataAsync();
        }
    }

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "pulse_view.log");

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        Debug.WriteLine(line);
        try
        {
            var dir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
        catch { }
    }

    #endregion
}
