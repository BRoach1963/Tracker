using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.ViewModels;
using ProCohere.Avalonia.Views.Dialogs;

namespace ProCohere.Avalonia.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        
        // Wire up the file picker when DataContext is set
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.OpenFilePickerFunc = OpenAvatarFilePickerAsync;
            vm.OpenFullProfileEditorRequested += OnOpenFullProfileEditorRequested;
        }
    }

    private async void OnOpenFullProfileEditorRequested()
    {
        try
        {
            // Load current user profile
            var profile = await AuthService.Instance.LoadUserProfileAsync();
            if (profile == null) return;

            // Create the dialog (non-modal, draggable window)
            var dialog = new EditAccountDialog();
            dialog.LoadProfile(profile);
            
            // Subscribe to save event to refresh UI
            dialog.ProfileSaved += async () =>
            {
                if (DataContext is SettingsViewModel vm)
                {
                    await vm.LoadUserProfileAsync();
                }
            };
            
            // Show as non-modal window (can be dragged, doesn't block)
            dialog.Show();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing edit profile dialog: {ex.Message}");
        }
    }

    private async Task<string?> OpenAvatarFilePickerAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Avatar Image",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("Image Files")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.webp" },
                    MimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" }
                }
            }
        });

        if (files.Count >= 1)
        {
            // Get the local file path
            var file = files[0];
            return file.TryGetLocalPath();
        }

        return null;
    }
}
