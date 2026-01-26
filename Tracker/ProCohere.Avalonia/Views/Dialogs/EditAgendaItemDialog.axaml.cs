using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.ViewModels.Dialogs;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for editing agenda item details including context, talking points, and visibility.
/// </summary>
public partial class EditAgendaItemDialog : Window
{
    private readonly EditAgendaItemDialogViewModel _viewModel;
    
    /// <summary>
    /// The result of the dialog (null if cancelled).
    /// </summary>
    public EditAgendaItemResult? Result => _viewModel.Result;
    
    public EditAgendaItemDialog() : this(new DialogAgendaItem())
    {
    }
    
    public EditAgendaItemDialog(DialogAgendaItem item)
    {
        InitializeComponent();
        _viewModel = new EditAgendaItemDialogViewModel(item);
        DataContext = _viewModel;
        SetupViewModel();
    }
    
    private void SetupViewModel()
    {
        _viewModel.CloseRequested += () => Close();
        _viewModel.EditTalkingPointRequested += ShowEditTalkingPointDialog;
    }
    
    private void NewTalkingPointTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.TryAddFromTextBox();
        }
        else if (e.Key == Key.Escape)
        {
            _viewModel.CancelAddCommand.Execute(null);
        }
    }
    
    private async void ShowEditTalkingPointDialog(TalkingPoint tp)
    {
        var dialog = new Window
        {
            Title = "Edit Talking Point",
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };
        
        var textBox = new TextBox
        {
            Text = tp.Text,
            Margin = new Thickness(20, 20, 20, 10),
            AcceptsReturn = false
        };
        
        var okButton = new Button { Content = "OK", IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
        var cancelButton = new Button { Content = "Cancel", IsCancel = true };
        
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(20, 0, 20, 20)
        };
        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        
        var mainPanel = new StackPanel();
        mainPanel.Children.Add(textBox);
        mainPanel.Children.Add(buttonPanel);
        
        dialog.Content = mainPanel;
        
        var tcs = new TaskCompletionSource<bool>();
        okButton.Click += (s, args) => { tcs.TrySetResult(true); dialog.Close(); };
        cancelButton.Click += (s, args) => { tcs.TrySetResult(false); dialog.Close(); };
        dialog.Closed += (s, args) => tcs.TrySetResult(false);
        
        await dialog.ShowDialog(this);
        
        if (await tcs.Task)
        {
            var newText = textBox.Text?.Trim();
            if (!string.IsNullOrEmpty(newText))
            {
                _viewModel.UpdateTalkingPointText(tp, newText);
            }
        }
    }
}
