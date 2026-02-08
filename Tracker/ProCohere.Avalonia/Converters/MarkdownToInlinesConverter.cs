using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ProCohere.Avalonia.Converters;

/// <summary>
/// Converts markdown text to Avalonia Inlines for rich text display in chat.
/// Supports: **bold**, *italic*, `code`, ### headers, • bullets
/// </summary>
public class MarkdownToInlinesConverter : IValueConverter
{
    public static readonly MarkdownToInlinesConverter Instance = new();
    
    // Regex patterns for markdown parsing
    private static readonly Regex BoldPattern = new(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
    private static readonly Regex ItalicPattern = new(@"\*(.+?)\*", RegexOptions.Compiled);
    private static readonly Regex CodePattern = new(@"`(.+?)`", RegexOptions.Compiled);
    private static readonly Regex HeaderPattern = new(@"^#{1,3}\s+(.+)$", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex BulletPattern = new(@"^\s*[\*\-•]\s+", RegexOptions.Multiline | RegexOptions.Compiled);
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string markdown || string.IsNullOrEmpty(markdown))
            return new InlineCollection();
        
        var inlines = new InlineCollection();
        ParseMarkdown(markdown, inlines);
        return inlines;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private void ParseMarkdown(string markdown, InlineCollection inlines)
    {
        var lines = markdown.Split('\n');
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            
            if (i > 0)
            {
                inlines.Add(new LineBreak());
            }
            
            // Check for headers (### Header)
            var headerMatch = HeaderPattern.Match(line);
            if (headerMatch.Success)
            {
                var headerLevel = line.TakeWhile(c => c == '#').Count();
                var headerText = headerMatch.Groups[1].Value.Trim();
                
                inlines.Add(new Run
                {
                    Text = headerText,
                    FontWeight = FontWeight.Bold,
                    FontSize = headerLevel switch
                    {
                        1 => 18,
                        2 => 16,
                        _ => 14
                    }
                });
                continue;
            }
            
            // Check for bullet points
            var bulletMatch = BulletPattern.Match(line);
            if (bulletMatch.Success)
            {
                inlines.Add(new Run { Text = "  • " });
                line = line.Substring(bulletMatch.Length);
            }
            
            // Parse inline formatting
            ParseInlineFormatting(line, inlines);
        }
    }
    
    private void ParseInlineFormatting(string text, InlineCollection inlines)
    {
        var segments = new List<(string Text, bool Bold, bool Italic, bool Code)>();
        var currentPos = 0;
        
        // Find all formatting markers
        var allMatches = new List<(int Start, int End, string Content, string Type)>();
        
        foreach (Match match in BoldPattern.Matches(text))
        {
            allMatches.Add((match.Index, match.Index + match.Length, match.Groups[1].Value, "bold"));
        }
        
        foreach (Match match in CodePattern.Matches(text))
        {
            allMatches.Add((match.Index, match.Index + match.Length, match.Groups[1].Value, "code"));
        }
        
        // Sort by position
        allMatches.Sort((a, b) => a.Start.CompareTo(b.Start));
        
        // Remove overlapping matches (keep first)
        var nonOverlapping = new List<(int Start, int End, string Content, string Type)>();
        int lastEnd = -1;
        foreach (var match in allMatches)
        {
            if (match.Start >= lastEnd)
            {
                nonOverlapping.Add(match);
                lastEnd = match.End;
            }
        }
        
        // Build segments
        currentPos = 0;
        foreach (var match in nonOverlapping)
        {
            // Add text before this match
            if (match.Start > currentPos)
            {
                var beforeText = text.Substring(currentPos, match.Start - currentPos);
                // Check for italic in the before text (only if not bold)
                AddTextWithItalics(beforeText, inlines);
            }
            
            // Add the formatted text
            switch (match.Type)
            {
                case "bold":
                    inlines.Add(new Run
                    {
                        Text = match.Content,
                        FontWeight = FontWeight.Bold
                    });
                    break;
                case "code":
                    inlines.Add(new Run
                    {
                        Text = match.Content,
                        FontFamily = new FontFamily("Cascadia Code, Consolas, monospace"),
                        Background = Brushes.LightGray
                    });
                    break;
            }
            
            currentPos = match.End;
        }
        
        // Add remaining text
        if (currentPos < text.Length)
        {
            var remaining = text.Substring(currentPos);
            AddTextWithItalics(remaining, inlines);
        }
        
        // If no matches, just add plain text with italic support
        if (nonOverlapping.Count == 0 && text.Length > 0)
        {
            // Already handled above
        }
    }
    
    private void AddTextWithItalics(string text, InlineCollection inlines)
    {
        var currentPos = 0;
        foreach (Match match in ItalicPattern.Matches(text))
        {
            // Skip if this looks like it might be part of bold (already handled)
            if (match.Index > 0 && text[match.Index - 1] == '*')
                continue;
            if (match.Index + match.Length < text.Length && text[match.Index + match.Length] == '*')
                continue;
            
            // Add text before
            if (match.Index > currentPos)
            {
                inlines.Add(new Run { Text = text.Substring(currentPos, match.Index - currentPos) });
            }
            
            // Add italic text
            inlines.Add(new Run
            {
                Text = match.Groups[1].Value,
                FontStyle = FontStyle.Italic
            });
            
            currentPos = match.Index + match.Length;
        }
        
        // Add remaining
        if (currentPos < text.Length)
        {
            inlines.Add(new Run { Text = text.Substring(currentPos) });
        }
        else if (currentPos == 0 && text.Length > 0)
        {
            inlines.Add(new Run { Text = text });
        }
    }
}
