using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Views.Controls;

/// <summary>
/// A reusable flyout shell control that animates in/out and displays entity details.
/// The actual content is rendered via DataTemplates based on the entity type.
/// 
/// Usage:
/// 1. Set FlyoutContent to any object implementing IDetailEntity (or any object with a matching DataTemplate)
/// 2. Set IsOpen to true to slide in, false to slide out
/// 3. Parent wires up CloseCommand, EditCommand, DeleteCommand on the entity
/// </summary>
public partial class EntityDetailFlyout : UserControl
{
    private Border? _flyoutContainer;
    private TranslateTransform? _slideTransform;
    private TextBlock? _headerTitle;
    private bool _isAnimating;
    
    #region Styled Properties
    
    /// <summary>
    /// The entity to display in the flyout. 
    /// Should implement IDetailEntity for command bindings, 
    /// and have a matching DataTemplate for rendering.
    /// </summary>
    public static readonly StyledProperty<object?> FlyoutContentProperty =
        AvaloniaProperty.Register<EntityDetailFlyout, object?>(nameof(FlyoutContent));
    
    public object? FlyoutContent
    {
        get => GetValue(FlyoutContentProperty);
        set => SetValue(FlyoutContentProperty, value);
    }
    
    /// <summary>
    /// Whether the flyout is open (visible and slid in).
    /// </summary>
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<EntityDetailFlyout, bool>(nameof(IsOpen), defaultValue: false);
    
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }
    
    /// <summary>
    /// Width of the flyout panel.
    /// </summary>
    public static readonly StyledProperty<double> FlyoutWidthProperty =
        AvaloniaProperty.Register<EntityDetailFlyout, double>(nameof(FlyoutWidth), defaultValue: 360);
    
    public double FlyoutWidth
    {
        get => GetValue(FlyoutWidthProperty);
        set => SetValue(FlyoutWidthProperty, value);
    }
    
    /// <summary>
    /// Duration of the slide animation in milliseconds.
    /// </summary>
    public static readonly StyledProperty<int> AnimationDurationProperty =
        AvaloniaProperty.Register<EntityDetailFlyout, int>(nameof(AnimationDuration), defaultValue: 250);
    
    public int AnimationDuration
    {
        get => GetValue(AnimationDurationProperty);
        set => SetValue(AnimationDurationProperty, value);
    }
    
    #endregion
    
    public EntityDetailFlyout()
    {
        InitializeComponent();
    }
    
    /// <summary>
    /// Handles the close button click by executing the CloseCommand on the entity.
    /// </summary>
    private void OnCloseButtonClick(object? sender, RoutedEventArgs e)
    {
        if (FlyoutContent is IDetailEntity entity && entity.CloseCommand?.CanExecute(null) == true)
        {
            entity.CloseCommand.Execute(null);
        }
    }
    
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        // Get references to animated elements after control is loaded
        _flyoutContainer = this.FindControl<Border>("FlyoutContainer");
        _slideTransform = _flyoutContainer?.RenderTransform as TranslateTransform;
        _headerTitle = this.FindControl<TextBlock>("HeaderTitle");
        
        // Set initial state (hidden, slid off to the right)
        if (_slideTransform != null)
        {
            _slideTransform.X = FlyoutWidth;
        }
        IsVisible = false;
    }
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == IsOpenProperty)
        {
            var isOpen = change.GetNewValue<bool>();
            if (isOpen)
                AnimateIn();
            else
                AnimateOut();
        }
        else if (change.Property == FlyoutContentProperty)
        {
            UpdateHeaderTitle(change.NewValue);
        }
        else if (change.Property == FlyoutWidthProperty)
        {
            // Update initial offset if width changes
            if (!IsOpen && _slideTransform != null)
                _slideTransform.X = FlyoutWidth;
        }
    }
    
    /// <summary>
    /// Updates the header title based on the entity type.
    /// </summary>
    private void UpdateHeaderTitle(object? content)
    {
        if (_headerTitle == null) return;
        
        _headerTitle.Text = content switch
        {
            GoalDetail => "Goal",
            TaskDetail => "Task",
            MetricDetail => "Metric",
            MeetingDetail => "Meeting",
            TeamMemberDetail => "Team Member",
            Note => "Note",
            IDetailEntity entity => entity.GetType().Name.Replace("Detail", ""),
            null => "Details",
            _ => content.GetType().Name.Replace("Detail", "")
        };
    }
    
    /// <summary>
    /// Animates the flyout sliding in from the right.
    /// </summary>
    private async void AnimateIn()
    {
        if (_isAnimating || _slideTransform == null || _flyoutContainer == null) return;
        _isAnimating = true;
        
        try
        {
            // Make visible before animating
            IsVisible = true;
            _slideTransform.X = FlyoutWidth;
            
            // Use simple interpolation animation
            var duration = AnimationDuration;
            var easing = new CubicEaseOut();
            var startTime = DateTime.Now;
            var startX = FlyoutWidth;
            var endX = 0.0;
            
            while (true)
            {
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                var progress = Math.Min(elapsed / duration, 1.0);
                var easedProgress = easing.Ease(progress);
                
                _slideTransform.X = startX + (endX - startX) * easedProgress;
                
                if (progress >= 1.0)
                    break;
                    
                await Task.Delay(16); // ~60fps
            }
            
            _slideTransform.X = 0;
        }
        finally
        {
            _isAnimating = false;
        }
    }
    
    /// <summary>
    /// Animates the flyout sliding out to the right.
    /// </summary>
    private async void AnimateOut()
    {
        if (_isAnimating || _slideTransform == null || _flyoutContainer == null) return;
        _isAnimating = true;
        
        try
        {
            // Use simple interpolation animation
            var duration = AnimationDuration;
            var easing = new CubicEaseIn();
            var startTime = DateTime.Now;
            var startX = 0.0;
            var endX = FlyoutWidth;
            
            while (true)
            {
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                var progress = Math.Min(elapsed / duration, 1.0);
                var easedProgress = easing.Ease(progress);
                
                _slideTransform.X = startX + (endX - startX) * easedProgress;
                
                if (progress >= 1.0)
                    break;
                    
                await Task.Delay(16); // ~60fps
            }
            
            _slideTransform.X = FlyoutWidth;
            
            // Hide after animation completes
            IsVisible = false;
        }
        finally
        {
            _isAnimating = false;
        }
    }
}
