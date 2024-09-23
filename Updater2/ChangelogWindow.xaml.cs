using System.Windows;
using Markdig;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Net.Http;
using System;

namespace DS4Updater
{
    public partial class ChangelogWindow : Window
    {

        public ChangelogWindow(string url)
        {
            InitializeComponent();
            LoadMarkdownFromUrl(url);
        }
        private async void LoadMarkdownFromUrl(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string markdown = await client.GetStringAsync(url);
                    RenderMarkdown(markdown);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching Markdown: {ex.Message}");
            }
        }

        private void RenderMarkdown(string markdown)
        {
            var formattedText = new TextBlock { Margin = new Thickness(8) }; // Set 8px margin

            // Split markdown by lines and handle basic formatting
            foreach (var line in markdown.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                // Handle headers
                if (line.StartsWith("# ")) // H1
                {
                    formattedText.Inlines.Add(new Run(line.Substring(2)) { FontSize = 24, FontWeight = FontWeights.Bold });
                }
                else if (line.StartsWith("## ")) // H2
                {
                    formattedText.Inlines.Add(new Run(line.Substring(3)) { FontSize = 20, FontWeight = FontWeights.Bold });
                }
                else if (line.StartsWith("### ")) // H3
                {
                    formattedText.Inlines.Add(new Run(line.Substring(4)) { FontSize = 16, FontWeight = FontWeights.Bold });
                }
                else if (line.Contains("**") && line.Contains("**")) // Bold
                {
                    AddFormattedText(formattedText, line, "**", FontWeights.Bold);
                }
                else if (line.Contains("*") && line.Contains("*")) // Italic
                {
                    AddFormattedText(formattedText, line, "*", FontStyles.Italic);
                }
                else if (line.Contains("[") && line.Contains("](")) // Link
                {
                    AddLink(formattedText, line);
                }
                else
                {
                    formattedText.Inlines.Add(new Run(line)); // Regular text
                }

                formattedText.Inlines.Add(new LineBreak()); // Add line break
            }

            // Wrap the TextBlock in a ScrollViewer
            var scrollViewer = new ScrollViewer
            {
                Content = formattedText,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            Content = scrollViewer; // Set ScrollViewer as the window content
        }

        private void AddFormattedText(TextBlock textBlock, string line, string delimiter, object fontWeight)
        {
            var parts = line.Split(new[] { delimiter }, StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++)
            {
                if (i % 2 == 1) // Bold or Italic text
                {
                    textBlock.Inlines.Add(new Run(parts[i]) { FontWeight = (FontWeight)fontWeight });
                }
                else
                {
                    textBlock.Inlines.Add(new Run(parts[i]));
                }
            }
        }

        private void AddLink(TextBlock textBlock, string line)
        {
            var parts = line.Split(new[] { "[", "](", ")" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                textBlock.Inlines.Add(new Run(parts[0])); // Text before the link
                var hyperlink = new Hyperlink(new Run(parts[1]))
                {
                    Foreground = System.Windows.Media.Brushes.Blue,
                    TextDecorations = System.Windows.TextDecorations.Underline
                };
                hyperlink.Click += (s, e) =>
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = parts[2],
                        UseShellExecute = true
                    });
                };
                textBlock.Inlines.Add(hyperlink); // Add the link
                textBlock.Inlines.Add(new Run("")); // Add empty run to handle space after the link
            }
        }
    }
}