using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for selecting and applying a meeting template to a meeting.
/// </summary>
public partial class ApplyTemplateDialog : Window
{
    private MeetingDetail? _meeting;
    private MeetingTemplateDetail? _selectedTemplate;
    private List<MeetingTemplateDetail> _allTemplates = new();
    private string? _currentCategoryFilter;

    /// <summary>
    /// Result of the dialog - the selected template ID if applied, null if cancelled.
    /// </summary>
    public ApplyTemplateResult? Result { get; private set; }

    public ApplyTemplateDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Sets the meeting context for this dialog.
    /// </summary>
    public void SetMeeting(MeetingDetail meeting)
    {
        _meeting = meeting;
        MeetingTitleText.Text = $"Add agenda items to: {meeting.Title}";
    }

    /// <summary>
    /// Loads and displays templates.
    /// </summary>
    public async Task LoadTemplatesAsync()
    {
        LoadingPanel.IsVisible = true;
        TemplatesItemsControl.IsVisible = false;
        EmptyStatePanel.IsVisible = false;

        try
        {
            var service = MeetingTemplateService.Instance;
            
            // Ensure default templates exist
            await service.EnsureDefaultTemplatesAsync();
            
            // Load all templates
            _allTemplates = await service.GetTemplatesAsync();
            
            ApplyFilter();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading templates: {ex.Message}");
            _allTemplates = new List<MeetingTemplateDetail>();
            ApplyFilter();
        }
        finally
        {
            LoadingPanel.IsVisible = false;
        }
    }

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrEmpty(_currentCategoryFilter)
            ? _allTemplates
            : _allTemplates.Where(t => t.MeetingType == _currentCategoryFilter).ToList();

        TemplatesItemsControl.ItemsSource = filtered;
        TemplatesItemsControl.IsVisible = filtered.Count > 0;
        EmptyStatePanel.IsVisible = filtered.Count == 0;
    }

    private void CategoryFilterComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (CategoryFilterComboBox.SelectedItem is ComboBoxItem item)
        {
            _currentCategoryFilter = item.Tag?.ToString();
            ApplyFilter();
        }
    }

    private void TemplateCard_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is MeetingTemplateDetail template)
        {
            SelectTemplate(template);
        }
    }

    private void SelectTemplate(MeetingTemplateDetail template)
    {
        _selectedTemplate = template;
        
        // Update UI
        SelectedTemplateText.Text = $"Selected: {template.Name} ({template.ItemCountDisplay})";
        ApplyButton.IsEnabled = true;
        
        // Update visual selection (toggle 'selected' class)
        // In Avalonia, we can update via data binding or code-behind
        // For simplicity, we update the enabled state
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }

    private async void ApplyButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedTemplate == null || _meeting == null)
        {
            return;
        }

        ApplyButton.IsEnabled = false;
        ApplyButton.Content = "Applying...";

        try
        {
            var service = MeetingTemplateService.Instance;
            var success = await service.ApplyTemplateToMeetingAsync(_selectedTemplate.Id, _meeting.Id);

            if (success)
            {
                Result = new ApplyTemplateResult
                {
                    TemplateId = _selectedTemplate.Id,
                    TemplateName = _selectedTemplate.Name,
                    ItemsAdded = _selectedTemplate.Items.Count
                };
                Close();
            }
            else
            {
                // Show error
                SelectedTemplateText.Text = $"Error: {service.LastError}";
                ApplyButton.IsEnabled = true;
                ApplyButton.Content = "Apply Template";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error applying template: {ex.Message}");
            SelectedTemplateText.Text = $"Error: {ex.Message}";
            ApplyButton.IsEnabled = true;
            ApplyButton.Content = "Apply Template";
        }
    }
}

/// <summary>
/// Result data from the ApplyTemplateDialog.
/// </summary>
public class ApplyTemplateResult
{
    public required Guid TemplateId { get; init; }
    public required string TemplateName { get; init; }
    public required int ItemsAdded { get; init; }
}
