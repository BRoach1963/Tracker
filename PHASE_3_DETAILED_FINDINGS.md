# Phase 3: Detailed Findings - Custom Controls & Styling

## Finding 1: Critical Security Vulnerability in SocialMediaLinkTextBox

**File**: `Tracker/Tracker/Controls/CustomControls/SocialMediaLinkTextBox.cs` (line 26)

**Current Code**:
```csharp
private void ExecuteLaunchUrlCommand(object? obj)
{
    if (string.IsNullOrEmpty(this.Text)) return;
    
    // ❌ DANGEROUS: No URL validation
    Process.Start(FormatUrl(this.Text));
}

private ProcessStartInfo FormatUrl(string url)
{
    // Only adds https:// prefix, no validation
    if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
        !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    {
        url = "https://" + url;
    }
    
    return new ProcessStartInfo
    {
        FileName = url,
        UseShellExecute = true
    };
}
```

**Issues**:
1. User can paste `javascript:alert('xss')` → becomes `https://javascript:alert('xss')`
2. User can paste `file:///C:/Windows/System32/cmd.exe`
3. No validation of URL scheme (only http/https allowed)
4. No error handling if Process.Start fails
5. UseShellExecute=true is dangerous with untrusted input

**Attack Scenarios**:
- Paste `file:///C:/sensitive/data.txt` → opens local file
- Paste `cmd.exe /c del *.*` → could execute commands
- Paste `\\attacker.com\share` → SMB attack

**Recommendation**: Validate URL scheme, use Uri class, whitelist protocols

---

## Finding 2: Event Handler Memory Leaks in TextBoxWithHint

**File**: `Tracker/Tracker/Controls/CustomControls/TextBoxWithHint.cs` (lines 176-198)

**Current Code**:
```csharp
private void SubscribeToControlEvents()
{
    this.PreviewTextInput += OnPreviewTextInput;
    this.Loaded += OnLoaded;
    this.Unloaded += OnUnloaded;
    this.GotFocus += OnGotFocus;
    this.LostFocus += OnLostFocus;
    this.TextChanged += OnTextChangedCallback;
    this.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
    this.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
}

private void UnsubscribeToControlEvents()
{
    // Only called from OnUnloaded
    this.PreviewTextInput -= OnPreviewTextInput;
    // ... etc
}

protected void OnUnloaded(object sender, RoutedEventArgs routedEventArgs)
{
    UnsubscribeToControlEvents();
}
```

**Issues**:
1. If Unloaded event doesn't fire (e.g., window closed abruptly), handlers stay subscribed
2. If control is reused in ItemsControl, subscriptions accumulate
3. No IDisposable pattern for explicit cleanup
4. 8 event subscriptions = 8 potential memory leaks

**Scenario**:
- ItemsControl with 100 TextBoxWithHint controls
- User scrolls, controls virtualized but not unloaded
- Each control keeps 8 event handlers alive
- = 800 event handlers in memory

**Recommendation**: Implement IDisposable, use weak event patterns, or unsubscribe in destructor

---

## Finding 3: Hardcoded Colors Break Theme System

**File**: `Tracker/Tracker/Controls/CustomControls/MeasurableItem.xaml.cs` (lines 33-35)

**Current Code**:
```csharp
private static readonly SolidColorBrush KpiBrush = new(Color.FromRgb(99, 102, 241));     // Indigo
private static readonly SolidColorBrush ProjectBrush = new(Color.FromRgb(16, 185, 129)); // Green
private static readonly SolidColorBrush TaskBrush = new(Color.FromRgb(245, 158, 11));    // Amber
```

**Also in StatusBadge.xaml.cs** (lines 28-33):
```csharp
private static readonly SolidColorBrush OnTrackBrush = new(Color.FromRgb(16, 185, 129));
private static readonly SolidColorBrush AtRiskBrush = new(Color.FromRgb(245, 158, 11));
private static readonly SolidColorBrush OffTrackBrush = new(Color.FromRgb(239, 68, 68));
```

**Issues**:
1. Theme changes don't affect these colors
2. Colors hardcoded in 2+ places (DRY violation)
3. Inconsistent with theme system (ThemeManager)
4. Can't customize colors per theme
5. Difficult to maintain color consistency

