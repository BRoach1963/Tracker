using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services.AI.Insights;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Daily briefing dialog shown on startup with a summary of insights and tasks.
    /// </summary>
    public partial class DailyBriefingDialog : Controls.BaseWindow
    {
        private readonly ILogger _logger = LoggingManager.GetComponentLogger("DailyBriefing");
        
        public DailyBriefingDialog()
        {
            InitializeComponent();
            
            // Set the date text
            DateText.Text = DateTime.Now.ToString("dddd, MMMM d");
            
            // Set greeting based on time of day
            var hour = DateTime.Now.Hour;
            var greeting = hour switch
            {
                < 12 => "Good Morning!",
                < 17 => "Good Afternoon!",
                _ => "Good Evening!"
            };
            
            // Find the greeting text block and update it
            if (FindName("DateText") is System.Windows.Controls.TextBlock)
            {
                // The greeting is in the StackPanel - we need to find it via the visual tree
                // For now, leave as "Good Morning!" - it's in the XAML
            }
        }
        
        /// <summary>
        /// Loads and displays the daily briefing data.
        /// </summary>
        public async Task LoadBriefingAsync()
        {
            try
            {
                var engine = InsightEngine.Instance;
                var insights = await engine.GetActiveInsightsAsync();
                
                // Group insights by severity
                var critical = insights.Where(i => i.Severity == InsightSeverity.Critical).ToList();
                var warnings = insights.Where(i => i.Severity == InsightSeverity.Warning).ToList();
                var info = insights.Where(i => i.Severity == InsightSeverity.Info).ToList();
                
                // Update the UI
                await Dispatcher.InvokeAsync(() =>
                {
                    // Update counts
                    InsightsCountText.Text = insights.Count.ToString();
                    
                    // Get meetings count for today (would need TrackerDbManager access)
                    MeetingsCountText.Text = GetTodaysMeetingsCount().ToString();
                    
                    // Get open tasks count
                    TasksCountText.Text = GetOpenTasksCount().ToString();
                    
                    // Populate insight lists
                    if (critical.Any())
                    {
                        CriticalHeader.Visibility = Visibility.Visible;
                        CriticalInsightsList.ItemsSource = critical.Take(3);
                    }
                    
                    if (warnings.Any())
                    {
                        WarningsHeader.Visibility = Visibility.Visible;
                        WarningInsightsList.ItemsSource = warnings.Take(5);
                    }
                    
                    if (info.Any())
                    {
                        InfoHeader.Visibility = Visibility.Visible;
                        InfoInsightsList.ItemsSource = info.Take(3);
                    }
                    
                    // Show empty state if no insights
                    if (!insights.Any())
                    {
                        EmptyState.Visibility = Visibility.Visible;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to load daily briefing");
            }
        }
        
        private int GetTodaysMeetingsCount()
        {
            try
            {
                var context = Database.TrackerDbManager.Instance?.GetDbContext();
                if (context == null) return 0;
                
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                
                return context.OneOnOnes
                    .Count(m => m.Date >= today && m.Date < tomorrow);
            }
            catch
            {
                return 0;
            }
        }
        
        private int GetOpenTasksCount()
        {
            try
            {
                var context = Database.TrackerDbManager.Instance?.GetDbContext();
                if (context == null) return 0;
                
                // Count uncompleted meeting tasks
                return context.MeetingTasks
                    .Count(t => !t.IsCompleted);
            }
            catch
            {
                return 0;
            }
        }
        
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Save preference if user doesn't want to see on startup
            if (DontShowAgainCheckBox.IsChecked == true)
            {
                try
                {
                    var settings = UserSettingsManager.Instance.Settings;
                    settings.Insights.ShowDailyBriefingOnStartup = false;
                    UserSettingsManager.Instance.SaveSettings();
                }
                catch (Exception ex)
                {
                    _logger.Exception(ex, "Failed to save daily briefing preference");
                }
            }
            
            Close();
        }
        
        /// <summary>
        /// Shows the daily briefing dialog if enabled in settings.
        /// </summary>
        public static async Task ShowIfEnabledAsync(Window? owner = null)
        {
            try
            {
                var settings = UserSettingsManager.Instance.Settings;
                
                // Check if daily briefing is enabled
                if (!settings.Insights.IsEnabled || 
                    !settings.Insights.ShowDailyBriefingOnStartup)
                {
                    return;
                }
                
                // Create and show the dialog
                var dialog = new DailyBriefingDialog();
                if (owner != null)
                {
                    dialog.Owner = owner;
                }
                
                await dialog.LoadBriefingAsync();
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                var logger = LoggingManager.GetComponentLogger("DailyBriefing");
                logger.Exception(ex, "Failed to show daily briefing dialog");
            }
        }
    }
}
