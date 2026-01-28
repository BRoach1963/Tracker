using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Notifications;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Handles Windows toast notification activations (button clicks).
/// This class processes the callback when users interact with reminder toast buttons.
/// </summary>
public static class ToastActivationHandler
{
    private static bool _initialized;

    /// <summary>
    /// Initializes the toast activation handler.
    /// Must be called once during app startup (before any toasts are shown).
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;
        
        // Only supported on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Debug.WriteLine("[ToastActivation] Not Windows, skipping initialization");
            return;
        }

        try
        {
            // Register the callback for toast activations
            ToastNotificationManagerCompat.OnActivated += OnToastActivated;
            _initialized = true;
            Debug.WriteLine("[ToastActivation] Toast activation handler initialized");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ToastActivation] Failed to initialize: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles toast activation events (button clicks or toast body click).
    /// </summary>
    private static void OnToastActivated(ToastNotificationActivatedEventArgsCompat e)
    {
        try
        {
            Debug.WriteLine($"[ToastActivation] Toast activated with arguments: {e.Argument}");

            // Parse the arguments
            var args = ToastArguments.Parse(e.Argument);

            if (!args.Contains("action"))
            {
                Debug.WriteLine("[ToastActivation] No action specified in arguments");
                return;
            }

            var action = args["action"];
            Debug.WriteLine($"[ToastActivation] Action: {action}");

            switch (action)
            {
                case "snooze":
                    HandleSnoozeAction(args);
                    break;
                    
                case "dismiss":
                    HandleDismissAction(args);
                    break;
                    
                case "reminderActivated":
                    // User clicked on the toast body itself - could open the app/entity
                    HandleReminderClicked(args);
                    break;
                    
                default:
                    Debug.WriteLine($"[ToastActivation] Unknown action: {action}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ToastActivation] Error handling activation: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the Snooze button click.
    /// </summary>
    private static void HandleSnoozeAction(ToastArguments args)
    {
        if (!args.Contains("reminderId"))
        {
            Debug.WriteLine("[ToastActivation] Snooze: No reminderId in arguments");
            return;
        }

        var reminderIdStr = args["reminderId"];
        if (!Guid.TryParse(reminderIdStr, out var reminderId))
        {
            Debug.WriteLine($"[ToastActivation] Snooze: Invalid reminderId: {reminderIdStr}");
            return;
        }

        // Get snooze minutes (default to 10)
        var snoozeMinutes = 10;
        if (args.Contains("snoozeMinutes"))
        {
            int.TryParse(args["snoozeMinutes"], out snoozeMinutes);
        }

        Debug.WriteLine($"[ToastActivation] Snoozing reminder {reminderId} for {snoozeMinutes} minutes");

        // Fire and forget - we're in a background thread
        _ = Task.Run(async () =>
        {
            try
            {
                await ReminderSchedulerService.Instance.SnoozeReminderAsync(reminderId, snoozeMinutes);
                
                // Remove the toast from action center
                NotificationService.Instance.RemoveReminderToast(reminderId);
                
                Debug.WriteLine($"[ToastActivation] Reminder snoozed successfully: {reminderId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ToastActivation] Failed to snooze reminder: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Handles the Dismiss button click.
    /// </summary>
    private static void HandleDismissAction(ToastArguments args)
    {
        if (!args.Contains("reminderId"))
        {
            Debug.WriteLine("[ToastActivation] Dismiss: No reminderId in arguments");
            return;
        }

        var reminderIdStr = args["reminderId"];
        if (!Guid.TryParse(reminderIdStr, out var reminderId))
        {
            Debug.WriteLine($"[ToastActivation] Dismiss: Invalid reminderId: {reminderIdStr}");
            return;
        }

        Debug.WriteLine($"[ToastActivation] Dismissing reminder {reminderId}");

        // Fire and forget - we're in a background thread
        _ = Task.Run(async () =>
        {
            try
            {
                await ReminderSchedulerService.Instance.DismissReminderAsync(reminderId);
                
                // Remove the toast from action center
                NotificationService.Instance.RemoveReminderToast(reminderId);
                
                Debug.WriteLine($"[ToastActivation] Reminder dismissed successfully: {reminderId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ToastActivation] Failed to dismiss reminder: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Handles clicking on the toast body (not a button).
    /// Could be used to open the app and navigate to the related entity.
    /// </summary>
    private static void HandleReminderClicked(ToastArguments args)
    {
        if (!args.Contains("reminderId"))
        {
            Debug.WriteLine("[ToastActivation] ReminderClicked: No reminderId in arguments");
            return;
        }

        var reminderIdStr = args["reminderId"];
        Debug.WriteLine($"[ToastActivation] Reminder clicked: {reminderIdStr}");

        // Future enhancement: Open the app and navigate to the related entity
        // For now, just log it - the app will be brought to foreground automatically
        // by the Windows toast system if the activation type isn't background
    }
}
