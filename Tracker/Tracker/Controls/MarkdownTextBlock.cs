using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Tracker.Controls
{
    /// <summary>
    /// A TextBlock that renders simple Markdown formatting inline.
    /// Supports: **bold**, *italic*, `code`, [links](url), and numbered/bulleted lists.
    /// </summary>
    public class MarkdownTextBlock : TextBlock
    {
        #region Dependency Properties

        public static readonly DependencyProperty MarkdownProperty =
            DependencyProperty.Register(
                nameof(Markdown),
                typeof(string),
                typeof(MarkdownTextBlock),
                new PropertyMetadata(string.Empty, OnMarkdownChanged));

        public static readonly DependencyProperty CodeBackgroundProperty =
            DependencyProperty.Register(
                nameof(CodeBackground),
                typeof(Brush),
                typeof(MarkdownTextBlock),
                new PropertyMetadata(null));

        public static readonly DependencyProperty LinkForegroundProperty =
            DependencyProperty.Register(
                nameof(LinkForeground),
                typeof(Brush),
                typeof(MarkdownTextBlock),
                new PropertyMetadata(null));

        /// <summary>
        /// The markdown content to render.
        /// </summary>
        public string Markdown
        {
            get => (string)GetValue(MarkdownProperty);
            set => SetValue(MarkdownProperty, value);
        }

        /// <summary>
        /// Background color for inline code spans.
        /// </summary>
        public Brush? CodeBackground
        {
            get => (Brush?)GetValue(CodeBackgroundProperty);
            set => SetValue(CodeBackgroundProperty, value);
        }

        /// <summary>
        /// Foreground color for links.
        /// </summary>
        public Brush? LinkForeground
        {
            get => (Brush?)GetValue(LinkForegroundProperty);
            set => SetValue(LinkForegroundProperty, value);
        }

        #endregion

        private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MarkdownTextBlock control)
            {
                control.RenderMarkdown((string?)e.NewValue ?? string.Empty);
            }
        }

        private void RenderMarkdown(string markdown)
        {
            Inlines.Clear();

            if (string.IsNullOrEmpty(markdown))
                return;

            // Process line by line to handle lists and paragraphs
            var lines = markdown.Split('\n');
            bool firstLine = true;
            bool previousWasListItem = false;

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd('\r');

                // Check for list items
                var bulletMatch = Regex.Match(line, @"^(\s*)[-*•]\s+(.*)$");
                var numberedMatch = Regex.Match(line, @"^(\s*)(\d+)\.\s+(.*)$");
                bool isListItem = bulletMatch.Success || numberedMatch.Success;

                // Add line break between lines (except first)
                if (!firstLine)
                {
                    Inlines.Add(new LineBreak());
                    
                    // Add extra spacing between paragraphs (blank lines or after list items ending)
                    if (string.IsNullOrWhiteSpace(line) || (!isListItem && previousWasListItem))
                    {
                        Inlines.Add(new LineBreak());
                    }
                }
                firstLine = false;
                previousWasListItem = isListItem;

                // Skip blank lines after adding the breaks
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (bulletMatch.Success)
                {
                    var indent = bulletMatch.Groups[1].Value.Length / 2;
                    var content = bulletMatch.Groups[2].Value;
                    Inlines.Add(new Run(new string(' ', indent * 2) + "  • "));
                    AddFormattedText(content);
                }
                else if (numberedMatch.Success)
                {
                    var indent = numberedMatch.Groups[1].Value.Length / 2;
                    var number = numberedMatch.Groups[2].Value;
                    var content = numberedMatch.Groups[3].Value;
                    Inlines.Add(new Run(new string(' ', indent * 2) + "  " + number + ". "));
                    AddFormattedText(content);
                }
                else
                {
                    AddFormattedText(line);
                }
            }
        }

        private void AddFormattedText(string text)
        {
            // Pattern to match: **bold**, *italic*, `code`, [text](url)
            // Using a combined pattern to process in order
            var pattern = @"(\*\*(.+?)\*\*)|(\*(.+?)\*)|(`(.+?)`)|(\[([^\]]+)\]\(([^\)]+)\))";
            var matches = Regex.Matches(text, pattern);

            int lastIndex = 0;
            foreach (Match match in matches)
            {
                // Add text before match
                if (match.Index > lastIndex)
                {
                    Inlines.Add(new Run(text.Substring(lastIndex, match.Index - lastIndex)));
                }

                if (match.Groups[2].Success) // **bold**
                {
                    Inlines.Add(new Bold(new Run(match.Groups[2].Value)));
                }
                else if (match.Groups[4].Success) // *italic*
                {
                    Inlines.Add(new Italic(new Run(match.Groups[4].Value)));
                }
                else if (match.Groups[6].Success) // `code`
                {
                    var codeRun = new Run(match.Groups[6].Value)
                    {
                        FontFamily = new FontFamily("Consolas"),
                    };
                    
                    // Apply code background if set
                    if (CodeBackground != null)
                    {
                        var border = new Border
                        {
                            Background = CodeBackground,
                            CornerRadius = new CornerRadius(3),
                            Padding = new Thickness(4, 1, 4, 1),
                            Child = new TextBlock { Text = match.Groups[6].Value, FontFamily = new FontFamily("Consolas") }
                        };
                        Inlines.Add(new InlineUIContainer(border) { BaselineAlignment = BaselineAlignment.Center });
                    }
                    else
                    {
                        Inlines.Add(codeRun);
                    }
                }
                else if (match.Groups[8].Success) // [text](url)
                {
                    var linkText = match.Groups[8].Value;
                    var linkUrl = match.Groups[9].Value;
                    var hyperlink = new Hyperlink(new Run(linkText))
                    {
                        NavigateUri = Uri.TryCreate(linkUrl, UriKind.Absolute, out var uri) ? uri : null,
                        ToolTip = linkUrl
                    };
                    
                    if (LinkForeground != null)
                    {
                        hyperlink.Foreground = LinkForeground;
                    }
                    
                    hyperlink.RequestNavigate += (s, e) =>
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = e.Uri.ToString(),
                                UseShellExecute = true
                            });
                        }
                        catch { }
                        e.Handled = true;
                    };
                    
                    Inlines.Add(hyperlink);
                }

                lastIndex = match.Index + match.Length;
            }

            // Add remaining text after the last match (or whole text if no matches)
            if (lastIndex < text.Length)
            {
                Inlines.Add(new Run(text.Substring(lastIndex)));
            }
        }
    }
}
