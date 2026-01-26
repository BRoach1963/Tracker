using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.ViewModels.Dialogs;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for selecting and applying a meeting template to a meeting.
/// </summary>
public partial class ApplyTemplateDialog : Window
{
    private readonly ApplyTemplateDialogViewModel _viewModel;

    /// <summary>
    /// Result of the dialog - the selected template ID if applied, null if cancelled.
    /// </summary>
    public ApplyTemplateResult? Result => _viewModel.Result;

    public ApplyTemplateDialog()
    {
        InitializeComponent();
        _viewModel = new ApplyTemplateDialogViewModel();
        DataContext = _viewModel;
        _viewModel.CloseRequested += () => Close();
    }

    /// <summary>
    /// Sets the meeting context for this dialog.
    /// </summary>
    public void SetMeeting(MeetingDetail meeting)
    {
        _viewModel.SetMeeting(meeting);
    }

    /// <summary>
    /// Loads and displays templates.
    /// </summary>
    public Task LoadTemplatesAsync()
    {
        return _viewModel.LoadTemplatesAsync();
    }
    
    private void TemplateCard_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is MeetingTemplateDetail template)
        {
            _viewModel.SelectTemplateCommand.Execute(template);
        }
    }
}
