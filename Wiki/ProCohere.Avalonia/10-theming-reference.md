# 10 – Theming Reference

This document describes the **theming system** in ProCohere.Avalonia.

---

## Overview

The app supports Light and Dark themes using:
- **Theme-aware resource dictionaries** (`LightTheme.axaml`, `DarkTheme.axaml`)
- **ThemeService** singleton for runtime switching
- **DynamicResource** bindings in XAML
- **LocalSettingsService** for persistence

---

## Theme Files

| File | Purpose |
|------|---------|
| `Themes/LightTheme.axaml` | Light mode colors and brushes |
| `Themes/DarkTheme.axaml` | Dark mode colors and brushes |
| `Themes/ToolTipOverride.axaml` | Tooltip styling |
| `Services/ThemeService.cs` | Runtime theme switching |
| `Services/LocalSettingsService.cs` | Theme preference persistence |

---

## Color System

### Brand Colors

| Token | Light | Dark | Usage |
|-------|-------|------|-------|
| `ColorPrimary` | #3C6248 | #3C6248 | Primary actions, accents |
| `ColorPrimaryHover` | #32533D | #4F7A5D | Hover state |
| `ColorPrimaryPressed` | #284333 | #2F4F3B | Pressed state |
| `ColorPrimarySoft` | #E7EFEA | #334F7A5D | Subtle backgrounds |
| `ColorSecondary` | #2E3A5A | #2E3A5A | Secondary actions |
| `ColorHighlight` | #D0AF5F | #D0AF5F | Attention, gold accent |

### Background Colors

| Token | Light | Dark | Usage |
|-------|-------|------|-------|
| `ColorBackground` | #FFFFFF | #2E3A5A | App background |
| `ColorSurface` | #F7F8FA | #35436A | Card/panel background |
| `ColorSurfaceAlt` | #F1F3F6 | #3D4C78 | Alternate surface |

### Border Colors

| Token | Light | Dark | Usage |
|-------|-------|------|-------|
| `ColorBorder` | #CDD3DD | #4B5C86 | Standard borders |
| `ColorBorderStrong` | #B6BECB | #6273A0 | Emphasized borders |

### Text Colors

| Token | Light | Dark | Usage |
|-------|-------|------|-------|
| `ColorTextPrimary` | #111827 | #F9FAFB | Primary text |
| `ColorTextSecondary` | #374151 | #D1D5DB | Secondary text |
| `ColorTextTertiary` | #6B7280 | #9CA3AF | Disabled/muted text |
| `ColorTextOnPrimary` | #FFFFFF | #FFFFFF | Text on primary color |

### Semantic Colors

| Token | Light | Dark | Usage |
|-------|-------|------|-------|
| `ColorError` | #B42318 | #F97066 | Error states |
| `ColorWarning` | #B54708 | #FBBF24 | Warning states |
| `ColorSuccess` | #067647 | #34D399 | Success states |
| `ColorInfo` | #175CD3 | #60A5FA | Info states |

---

## Brush Keys

All colors have corresponding `SolidColorBrush` keys:

```xml
<!-- Color and Brush pair -->
<Color x:Key="ColorPrimary">#FF3C6248</Color>
<SolidColorBrush x:Key="BrushPrimary" Color="{StaticResource ColorPrimary}" />
```

### Common Brushes

| Brush Key | Usage |
|-----------|-------|
| `BrushPrimary` | Primary buttons, links |
| `BrushPrimaryHover` | Hover on primary |
| `BrushPrimaryPressed` | Pressed on primary |
| `BrushPrimarySoft` | Subtle primary background |
| `BrushSecondary` | Secondary buttons |
| `BrushHighlight` | Attention items, gold |
| `BrushBackground` | Page background |
| `BrushSurface` | Card background |
| `BrushSurfaceAlt` | Alternate card |
| `BrushBorder` | Standard borders |
| `BrushBorderStrong` | Emphasized borders |
| `BrushTextPrimary` | Main text |
| `BrushTextSecondary` | Secondary text |
| `BrushTextTertiary` | Muted text |
| `BrushTextOnPrimary` | Text on primary color |
| `BrushError` | Error text/icons |
| `BrushWarning` | Warning text/icons |
| `BrushSuccess` | Success text/icons |
| `BrushInfo` | Info text/icons |
| `BrushFocus` | Focus rings |
| `BrushSelection` | Selected item background |

---

## Design Tokens

### Spacing

```xml
<Thickness x:Key="ThicknessControlBorder">1</Thickness>
<Thickness x:Key="ThicknessFocusRing">2</Thickness>
```

### Corner Radius

```xml
<CornerRadius x:Key="CornerRadiusControl">10</CornerRadius>
<CornerRadius x:Key="CornerRadiusSmall">6</CornerRadius>
```

### Typography

```xml
<FontFamily x:Key="FontFamilyDefault">Inter, Segoe UI, Arial</FontFamily>
<x:Double x:Key="FontSizeBody">14</x:Double>
<x:Double x:Key="FontSizeCaption">12</x:Double>
<x:Double x:Key="FontSizeTitle">18</x:Double>
```

### Opacity

```xml
<x:Double x:Key="OpacityDisabled">0.55</x:Double>
```

---

## ThemeService

