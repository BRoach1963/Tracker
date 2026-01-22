# 09 – Converters Reference

This document describes all **Value Converters** in ProCohere.Avalonia.

---

## Overview

Converters transform values for XAML bindings:
- `IValueConverter` - single value transformation
- `IMultiValueConverter` - multiple values combined
- `FuncValueConverter<TIn, TOut>` - inline lambda converters

All converters use static `Instance` for singleton access in XAML.

---

## Converter Index

### Navigation Converters (NavigationConverters.cs)

| Converter | Type | Input → Output |
|-----------|------|----------------|
| `EnumEqualConverter` | IValueConverter | Enum + Enum → bool |
| `EnumNotEqualConverter` | IValueConverter | Enum + Enum → bool |
| `NavWidthConverter` | IValueConverter | bool → double (64/200) |
| `ThemeIconConverter` | IValueConverter | bool → StreamGeometry |
| `ThemeTextConverter` | IValueConverter | bool → string |
| `CollapseIconConverter` | IValueConverter | bool → StreamGeometry |
| `BoolToColorConverter` | IMultiValueConverter | bool → Brush |
| `MeetingStatusColorConverter` | IMultiValueConverter | bool → Brush |
| `EqualToZeroConverter` | IValueConverter | int → bool |
| `GreaterThanZeroConverter` | IValueConverter | int → bool |
| `InverseBoolConverter` | IValueConverter | bool → bool |
| `BoolToVisibilityConverter` | IValueConverter | bool → bool (visibility) |
| `StringNotEmptyConverter` | IValueConverter | string → bool |
| `NullToVisibilityConverter` | IValueConverter | object → bool |
| `NotNullToVisibilityConverter` | IValueConverter | object → bool |
| `DateToStringConverter` | IValueConverter | DateTime → string |
| `TimeSpanToStringConverter` | IValueConverter | TimeSpan → string |
| `StatusToColorConverter` | IValueConverter | string → Brush |
| `PriorityToColorConverter` | IValueConverter | string → Brush |
| `InitialsConverter` | IValueConverter | string → string |
| `FirstCharacterConverter` | IValueConverter | string → string |
| `TruncateConverter` | IValueConverter | string + int → string |
| `CountToVisibilityConverter` | IValueConverter | int → bool |
| `ListToCountConverter` | IValueConverter | IList → int |
| `HealthToBrushConverter` | IValueConverter | GoalHealth → Brush |
| `TrendToArrowConverter` | IValueConverter | MetricTrend → string |
| `TrendToBrushConverter` | IValueConverter | MetricTrend → Brush |

### Phone Converter (PhoneNumberConverter.cs)

| Converter | Type | Input → Output |
|-----------|------|----------------|
| `PhoneNumberConverter` | IValueConverter | string → formatted string |

---

## Core Converters

### EnumEqualConverter
Compare enum value to parameter:

```csharp
public object? Convert(object? value, Type targetType, 
    object? parameter, CultureInfo culture)
{
    return value?.Equals(parameter) ?? false;
}
```

**XAML Usage**:
```xml
<RadioButton IsChecked="{Binding SelectedTab, 
    Converter={x:Static conv:EnumEqualConverter.Instance}, 
    ConverterParameter={x:Static vm:MeTab.Tasks}}" />
```

### InverseBoolConverter
Negate a boolean:

```csharp
public object? Convert(object? value, ...)
{
    return value is bool b ? !b : false;
}
```

**XAML Usage**:
```xml
<Button IsEnabled="{Binding IsLoading, 
    Converter={x:Static conv:InverseBoolConverter.Instance}}" />
```

### NullToVisibilityConverter
Show/hide based on null:

```csharp
public object? Convert(object? value, ...)
{
    return value == null;
}
```

**XAML Usage**:
```xml
<TextBlock Text="No avatar" 
    IsVisible="{Binding AvatarUrl, 
    Converter={x:Static conv:NullToVisibilityConverter.Instance}}" />
```

---

## Navigation Converters

### NavWidthConverter
Sidebar width based on expanded state:

```csharp
public object? Convert(object? value, ...)
{
    return value is bool isExpanded 
        ? (isExpanded ? 200.0 : 64.0) 
        : 200.0;
}
```

**XAML Usage**:
```xml
<Border Width="{Binding IsNavigationExpanded, 
    Converter={x:Static conv:NavWidthConverter.Instance}}" />
```

### ThemeIconConverter
Sun/Moon icon for theme toggle:

```csharp
// Sun icon for dark mode (click to switch to light)
private const string SunIcon = "M12,7A5,5 0 0,1...";

// Moon icon for light mode (click to switch to dark)
private const string MoonIcon = "M17.75,4.09...";

public object? Convert(object? value, ...)
{
    return value is bool isDark 
        ? StreamGeometry.Parse(isDark ? SunIcon : MoonIcon)
        : StreamGeometry.Parse(SunIcon);
}
```

### CollapseIconConverter
Chevron icon for expand/collapse:

```csharp
private const string CollapseIcon = "M15.41,16.58..."; // Left chevron
private const string ExpandIcon = "M8.59,16.58...";    // Right chevron

public object? Convert(object? value, ...)
{
    return value is bool isExpanded 
        ? StreamGeometry.Parse(isExpanded ? CollapseIcon : ExpandIcon)
        : StreamGeometry.Parse(CollapseIcon);
}
```

---

## Status Converters

### HealthToBrushConverter
Goal health to color:

