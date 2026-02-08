using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the Report Issue dialog.
/// </summary>
public class ReportIssueDialogViewModel : ViewModelBase
{
    #region Properties

    private string _subject = string.Empty;
    public string Subject
    {
        get => _subject;
        set
        {
            if (SetProperty(ref _subject, value))
            {
                OnPropertyChanged(nameof(CanSubmit));
            }
        }
    }

    private string _description = string.Empty;
    public string Description
    {
        get => _description;
        set
        {
            if (SetProperty(ref _description, value))
            {
                OnPropertyChanged(nameof(CanSubmit));
            }
        }
    }

    private bool _includeLogs = true;
    public bool IncludeLogs
    {
        get => _includeLogs;
        set => SetProperty(ref _includeLogs, value);
    }

    private bool _isSubmitting;
    public bool IsSubmitting
    {
        get => _isSubmitting;
        set
        {
            if (SetProperty(ref _isSubmitting, value))
            {
                OnPropertyChanged(nameof(CanSubmit));
            }
        }
    }

    private string _progressMessage = string.Empty;
    public string ProgressMessage
    {
        get => _progressMessage;
        set => SetProperty(ref _progressMessage, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    private IBrush _statusMessageColor = Brushes.Gray;
    public IBrush StatusMessageColor
    {
        get => _statusMessageColor;
        set => SetProperty(ref _statusMessageColor, value);
    }

    public bool CanSubmit => 
        !IsSubmitting && 
        !string.IsNullOrWhiteSpace(Subject) && 
        !string.IsNullOrWhiteSpace(Description);

    #endregion

    #region Commands

    public ICommand SubmitCommand { get; }
    public ICommand CancelCommand { get; }

    #endregion

    #region Events

    public event EventHandler<bool>? RequestClose;

    #endregion

    private CancellationTokenSource? _cts;

    public ReportIssueDialogViewModel()
    {
        SubmitCommand = new AsyncRelayCommand(SubmitAsync);
        CancelCommand = new RelayCommand(() =>
        {
            _cts?.Cancel();
            RequestClose?.Invoke(this, false);
        });
    }

    private async Task SubmitAsync()
    {
        if (!CanSubmit) return;

        IsSubmitting = true;
        StatusMessage = string.Empty;
        _cts = new CancellationTokenSource();

        try
        {
            var service = SupportBundleService.Instance;

            if (IncludeLogs)
            {
                ProgressMessage = "Creating log bundle...";
                var bundlePath = await service.CreateBundleAsync(_cts.Token);

                if (bundlePath == null)
                {
                    // Continue without bundle
                    ProgressMessage = "Sending report...";
                }
                else
                {
                    ProgressMessage = "Uploading logs...";
                    var bundleUrl = await service.UploadBundleAsync(bundlePath, _cts.Token);

                    ProgressMessage = "Sending report...";
                    var success = await service.SendSupportRequestAsync(
                        Subject, Description, bundleUrl, _cts.Token);

                    if (success)
                    {
                        StatusMessage = "Report submitted successfully! Thank you.";
                        StatusMessageColor = Brushes.Green;
                        await Task.Delay(1500); // Let user see success message
                        RequestClose?.Invoke(this, true);
                        return;
                    }
                }
            }
            else
            {
                ProgressMessage = "Sending report...";
                var success = await service.SendSupportRequestAsync(
                    Subject, Description, null, _cts.Token);

                if (success)
                {
                    StatusMessage = "Report submitted successfully! Thank you.";
                    StatusMessageColor = Brushes.Green;
                    await Task.Delay(1500);
                    RequestClose?.Invoke(this, true);
                    return;
                }
            }

            // If we get here, something failed
            StatusMessage = service.LastError ?? "Failed to submit report. Please try again.";
            StatusMessageColor = Brushes.Red;
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Submission cancelled.";
            StatusMessageColor = Brushes.Orange;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            StatusMessageColor = Brushes.Red;
        }
        finally
        {
            IsSubmitting = false;
            ProgressMessage = string.Empty;
        }
    }
}
