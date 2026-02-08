using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace ProCohere.Avalonia.Controls;

/// <summary>
/// A TextBlock that renders basic markdown formatting.
/// Supports: **bold**, *italic*, `code`, ### headers, • bullets, numbered lists
/// </summary>
public class MarkdownTextBlock : SelectableTextBlock
{
    public static readonly StyledProperty<string?> MarkdownProperty =
        AvaloniaProperty.Register<MarkdownTextBlock, string?>(nameof(Markdown));

    public string? Markdown
    {
        get => GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    static MarkdownTextBlock()
    {
        MarkdownProperty.Changed.AddClassHandler<MarkdownTextBlock>((x, e) => x.OnMarkdownChanged());
    }

    public MarkdownTextBlock()
    {
        TextWrapping = TextWrapping.Wrap;
    }

    private void OnMarkdownChanged()
    {
        Inlines?.Clear();
        
        var markdown = Markdown;
        if (string.IsNullOrEmpty(markdown))
            return;

        ParseMarkdown(markdown);
    }

    // Regex patterns
    private static readonly Regex BoldPattern = new(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
    private static readonly Regex ItalicPattern = new(@"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", RegexOptions.Compiled);
    private static readonly Regex CodePattern = new(@"`([^`]+)`", RegexOptions.Compiled);
    private static readonly Regex HeaderPattern = new(@"^(#{1,3})\s+(.+)$", RegexOptions.Compiled);
    private static readonly Regex BulletPattern = new(@"^\s*[\*\-•]\s+(.*)$", RegexOptions.Compiled);
    private static readonly Regex NumberedPattern = new(@"^\s*(\d+)\.\s+(.*)$", RegexOptions.Compiled);

    private void ParseMarkdown(string markdown)
    {
        var lines = markdown.Split('\n');
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            
            if (i > 0)
            {
                Inlines?.Add(new LineBreak());
            }
            
            // Skip empty lines but add spacing
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            
            // Check for headers
            var headerMatch = HeaderPattern.Match(line);
            if (headerMatch.Success)
            {
                var headerLevel = headerMatch.Groups[1].Value.Length;
                var headerText = headerMatch.Groups[2].Value.Trim();
                
                Inlines?.Add(new Run
                {
                    Text = headerText,
                    FontWeight = FontWeight.Bold,
                    FontSize = headerLevel switch
                    {
                        1 => 16,
                        2 => 14,
                        _ => 13
                    }
                });
                continue;
            }
            
            // Check for bullet points
            var bulletMatch = BulletPattern.Match(line);
            if (bulletMatch.Success)
            {
                Inlines?.Add(new Run { Text = "  • " });
                ParseInlineFormatting(bulletMatch.Groups[1].Value);
                continue;
            }
            
            // Check for numbered lists
            var numberedMatch = NumberedPattern.Match(line);
            if (numberedMatch.Success)
            {
                Inlines?.Add(new Run { Text = $"  {numberedMatch.Groups[1].Value}. " });
                ParseInlineFormatting(numberedMatch.Groups[2].Value);
                continue;
            }
            
            // Regular line - parse inline formatting
            ParseInlineFormatting(line);
        }
    }

    private void ParseInlineFormatting(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        // Find all formatting spans
        var spans = new List<(int Start, int End, string Content, SpanType Type)>();
        
        // Find bold spans
        foreach (Match match in BoldPattern.Matches(text))
        {
            spans.Add((match.Index, match.Index + match.Length, match.Groups[1].Value, SpanType.Bold));
        }
        
        // Find code spans
        foreach (Match match in CodePattern.Matches(text))
        {
            spans.Add((match.Index, match.Index + match.Length, match.Groups[1].Value, SpanType.Code));
        }
        
        // Sort by position and remove overlaps
        spans.Sort((a, b) => a.Start.CompareTo(b.Start));
        var nonOverlapping = new List<(int Start, int End, string Content, SpanType Type)>();
        int lastEnd = -1;
        foreach (var span in spans)
        {
            if (span.Start >= lastEnd)
            {
                nonOverlapping.Add(span);
                lastEnd = span.End;
            }
        }
        
        // Build text with formatting
        int currentPos = 0;
        foreach (var span in nonOverlapping)
        {
            // Add plain text before this span
            if (span.Start > currentPos)
            {
                var plainText = text.Substring(currentPos, span.Start - currentPos);
                AddTextWithItalics(plainText);
            }
            
            // Add formatted span
            switch (span.Type)
            {
                case SpanType.Bold:
                    Inlines?.Add(new Run
                    {
                        Text = span.Content,
                        FontWeight = FontWeight.Bold
                    });
                    break;
                    
                case SpanType.Code:
                    Inlines?.Add(new Run
                    {
                        Text = span.Content,
                        FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New, monospace"),
                        Background = new SolidColorBrush(Color.FromRgb(230, 230, 230))
                    });
                    break;
            }
            
            currentPos = span.End;
        }
        
        // Add remaining text
        if (currentPos < text.Length)
        {
            AddTextWithItalics(text.Substring(currentPos));
        }
        
        // If no spans, just add plain text with italic support
        if (nonOverlapping.Count == 0 && text.Length > 0 && currentPos == 0)
        {
            AddTextWithItalics(text);
        }
    }

    private void AddTextWithItalics(string text)
    {
        int currentPos = 0;
        foreach (Match match in ItalicPattern.Matches(text))
        {
            // Add text before italic
            if (match.Index > currentPos)
            {
                Inlines?.Add(new Run { Text = text.Substring(currentPos, match.Index - currentPos) });
            }
            
            // Add italic text
            Inlines?.Add(new Run
            {
                Text = match.Groups[1].Value,
                FontStyle = FontStyle.Italic
            });
            
            currentPos = match.Index + match.Length;
        }
        
        // Add remaining text
        if (currentPos < text.Length)
        {
            Inlines?.Add(new Run { Text = text.Substring(currentPos) });
        }
    }

    private enum SpanType
    {
        Bold,
        Italic,
        Code
    }
}
