using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ProCohere.Avalonia.ViewModels.Dialogs;

namespace ProCohere.Avalonia.Views.Controls.Meeting;

/// <summary>
/// Left panel containing meeting details form.
/// Minimal code-behind - just ComboBox selection handling that can't be done in XAML.
/// </summary>
public partial class MeetingDetailsPanel : UserControl
{
    public MeetingDetailsPanel()
    {
        InitializeComponent();
    }

    #region UI Event Handlers (ComboBox Selection)
    
    /// <summary>
    /// Handles meeting type selection change.
    /// ComboBox doesn't support direct enum binding well in Avalonia.
    /// </summary>
    private void MeetingTypeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not EditMeetingDialogViewModel vm) return;
        if (MeetingTypeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            vm.MeetingType = tag;
        }
    }

    /// <summary>
    /// Handles duration selection change.
    /// </summary>
    private void DurationComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not EditMeetingDialogViewModel vm) return;
        if (DurationComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            if (int.TryParse(tag, out var minutes))
            {
                vm.DurationMinutes = minutes;
            }
        }
    }
    
    #endregion

    #region Public Methods
    
    /// <summary>
    /// Sets the selected date/time. Called by parent dialog during loading.
    /// </summary>
    public void SetDateTime(DateTime? dateTime)
    {
        DateTimeSelector.SelectedDateTime = dateTime;
    }

    /// <summary>
    /// Gets the selected date/time.
    /// </summary>
    public DateTime? GetDateTime()
    {
        return DateTimeSelector.SelectedDateTime;
    }

    /// <summary>
    /// Sets meeting type selection by tag value.
    /// </summary>
    public void SetMeetingType(string meetingType)
    {
        for (int i = 0; i < MeetingTypeComboBox.Items.Count; i++)
        {
            if (MeetingTypeComboBox.Items[i] is ComboBoxItem item && 
                item.Tag?.ToString() == meetingType)
            {
                MeetingTypeComboBox.SelectedIndex = i;
                break;
            }
        }
    }

    /// <summary>
    /// Sets duration selection by minutes value.
    /// </summary>
    public void SetDuration(int minutes)
    {
        var tag = minutes.ToString();
        for (int i = 0; i < DurationComboBox.Items.Count; i++)
        {
            if (DurationComboBox.Items[i] is ComboBoxItem item && 
                item.Tag?.ToString() == tag)
            {
                DurationComboBox.SelectedIndex = i;
                break;
            }
        }
    }
    
    #endregion
}
