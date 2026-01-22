using System;
using Avalonia.Media;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Represents the "load" for a single day - count of tasks and meetings due.
/// Used for the Briefing sparkline visualization.
/// </summary>
public class DailyLoad
{
    /// <summary>
    /// The date this load represents.
    /// </summary>
    public DateTime Date { get; set; }
    
    /// <summary>
    /// Day of week abbreviation (Mon, Tue, etc.)
    /// </summary>
    public string DayLabel => Date.ToString("ddd");
    
    /// <summary>
    /// Day number (1-31)
    /// </summary>
    public int DayNumber => Date.Day;
    
    /// <summary>
    /// Number of tasks due on this day.
    /// </summary>
    public int TasksDue { get; set; }
    
    /// <summary>
    /// Number of meetings scheduled on this day.
    /// </summary>
    public int Meetings { get; set; }
    
    /// <summary>
    /// Total load (tasks + meetings).
    /// </summary>
    public int TotalLoad => TasksDue + Meetings;
    
    /// <summary>
    /// Whether this is today.
    /// </summary>
    public bool IsToday => Date.Date == DateTime.Today;
    
    /// <summary>
    /// Tooltip text for the sparkline point.
    /// </summary>
    public string Tooltip => $"{Date:ddd, MMM d}\n{TasksDue} task{(TasksDue == 1 ? "" : "s")}, {Meetings} meeting{(Meetings == 1 ? "" : "s")}";
    
    /// <summary>
    /// Maximum load in the week (set by ViewModel after loading data).
    /// Used to scale bar heights.
    /// </summary>
    public int MaxLoad { get; set; }
    
    /// <summary>
    /// Computed bar height (4-60px range based on load ratio).
    /// </summary>
    public double BarHeight
    {
        get
        {
            if (MaxLoad <= 0 || TotalLoad <= 0) return 4.0;
            var ratio = (double)TotalLoad / MaxLoad;
            return 4.0 + (ratio * 56.0); // 4 min, 60 max
        }
    }
    
    /// <summary>
    /// Computed bar color based on load intensity and whether it's today.
    /// Colors are desaturated and cool to avoid implying urgency or judgment.
    /// </summary>
    public IBrush BarColor
    {
        get
        {
            // Desaturated, cool color palette - no warm colors (no orange/red)
            // These colors "speak quietly" - managers read the pattern, not the alarm
            var lightBrush = new SolidColorBrush(Color.Parse("#94A3B8"));   // Slate-400 (cool gray-blue)
            var mediumBrush = new SolidColorBrush(Color.Parse("#64748B"));  // Slate-500 (medium gray-blue)
            var heavyBrush = new SolidColorBrush(Color.Parse("#475569"));   // Slate-600 (darker gray-blue)
            var todayBrush = new SolidColorBrush(Color.Parse("#6366F1"));   // Indigo-500 (accent, but muted)
            var emptyBrush = new SolidColorBrush(Color.Parse("#E2E8F0"));   // Slate-200 (very light)
            
            if (TotalLoad <= 0) return emptyBrush;
            if (IsToday) return todayBrush;
            if (MaxLoad <= 0) return lightBrush;
            
            var ratio = (double)TotalLoad / MaxLoad;
            if (ratio < 0.33) return lightBrush;
            if (ratio < 0.66) return mediumBrush;
            return heavyBrush;
        }
    }
    
    /// <summary>
    /// FontWeight for the day label (bold if today).
    /// </summary>
    public FontWeight DayLabelWeight => IsToday ? FontWeight.Bold : FontWeight.Normal;
}
