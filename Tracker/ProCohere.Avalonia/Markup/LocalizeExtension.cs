using System;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.Markup;

/// <summary>
/// XAML markup extension for localized strings.
/// Usage: Text="{markup:Localize Key=Button_Save}"
/// </summary>
public class LocalizeExtension : MarkupExtension
{
    /// <summary>
    /// The resource key to look up.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Optional fallback text if key is not found.
    /// </summary>
    public string? Fallback { get; set; }

    public LocalizeExtension()
    {
    }

    public LocalizeExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key))
            return Fallback ?? string.Empty;

        var localized = LocalizationService.Instance.Get(Key);
        
        // If the key was not found (returns [Key]), use fallback if provided
        if (localized.StartsWith("[") && localized.EndsWith("]") && Fallback != null)
            return Fallback;

        return localized;
    }
}

/// <summary>
/// Binding-based localization extension that updates when culture changes.
/// Usage: Text="{markup:LocalizeBinding Key=Button_Save}"
/// </summary>
public class LocalizeBindingExtension : MarkupExtension
{
    /// <summary>
    /// The resource key to look up.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public LocalizeBindingExtension()
    {
    }

    public LocalizeBindingExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key))
            return string.Empty;

        // Create a source that updates when culture changes
        var source = new LocalizedStringSource(Key);
        
        // Create and return a binding to the source's Value property
        var binding = new Binding
        {
            Source = source,
            Path = nameof(LocalizedStringSource.Value),
            Mode = BindingMode.OneWay
        };

        return binding;
    }
}

/// <summary>
/// Helper class that provides a bindable localized string value.
/// Listens for culture changes and updates the value.
/// </summary>
public class LocalizedStringSource : System.ComponentModel.INotifyPropertyChanged
{
    private readonly string _key;

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    public string Value => LocalizationService.Instance.Get(_key);

    public LocalizedStringSource(string key)
    {
        _key = key;
        LocalizationService.Instance.CultureChanged += OnCultureChanged;
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Value)));
    }
}
