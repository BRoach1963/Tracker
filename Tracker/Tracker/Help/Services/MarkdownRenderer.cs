using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;
using Tracker.Logging;

namespace Tracker.Help.Services
{
    /// <summary>
    /// Renders Markdown content to WPF FlowDocument for display in the help viewer.
    /// Supports common Markdown elements styled to match the application theme.
    /// </summary>
    public class MarkdownRenderer
    {
        private readonly ILogger _logger;
        
        // Theme colors (loaded from application resources with fallbacks)
        private Brush _foregroundBrush = Brushes.White;
        private Brush _accentBrush = Brushes.Gold;
        private Brush _backgroundBrush = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        private Brush _codeBrush = new SolidColorBrush(Color.FromRgb(40, 44, 52));
        private Brush _blockquoteBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));
        private Brush _linkBrush = new SolidColorBrush(Color.FromRgb(100, 149, 237));
        private Brush _tableBorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
        private Brush _tableHeaderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 50));
        private FontFamily _codeFont = new FontFamily("Consolas");

        /// <summary>
        /// Event fired when a help topic link is clicked.
        /// </summary>
        public event EventHandler<string>? TopicLinkClicked;

        public MarkdownRenderer()
        {
            _logger = LoggingManager.GetComponentLogger("MarkdownRenderer");
            LoadThemeColors();
        }

        /// <summary>
        /// Renders markdown content to a FlowDocument.
        /// </summary>
        /// <param name="markdown">The markdown content to render.</param>
        /// <returns>A styled FlowDocument.</returns>
        public FlowDocument Render(string markdown)
        {
            var document = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 14,
                Foreground = _foregroundBrush,
                PagePadding = new Thickness(20),
                TextAlignment = TextAlignment.Left
            };

            if (string.IsNullOrWhiteSpace(markdown))
            {
                document.Blocks.Add(new Paragraph(new Run("No content available.")));
                return document;
            }

            try
            {
                var lines = markdown.Split('\n');
                var currentParagraph = new Paragraph();
                var inCodeBlock = false;
                var codeBlockContent = new List<string>();
                var inTable = false;
                var tableRows = new List<string>();
                var inList = false;
                var listItems = new List<(int indent, string text, bool ordered)>();

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].TrimEnd('\r');

                    // Code block handling
                    if (line.StartsWith("```"))
                    {
                        if (inCodeBlock)
                        {
                            // End code block
                            document.Blocks.Add(CreateCodeBlock(string.Join("\n", codeBlockContent)));
                            codeBlockContent.Clear();
                            inCodeBlock = false;
                        }
                        else
                        {
                            // Start code block - flush current paragraph
                            FlushParagraph(document, currentParagraph);
                            currentParagraph = new Paragraph();
                            inCodeBlock = true;
                        }
                        continue;
                    }

                    if (inCodeBlock)
                    {
                        codeBlockContent.Add(line);
                        continue;
                    }

                    // Table handling
                    if (line.StartsWith("|") && line.EndsWith("|"))
                    {
                        if (!inTable)
                        {
                            FlushParagraph(document, currentParagraph);
                            currentParagraph = new Paragraph();
                            inTable = true;
                        }
                        tableRows.Add(line);
                        continue;
                    }
                    else if (inTable)
                    {
                        document.Blocks.Add(CreateTable(tableRows));
                        tableRows.Clear();
                        inTable = false;
                    }

                    // List handling
                    var listMatch = Regex.Match(line, @"^(\s*)(-|\*|\d+\.)\s+(.+)$");
                    if (listMatch.Success)
                    {
                        if (!inList)
                        {
                            FlushParagraph(document, currentParagraph);
                            currentParagraph = new Paragraph();
                            inList = true;
                        }
                        var indent = listMatch.Groups[1].Value.Length;
                        var isOrdered = char.IsDigit(listMatch.Groups[2].Value[0]);
                        listItems.Add((indent, listMatch.Groups[3].Value, isOrdered));
                        continue;
                    }
                    else if (inList)
                    {
                        document.Blocks.Add(CreateList(listItems));
                        listItems.Clear();
                        inList = false;
                    }

                    // Empty line - paragraph break
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        FlushParagraph(document, currentParagraph);
                        currentParagraph = new Paragraph();
                        continue;
                    }

                    // Headers
                    var headerMatch = Regex.Match(line, @"^(#{1,6})\s+(.+)$");
                    if (headerMatch.Success)
                    {
                        FlushParagraph(document, currentParagraph);
                        currentParagraph = new Paragraph();
                        
                        var level = headerMatch.Groups[1].Value.Length;
                        var text = headerMatch.Groups[2].Value;
                        document.Blocks.Add(CreateHeader(text, level));
                        continue;
                    }

                    // Horizontal rule
                    if (Regex.IsMatch(line, @"^(-{3,}|\*{3,}|_{3,})$"))
                    {
                        FlushParagraph(document, currentParagraph);
                        currentParagraph = new Paragraph();
                        document.Blocks.Add(CreateHorizontalRule());
                        continue;
                    }

                    // Blockquote
                    if (line.StartsWith(">"))
                    {
                        FlushParagraph(document, currentParagraph);
                        currentParagraph = new Paragraph();
                        
                        var quoteText = line.TrimStart('>').Trim();
                        document.Blocks.Add(CreateBlockquote(quoteText));
                        continue;
                    }

                    // Regular text - add to current paragraph
                    if (currentParagraph.Inlines.Count > 0)
                    {
                        currentParagraph.Inlines.Add(new Run(" "));
                    }
                    AddFormattedText(currentParagraph, line);
                }

                // Flush remaining content
                if (inTable && tableRows.Count > 0)
                {
                    document.Blocks.Add(CreateTable(tableRows));
                }
                if (inList && listItems.Count > 0)
                {
                    document.Blocks.Add(CreateList(listItems));
                }
                FlushParagraph(document, currentParagraph);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error rendering markdown");
                document.Blocks.Add(new Paragraph(new Run($"Error rendering content: {ex.Message}")));
            }

            return document;
        }

        private void FlushParagraph(FlowDocument document, Paragraph paragraph)
        {
            if (paragraph.Inlines.Count > 0)
            {
                paragraph.Margin = new Thickness(0, 0, 0, 10);
                document.Blocks.Add(paragraph);
            }
        }

        private Block CreateHeader(string text, int level)
        {
            var paragraph = new Paragraph
            {
                Margin = new Thickness(0, level == 1 ? 0 : 15, 0, 10)
            };

            var fontSize = level switch
            {
                1 => 28,
                2 => 22,
                3 => 18,
                4 => 16,
                5 => 14,
                _ => 14
            };

            var run = new Run(text)
            {
                FontSize = fontSize,
                FontWeight = FontWeights.Bold,
                Foreground = level == 1 ? _accentBrush : _foregroundBrush
            };

            paragraph.Inlines.Add(run);

            // Add underline for H1 and H2
            if (level <= 2)
            {
                paragraph.BorderBrush = _accentBrush;
                paragraph.BorderThickness = new Thickness(0, 0, 0, 1);
                paragraph.Padding = new Thickness(0, 0, 0, 5);
            }

            return paragraph;
        }

        private Block CreateCodeBlock(string code)
        {
            var paragraph = new Paragraph
            {
                Background = _codeBrush,
                Padding = new Thickness(15),
                Margin = new Thickness(0, 10, 0, 10),
                FontFamily = _codeFont,
                FontSize = 13
            };

            paragraph.Inlines.Add(new Run(code)
            {
                Foreground = new SolidColorBrush(Color.FromRgb(171, 178, 191))
            });

            return paragraph;
        }

        private Block CreateBlockquote(string text)
        {
            var paragraph = new Paragraph
            {
                Background = _blockquoteBrush,
                Padding = new Thickness(15),
                Margin = new Thickness(20, 10, 0, 10),
                BorderBrush = _accentBrush,
                BorderThickness = new Thickness(4, 0, 0, 0)
            };

            AddFormattedText(paragraph, text);
            return paragraph;
        }

        private Block CreateHorizontalRule()
        {
            return new Paragraph
            {
                Margin = new Thickness(0, 20, 0, 20),
                BorderBrush = _tableBorderBrush,
                BorderThickness = new Thickness(0, 1, 0, 0)
            };
        }

        private Block CreateTable(List<string> rows)
        {
            var table = new Table
            {
                CellSpacing = 0,
                BorderBrush = _tableBorderBrush,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 10, 0, 10)
            };

            if (rows.Count == 0) return table;

            // Parse header row to determine column count
            var headerCells = ParseTableRow(rows[0]);
            var columnCount = headerCells.Count;

            // Create columns
            for (int i = 0; i < columnCount; i++)
            {
                table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            }

            var rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                // Skip separator row
                if (rows[rowIndex].Contains("---")) continue;

                var cells = ParseTableRow(rows[rowIndex]);
                var tableRow = new TableRow();

                for (int cellIndex = 0; cellIndex < columnCount; cellIndex++)
                {
                    var cellText = cellIndex < cells.Count ? cells[cellIndex] : "";
                    var cell = new TableCell(new Paragraph(new Run(cellText)))
                    {
                        Padding = new Thickness(10, 5, 10, 5),
                        BorderBrush = _tableBorderBrush,
                        BorderThickness = new Thickness(0, 0, 1, 1)
                    };

                    // Header row styling
                    if (rowIndex == 0)
                    {
                        cell.Background = _tableHeaderBrush;
                        ((Paragraph)cell.Blocks.FirstBlock).FontWeight = FontWeights.Bold;
                    }

                    tableRow.Cells.Add(cell);
                }

                rowGroup.Rows.Add(tableRow);
            }

            return table;
        }

        private List<string> ParseTableRow(string row)
        {
            return row.Trim('|')
                .Split('|')
                .Select(c => c.Trim())
                .ToList();
        }

        private Block CreateList(List<(int indent, string text, bool ordered)> items)
        {
            var list = new List
            {
                Margin = new Thickness(0, 10, 0, 10),
                Padding = new Thickness(20, 0, 0, 0)
            };

            int orderedCounter = 1;
            foreach (var (indent, text, ordered) in items)
            {
                var listItem = new ListItem();
                var paragraph = new Paragraph();
                AddFormattedText(paragraph, text);
                listItem.Blocks.Add(paragraph);
                listItem.Margin = new Thickness(indent * 10, 2, 0, 2);
                
                if (ordered)
                {
                    list.MarkerStyle = TextMarkerStyle.Decimal;
                }
                else
                {
                    list.MarkerStyle = TextMarkerStyle.Disc;
                }

                list.ListItems.Add(listItem);
                orderedCounter++;
            }

            return list;
        }

        private void AddFormattedText(Paragraph paragraph, string text)
        {
            // Process inline formatting
            var pattern = @"(\*\*\*(.+?)\*\*\*)|(\*\*(.+?)\*\*)|(\*(.+?)\*)|(`(.+?)`)|(\[(.+?)\]\((.+?)\))|(~~(.+?)~~)";
            var lastIndex = 0;
            
            foreach (Match match in Regex.Matches(text, pattern))
            {
                // Add text before match
                if (match.Index > lastIndex)
                {
                    paragraph.Inlines.Add(new Run(text.Substring(lastIndex, match.Index - lastIndex)));
                }

                if (match.Groups[1].Success) // Bold + Italic ***text***
                {
                    paragraph.Inlines.Add(new Run(match.Groups[2].Value)
                    {
                        FontWeight = FontWeights.Bold,
                        FontStyle = FontStyles.Italic
                    });
                }
                else if (match.Groups[3].Success) // Bold **text**
                {
                    paragraph.Inlines.Add(new Run(match.Groups[4].Value)
                    {
                        FontWeight = FontWeights.Bold
                    });
                }
                else if (match.Groups[5].Success) // Italic *text*
                {
                    paragraph.Inlines.Add(new Run(match.Groups[6].Value)
                    {
                        FontStyle = FontStyles.Italic
                    });
                }
                else if (match.Groups[7].Success) // Code `text`
                {
                    paragraph.Inlines.Add(new Run(match.Groups[8].Value)
                    {
                        FontFamily = _codeFont,
                        Background = _codeBrush,
                        Foreground = new SolidColorBrush(Color.FromRgb(171, 178, 191))
                    });
                }
                else if (match.Groups[9].Success) // Link [text](url)
                {
                    var linkText = match.Groups[10].Value;
                    var linkUrl = match.Groups[11].Value;
                    
                    var hyperlink = new Hyperlink(new Run(linkText))
                    {
                        Foreground = _linkBrush,
                        TextDecorations = null
                    };

                    // Handle internal vs external links
                    if (linkUrl.StartsWith("http://") || linkUrl.StartsWith("https://"))
                    {
                        hyperlink.NavigateUri = new Uri(linkUrl);
                        hyperlink.RequestNavigate += (s, e) =>
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = e.Uri.ToString(),
                                UseShellExecute = true
                            });
                            e.Handled = true;
                        };
                    }
                    else
                    {
                        // Internal topic link
                        var topicId = linkUrl.Replace(".md", "").TrimStart('.', '/');
                        hyperlink.Click += (s, e) =>
                        {
                            TopicLinkClicked?.Invoke(this, topicId);
                        };
                    }

                    paragraph.Inlines.Add(hyperlink);
                }
                else if (match.Groups[12].Success) // Strikethrough ~~text~~
                {
                    paragraph.Inlines.Add(new Run(match.Groups[13].Value)
                    {
                        TextDecorations = TextDecorations.Strikethrough
                    });
                }

                lastIndex = match.Index + match.Length;
            }

            // Add remaining text
            if (lastIndex < text.Length)
            {
                paragraph.Inlines.Add(new Run(text.Substring(lastIndex)));
            }
        }

        private void LoadThemeColors()
        {
            try
            {
                if (Application.Current?.Resources != null)
                {
                    var resources = Application.Current.Resources;
                    
                    if (resources["ForegroundBrush"] is Brush fg)
                        _foregroundBrush = fg;
                    if (resources["AccentBrush"] is Brush accent)
                    {
                        _accentBrush = accent;
                        _linkBrush = accent; // Use accent for links too
                    }
                    if (resources["BackgroundBrush"] is Brush bg)
                        _backgroundBrush = bg;
                    if (resources["PopupBackgroundBrush"] is Brush popup)
                    {
                        _codeBrush = popup;
                        _blockquoteBrush = popup;
                        _tableHeaderBrush = popup;
                    }
                    if (resources["BorderBrush"] is Brush border)
                        _tableBorderBrush = border;
                }
            }
            catch
            {
                // Use defaults if resources not available
            }
        }

        /// <summary>
        /// Reloads theme colors. Call this when theme changes.
        /// </summary>
        public void RefreshTheme()
        {
            LoadThemeColors();
        }
    }
}

