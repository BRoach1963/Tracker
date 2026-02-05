using Avalonia.Controls;
using Avalonia.Interactivity;
using ProCohere.Avalonia.ViewModels;
using System;

namespace ProCohere.Avalonia.Dialogs;

public partial class SurveyAnalyticsDialog : Window
{
    private readonly SurveyAnalyticsViewModel _viewModel;

    // Parameterless constructor for XAML runtime loader
    public SurveyAnalyticsDialog() : this(Guid.Empty)
    {
    }

    public SurveyAnalyticsDialog(Guid surveyId)
    {
        InitializeComponent();

        _viewModel = new SurveyAnalyticsViewModel(surveyId);
        DataContext = _viewModel;

        // Load analytics on open
        Loaded += async (s, e) => await _viewModel.LoadAnalyticsCommand.ExecuteAsync(null);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