```csharp
public object? Convert(object? value, ...)
{
    return value is GoalHealth health ? health switch
    {
        GoalHealth.OnTrack => Brushes.Green,
        GoalHealth.NeedsAttention => Brushes.Orange,
        GoalHealth.AtRisk => Brushes.Red,
        GoalHealth.ReframingNeeded => Brushes.Purple,
        _ => Brushes.Gray
    } : Brushes.Gray;
}
```

### TrendToArrowConverter
Metric trend to arrow symbol:

```csharp
public object? Convert(object? value, ...)
{
    return value is MetricTrend trend ? trend switch
    {
        MetricTrend.TrendingUp => "↗",
        MetricTrend.Stable => "→",
        MetricTrend.TrendingDown => "↘",
        MetricTrend.Variable => "↔",
        _ => "?"
    } : "?";
}
```

### StatusToColorConverter
Task/meeting status to color:

```csharp
public object? Convert(object? value, ...)
{
    return value?.ToString()?.ToLower() switch
    {
        "completed" => Brushes.Green,
        "in_progress" => Brushes.Blue,
        "blocked" => Brushes.Red,
        "scheduled" => Brushes.Gray,
        "cancelled" => Brushes.DarkGray,
        _ => Brushes.Gray
    };
}
```

### PriorityToColorConverter
Task priority to color:

```csharp
public object? Convert(object? value, ...)
{
    return value?.ToString()?.ToLower() switch
    {
        "high" => Brushes.Red,
        "medium" => Brushes.Orange,
        "low" => Brushes.Gray,
        _ => Brushes.Gray
    };
}
```

---

## Date/Time Converters

### DateToStringConverter
Format DateTime:

```csharp
public object? Convert(object? value, object? parameter, ...)
{
    if (value is DateTime dt)
    {
        var format = parameter?.ToString() ?? "MMM d, yyyy";
        return dt.ToString(format);
    }
    return string.Empty;
}
```

**XAML Usage**:
```xml
<TextBlock Text="{Binding DueDate, 
    Converter={x:Static conv:DateToStringConverter.Instance},
    ConverterParameter='MMM d'}" />
```

### TimeSpanToStringConverter
Format TimeSpan:

```csharp
public object? Convert(object? value, ...)
{
    if (value is TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        return $"{ts.Minutes}m";
    }
    return string.Empty;
}
```

---

## String Converters

### InitialsConverter
Full name to initials:

```csharp
public object? Convert(object? value, ...)
{
    if (value is string name && !string.IsNullOrEmpty(name))
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][0].ToString().ToUpper(),
            _ => $"{parts[0][0]}{parts[^1][0]}".ToUpper()
        };
    }
    return "?";
}
```

### TruncateConverter
Truncate with ellipsis:

```csharp
public object? Convert(object? value, object? parameter, ...)
{
    if (value is string text && parameter is string maxLengthStr 
        && int.TryParse(maxLengthStr, out var maxLength))
    {
        return text.Length > maxLength 
            ? text[..(maxLength - 3)] + "..." 
            : text;
    }
    return value;
}
```

**XAML Usage**:
```xml
<TextBlock Text="{Binding Description, 
    Converter={x:Static conv:TruncateConverter.Instance},
    ConverterParameter='100'}" />
```

---

## Phone Number Converter

Formats phone numbers:

```csharp
public static string FormatPhoneNumber(string? phone)
{
    var digits = StripToDigits(phone);
    
    return digits.Length switch
    {
        10 => $"({digits[..3]}) {digits[3..6]}-{digits[6..]}",
        11 when digits.StartsWith('1') => 
            $"+1 ({digits[1..4]}) {digits[4..7]}-{digits[7..]}",
        > 11 => FormatInternational(digits),
        _ => phone
    };
}
```

**XAML Usage**:
```xml
<TextBlock Text="{Binding Phone, 
    Converter={x:Static conv:PhoneNumberConverter.Instance}}" />
```

---

## Collection Converters

### ListToCountConverter
Get count from collection:

```csharp
public object? Convert(object? value, ...)
{
    return value is IList list ? list.Count : 0;
}
```

### CountToVisibilityConverter
Show if count > 0:

```csharp
public object? Convert(object? value, ...)
{
    return value is int count && count > 0;
}
```

---

## Multi-Value Converters

### BoolToColorConverter
Multiple bools to color:

```csharp
public object? Convert(IList<object?> values, ...)
{
    if (values.Count > 0 && values[0] is bool isActive)
    {
        return isActive ? Brushes.Green : Brushes.Gray;
    }
    return Brushes.Gray;
}
```

**XAML Usage**:
```xml
<Ellipse Fill="{MultiBinding 
    Converter={x:Static conv:BoolToColorConverter.Instance}}">
    <MultiBinding.Bindings>
        <Binding Path="IsActive" />
    </MultiBinding.Bindings>
</Ellipse>
```

---

## XAML Registration

In `App.axaml` resources:
```xml
<Application.Resources>
    <conv:InverseBoolConverter x:Key="InverseBool" />
    <conv:DateToStringConverter x:Key="DateToString" />
</Application.Resources>
```

Or use static instances:
```xml
xmlns:conv="clr-namespace:ProCohere.Avalonia.Converters"

Converter={x:Static conv:InverseBoolConverter.Instance}
```

---

## Invariants

1. **Singletons via Instance** - no new allocations in XAML
2. **Null-safe** - always handle null input
3. **ConvertBack rarely implemented** - one-way bindings common
4. **No business logic** - pure transformations only

