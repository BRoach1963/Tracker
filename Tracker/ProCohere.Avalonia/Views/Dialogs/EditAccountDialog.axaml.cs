using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.ViewModels.Dialogs;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Window for editing user account/profile information.
/// Minimal code-behind - all business logic in ViewModel.
/// </summary>
public partial class EditAccountDialog : Window
{
    private readonly EditAccountDialogViewModel _viewModel;

    /// <summary>
    /// Result of the dialog - true if saved successfully, false if cancelled.
    /// </summary>
    public bool Result => _viewModel.Result;

    /// <summary>
    /// Event raised when profile is saved successfully.
    /// </summary>
    public event Action? ProfileSaved;

    public EditAccountDialog()
    {
        InitializeComponent();

        _viewModel = new EditAccountDialogViewModel();
        DataContext = _viewModel;

        _viewModel.CloseRequested += OnCloseRequested;
        _viewModel.ProfileSaved += () => ProfileSaved?.Invoke();
        _viewModel.AvatarPickerRequested += OnAvatarPickerRequested;
    }

    /// <summary>
    /// Loads the current user profile into the dialog.
    /// </summary>
    public void LoadProfile(UserProfile profile)
    {
        _viewModel.LoadProfile(profile);
    }

    private void OnCloseRequested(object? sender, bool result)
    {
        Close();
    }

    /// <summary>
    /// Handle header drag to move window.
    /// </summary>
    private void Header_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    /// <summary>
    /// Handle avatar picker request from ViewModel.
    /// </summary>
    private async void OnAvatarPickerRequested(object? sender, EventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Profile Photo",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Images")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.webp" },
                    MimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" }
                }
            }
        });

        if (files.Count > 0)
        {
            var file = files[0];
            _viewModel.SetPendingAvatarPath(file.Path.LocalPath);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.CloseRequested -= OnCloseRequested;
        _viewModel.AvatarPickerRequested -= OnAvatarPickerRequested;
        base.OnClosed(e);
    }
}