### Access
```csharp
ThemeService.Instance
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `IsDarkTheme` | bool | Current theme state |

### Methods

| Method | Description |
|--------|-------------|
| `ApplyTheme(bool isDark)` | Apply specific theme |
| `ToggleTheme()` | Switch between light/dark |
| `Initialize()` | Apply saved theme on startup |

### Events

```csharp
public event Action<bool>? ThemeChanged;
```

### Implementation

```csharp
public void ApplyTheme(bool isDark)
{
    if (Application.Current != null)
    {
        Application.Current.RequestedThemeVariant = isDark 
            ? ThemeVariant.Dark 
            : ThemeVariant.Light;
    }
}
```

---

## Using Themes in XAML

### DynamicResource (Theme-Aware)

Always use `DynamicResource` for theme-aware colors:

```xml
<Border Background="{DynamicResource BrushSurface}"
        BorderBrush="{DynamicResource BrushBorder}">
    <TextBlock Foreground="{DynamicResource BrushTextPrimary}" 
               Text="Hello" />
</Border>
```

### StaticResource (Non-Changing)

Use `StaticResource` only for tokens that don't change:

```xml
<Border CornerRadius="{StaticResource CornerRadiusControl}" />
```

### Example: Card

```xml
<Border Background="{DynamicResource BrushSurface}"
        BorderBrush="{DynamicResource BrushBorder}"
        BorderThickness="1"
        CornerRadius="{StaticResource CornerRadiusControl}"
        Padding="16">
    <StackPanel Spacing="8">
        <TextBlock Text="{Binding Title}"
                   Foreground="{DynamicResource BrushTextPrimary}"
                   FontSize="{StaticResource FontSizeTitle}"
                   FontWeight="SemiBold" />
        <TextBlock Text="{Binding Description}"
                   Foreground="{DynamicResource BrushTextSecondary}"
                   FontSize="{StaticResource FontSizeBody}" />
    </StackPanel>
</Border>
```

### Example: Button States

```xml
<Button Background="{DynamicResource BrushPrimary}"
        Foreground="{DynamicResource BrushTextOnPrimary}">
    <Button.Styles>
        <Style Selector="Button:pointerover">
            <Setter Property="Background" 
                    Value="{DynamicResource BrushPrimaryHover}" />
        </Style>
        <Style Selector="Button:pressed">
            <Setter Property="Background" 
                    Value="{DynamicResource BrushPrimaryPressed}" />
        </Style>
    </Button.Styles>
    Click Me
</Button>
```

---

## App.axaml Theme Loading

```xml
<Application xmlns="https://github.com/avaloniaui"
             RequestedThemeVariant="Light">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key="Light" 
                    Source="/Themes/LightTheme.axaml" />
                <ResourceDictionary x:Key="Dark" 
                    Source="/Themes/DarkTheme.axaml" />
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

---

## Theme Toggle in UI

### ViewModel

```csharp
[ObservableProperty]
private bool _isDarkTheme;

[RelayCommand]
private void ToggleTheme()
{
    IsDarkTheme = !IsDarkTheme;
    ThemeService.Instance.IsDarkTheme = IsDarkTheme;
}
```

### View

```xml
<Button Command="{Binding ToggleThemeCommand}">
    <PathIcon Data="{Binding IsDarkTheme, 
        Converter={x:Static conv:ThemeIconConverter.Instance}}" />
</Button>
```

---

## Persistence

Theme preference is saved to:
```
%LocalAppData%\ProCohere\settings.json
```

```json
{
  "IsDarkTheme": false,
  "RememberedEmail": "user@example.com",
  "RememberEmail": true
}
```

Loaded on startup in `App.axaml.cs`:
```csharp
ThemeService.Instance.Initialize();
```

---

## Custom Control Styling

### Override Avalonia Defaults

Create styles in `App.axaml` or theme files:

```xml
<Style Selector="TextBox">
    <Setter Property="Background" Value="{DynamicResource BrushSurface}" />
    <Setter Property="BorderBrush" Value="{DynamicResource BrushBorder}" />
    <Setter Property="Foreground" Value="{DynamicResource BrushTextPrimary}" />
    <Setter Property="CornerRadius" Value="{StaticResource CornerRadiusControl}" />
</Style>

<Style Selector="TextBox:focus">
    <Setter Property="BorderBrush" Value="{DynamicResource BrushFocus}" />
</Style>
```

---

## Semantic Color Usage

| State | Color Token |
|-------|-------------|
| Error | `BrushError` |
| Warning | `BrushWarning` |
| Success | `BrushSuccess` |
| Info | `BrushInfo` |
| Disabled | Apply `OpacityDisabled` |

### Example: Validation

```xml
<TextBlock Text="{Binding ErrorMessage}"
           Foreground="{DynamicResource BrushError}"
           IsVisible="{Binding HasError}" />
```

### Example: Status Badge

```xml
<Border Background="{Binding Status, 
    Converter={x:Static conv:StatusToColorConverter.Instance}}"
    CornerRadius="{StaticResource CornerRadiusSmall}"
    Padding="8,4">
    <TextBlock Text="{Binding StatusDisplay}"
               Foreground="{DynamicResource BrushTextOnPrimary}"
               FontSize="{StaticResource FontSizeCaption}" />
</Border>
```

---

## Invariants

1. **Always use DynamicResource** for colors - enables runtime switching
2. **Match Light/Dark keys** - both themes must define same keys
3. **Test both themes** - verify contrast and readability
4. **Use semantic colors** - Error/Warning/Success/Info consistently
5. **Persist preference** - save and restore on startup