**Impact**:
- User switches to "Light" theme
- MeasurableItem still shows dark indigo
- StatusBadge still shows dark colors
- Visual inconsistency

**Recommendation**: Move to theme resources, use DynamicResource binding

---

## Finding 4: Inefficient Visual Tree Traversal

**File**: `OkrCard.xaml.cs` (lines 183-193) & `KeyResultItem.xaml.cs` (lines 195-205)

**Current Code**:
```csharp
private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    if (e.OriginalSource is DependencyObject source)
    {
        var button = FindParent<Button>(source);  // ❌ Walks entire tree
        if (button == ActionsButton) return;
    }
    RaiseEvent(new RoutedEventArgs(CardClickedEvent, this));
}

private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
{
    var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
    while (parent != null)
    {
        if (parent is T found)
            return found;
        parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
    }
    return null;
}
```

**Issues**:
1. Called on EVERY mouse click
2. Walks entire visual tree if button not found
3. No caching of results
4. Duplicated in 2 controls (code duplication)
5. O(n) complexity where n = tree depth

**Performance Impact**:
- OkrCard in ListBox with 50 items
- Each click walks ~10 levels of visual tree
- = 500 tree traversals per interaction

**Recommendation**: Cache ActionsButton reference, use VisualTreeHelper.HitTest, or use event routing

---

## Finding 5: Missing Null Checks in Callbacks

**File**: `CalendarButton.xaml.cs` (line 37)

**Current Code**:
```csharp
private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    var control = (CalendarButton)d;
    if (control.DateCalendar != null)  // ✅ Has check
    {
        control.DateCalendar.SelectedDate = e.NewValue as DateTime?;
    }
}
```

**But in MeasurableItem.xaml.cs** (line 185):
```csharp
private static void OnMeasurableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    if (d is MeasurableItem item && e.NewValue is IMeasurable measurable)
    {
        item.DisplayName = measurable.DisplayName;  // ✅ Good
        item.DisplayValue = measurable.DisplayValue;
        item.MeasurableType = measurable.MeasurableType;
    }
}
```

**And AgendaItemControl.xaml.cs** (line 45):
```csharp
private static void OnAgendaItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    var control = (AgendaItemControl)d;
    control.DataContext = e.NewValue;  // ❌ No null check
}
```

**Issues**:
1. Inconsistent null checking patterns
2. Race condition if property set before InitializeComponent()
3. Silent failures if null
4. No validation of property values

**Recommendation**: Consistent null checking, validate before use

---

## Finding 6: Inconsistent Event Subscription Patterns

**Patterns Found**:
1. **TextBoxWithHint**: Subscribe in constructor, unsubscribe in OnUnloaded
2. **SocialMediaLinkTextBox**: Only subscribe to Unloaded (incomplete)
3. **CalendarButton**: Subscribe in constructor, NO unsubscribe

**Issues**:
- No consistent pattern across controls
- Makes code review difficult
- Increases bug risk
- Hard to maintain

**Recommendation**: Establish standard pattern, document in guidelines

---

## Finding 7: Theme Resource Lookup Performance

**File**: `TrackerProgressBar.xaml.cs` (lines 107-120)

**Current Code**:
```csharp
public Brush TrackBrush
{
    get => (Brush)GetValue(TrackBrushProperty) ?? (Brush)FindResource("BackgroundBrush");
    set => SetValue(TrackBrushProperty, value);
}

public Brush FillBrush
{
    get => (Brush)GetValue(FillBrushProperty) ?? (Brush)FindResource("AccentBrush");
    set => SetValue(FillBrushProperty, value);
}
```

**Issues**:
1. FindResource() called on EVERY property access
2. No caching of theme resources
3. Inefficient for frequently accessed properties
4. In ListBox with 50 items = 50+ FindResource calls per render

**Recommendation**: Cache theme resources, use DynamicResource in XAML instead

---

## Summary

**Total Issues Found**: 7
- **Critical**: 1 (Security)
- **High**: 3 (Memory, Theme, Performance)
- **Medium**: 3 (Robustness, Consistency, Performance)

**Most Impactful**: Security vulnerability in SocialMediaLinkTextBox
**Most Common**: Hardcoded colors and event handler patterns

