using Avalonia.Controls;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Result from the edit meeting dialog.
/// </summary>
public class EditMeetingResult
{
    public bool IsDeleted { get; set; }
    public Guid? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string MeetingType { get; set; } = "one_on_one";
    public DateTime? ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public string? Location { get; set; }
    public string? VideoLink { get; set; }
    public string? Notes { get; set; }
    public Guid? TeamMemberId { get; set; }
}

/// <summary>
/// Dialog for creating or editing meetings.
/// </summary>
public partial class EditMeetingDialog : Window
{
    private MeetingDetail? _existingMeeting;
    private List<TeamMemberDetail> _teamMembers = new();
    
    /// <summary>
    /// The result of the dialog (null if cancelled).
    /// </summary>
    public EditMeetingResult? Result { get; private set; }
    
    public EditMeetingDialog()
    {
        InitializeComponent();
        
        // Set default date/time to next hour
        var now = DateTime.Now;
        var nextHour = now.AddHours(1).Date.AddHours(now.Hour + 1);
        DatePicker.SelectedDate = DateTimeOffset.Now;
        TimePicker.SelectedTime = new TimeSpan(nextHour.Hour, 0, 0);
        
        // Wire up meeting type change to show/hide attendee
        MeetingTypeComboBox.SelectionChanged += MeetingTypeComboBox_SelectionChanged;
    }
    
    /// <summary>
    /// Load an existing meeting for editing.
    /// </summary>
    public void LoadMeeting(MeetingDetail meeting)
    {
        _existingMeeting = meeting;
        
        DialogTitle.Text = "Edit Meeting";
        DeleteButton.IsVisible = true;
        
        TitleTextBox.Text = meeting.Title;
        
        // Set meeting type
        for (int i = 0; i < MeetingTypeComboBox.Items.Count; i++)
        {
            var item = MeetingTypeComboBox.Items[i] as ComboBoxItem;
            if (item?.Tag?.ToString() == meeting.MeetingType)
            {
                MeetingTypeComboBox.SelectedIndex = i;
                break;
            }
        }
        
        // Set date/time
        if (meeting.ScheduledAt.HasValue)
        {
            DatePicker.SelectedDate = new DateTimeOffset(meeting.ScheduledAt.Value);
            TimePicker.SelectedTime = meeting.ScheduledAt.Value.TimeOfDay;
        }
        
        // Set duration
        var durationTag = meeting.DurationMinutes?.ToString() ?? "30";
        for (int i = 0; i < DurationComboBox.Items.Count; i++)
        {
            var item = DurationComboBox.Items[i] as ComboBoxItem;
            if (item?.Tag?.ToString() == durationTag)
            {
                DurationComboBox.SelectedIndex = i;
                break;
            }
        }
        
        LocationTextBox.Text = meeting.Location ?? "";
        VideoLinkTextBox.Text = meeting.VideoLink ?? "";
        NotesTextBox.Text = meeting.Notes ?? "";
        
        // Set attendee if we have team members loaded
        if (meeting.TeamMemberId.HasValue)
        {
            var attendee = _teamMembers.FirstOrDefault(t => t.Id == meeting.TeamMemberId.Value);
            if (attendee != null)
            {
                AttendeeComboBox.SelectedItem = attendee;
            }
        }
        
        UpdateAttendeeVisibility();
    }
    
    /// <summary>
    /// Set the list of team members for the attendee dropdown.
    /// </summary>
    public void SetTeamMembers(IEnumerable<TeamMemberDetail> teamMembers)
    {
        _teamMembers = teamMembers.ToList();
        AttendeeComboBox.ItemsSource = _teamMembers;
        
        // If editing and we have a team member id, select it
        if (_existingMeeting?.TeamMemberId.HasValue == true)
        {
            var attendee = _teamMembers.FirstOrDefault(t => t.Id == _existingMeeting.TeamMemberId.Value);
            if (attendee != null)
            {
                AttendeeComboBox.SelectedItem = attendee;
            }
        }
    }
    
    private void MeetingTypeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateAttendeeVisibility();
    }
    
    private void UpdateAttendeeVisibility()
    {
        var selectedItem = MeetingTypeComboBox.SelectedItem as ComboBoxItem;
        var meetingType = selectedItem?.Tag?.ToString() ?? "one_on_one";
        
        // Show attendee selector for 1:1 and performance meetings
        AttendeeSection.IsVisible = meetingType == "one_on_one" || meetingType == "performance";
    }
    
    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }
    
    private async void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        // Validate
        var title = TitleTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            // TODO: Show validation error
            TitleTextBox.Focus();
            return;
        }
        
        if (!DatePicker.SelectedDate.HasValue)
        {
            DatePicker.Focus();
            return;
        }
        
        if (!TimePicker.SelectedTime.HasValue)
        {
            TimePicker.Focus();
            return;
        }
        
        // Get meeting type
        var typeItem = MeetingTypeComboBox.SelectedItem as ComboBoxItem;
        var meetingType = typeItem?.Tag?.ToString() ?? "one_on_one";
        
        // Get duration
        var durationItem = DurationComboBox.SelectedItem as ComboBoxItem;
        var duration = int.Parse(durationItem?.Tag?.ToString() ?? "30");
        
        // Combine date and time
        var date = DatePicker.SelectedDate.Value.Date;
        var time = TimePicker.SelectedTime.Value;
        var scheduledAt = date.Add(time);
        
        // Get attendee
        Guid? teamMemberId = null;
        if (AttendeeSection.IsVisible && AttendeeComboBox.SelectedItem is TeamMemberDetail member)
        {
            teamMemberId = member.Id;
        }
        
        Result = new EditMeetingResult
        {
            Id = _existingMeeting?.Id,
            Title = title,
            MeetingType = meetingType,
            ScheduledAt = scheduledAt,
            DurationMinutes = duration,
            Location = string.IsNullOrWhiteSpace(LocationTextBox.Text) ? null : LocationTextBox.Text.Trim(),
            VideoLink = string.IsNullOrWhiteSpace(VideoLinkTextBox.Text) ? null : VideoLinkTextBox.Text.Trim(),
            Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim(),
            TeamMemberId = teamMemberId,
            IsDeleted = false
        };
        
        Debug.WriteLine($"[EditMeetingDialog] Saving meeting: {title} on {scheduledAt}");
        Close();
    }
    
    private async void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        // TODO: Show confirmation dialog
        Result = new EditMeetingResult
        {
            Id = _existingMeeting?.Id,
            IsDeleted = true
        };
        
        Debug.WriteLine($"[EditMeetingDialog] Deleting meeting: {_existingMeeting?.Id}");
        Close();
    }
}
