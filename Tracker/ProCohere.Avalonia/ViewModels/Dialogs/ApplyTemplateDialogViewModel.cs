using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the ApplyTemplateDialog.
/// </summary>
public partial class ApplyTemplateDialogViewModel : ObservableObject
{
    private MeetingDetail? _meeting;
    private List<MeetingTemplateDetail> _allTemplates = new();
    
    /// <summary>
    /// Result of the dialog - the selected template ID if applied, null if cancelled.
    /// </summary>
    public ApplyTemplateResult? Result { get; private set; }
    
    /// <summary>
    /// Raised when the dialog should close.
    /// </summary>
    public event Action? CloseRequested;
    
    #region Observable Properties
    
    [ObservableProperty]
    private string _meetingTitleText = "Select a template to add agenda items";
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private bool _hasTemplates;
    
    [ObservableProperty]
    private ObservableCollection<MeetingTemplateDetail> _filteredTemplates = new();
    
    [ObservableProperty]
    private MeetingTemplateDetail? _selectedTemplate;
    
    [ObservableProperty]
    private int _categoryFilterIndex;
    
    [ObservableProperty]
    private string _selectedTemplateText = string.Empty;
    
    [ObservableProperty]
    private bool _isApplyEnabled;
    
    [ObservableProperty]
    private bool _isApplying;
    
    [ObservableProperty]
    private string _applyButtonText = "Apply Template";
    
    #endregion
    
    // Category filter tags matching XAML order: "" (0), "one_on_one" (1), "team" (2), "project" (3), "custom" (4)
    private static readonly string?[] CategoryTags = { null, "one_on_one", "team", "project", "custom" };
    
    public ApplyTemplateDialogViewModel()
    {
    }
    
    /// <summary>
    /// Sets the meeting context for this dialog.
    /// </summary>
    public void SetMeeting(MeetingDetail meeting)
    {
        _meeting = meeting;
        MeetingTitleText = $"Add agenda items to: {meeting.Title}";
    }
    
    /// <summary>
    /// Loads and displays templates.
    /// </summary>
    public async Task LoadTemplatesAsync()
    {
        IsLoading = true;
        HasTemplates = false;

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
            Debug.WriteLine($"Error loading templates: {ex.Message}");
            _allTemplates = new List<MeetingTemplateDetail>();
            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    partial void OnCategoryFilterIndexChanged(int value)
    {
        ApplyFilter();
    }
    
    private void ApplyFilter()
    {
        var categoryFilter = CategoryFilterIndex >= 0 && CategoryFilterIndex < CategoryTags.Length 
            ? CategoryTags[CategoryFilterIndex] 
            : null;
            
        var filtered = string.IsNullOrEmpty(categoryFilter)
            ? _allTemplates
            : _allTemplates.Where(t => t.MeetingType == categoryFilter).ToList();

        FilteredTemplates = new ObservableCollection<MeetingTemplateDetail>(filtered);
        HasTemplates = filtered.Count > 0;
    }
    
    [RelayCommand]
    private void SelectTemplate(MeetingTemplateDetail? template)
    {
        if (template == null) return;
        
        SelectedTemplate = template;
        SelectedTemplateText = $"Selected: {template.Name} ({template.ItemCountDisplay})";
        IsApplyEnabled = true;
    }
    
    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        CloseRequested?.Invoke();
    }
    
    [RelayCommand]
    private async Task ApplyAsync()
    {
        if (SelectedTemplate == null || _meeting == null)
        {
            return;
        }

        IsApplyEnabled = false;
        IsApplying = true;
        ApplyButtonText = "Applying...";

        try
        {
            var service = MeetingTemplateService.Instance;
            var success = await service.ApplyTemplateToMeetingAsync(SelectedTemplate.Id, _meeting.Id);

            if (success)
            {
                Result = new ApplyTemplateResult
                {
                    TemplateId = SelectedTemplate.Id,
                    TemplateName = SelectedTemplate.Name,
                    ItemsAdded = SelectedTemplate.Items.Count
                };
                CloseRequested?.Invoke();
            }
            else
            {
                // Show error
                SelectedTemplateText = $"Error: {service.LastError}";
                IsApplyEnabled = true;
                IsApplying = false;
                ApplyButtonText = "Apply Template";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error applying template: {ex.Message}");
            SelectedTemplateText = $"Error: {ex.Message}";
            IsApplyEnabled = true;
            IsApplying = false;
            ApplyButtonText = "Apply Template";
        }
    }
}
