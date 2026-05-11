using System.IO;
using System.Windows;
using System.Windows.Input;
using WindowsEasyEmoji.Core.Emoji;
using WindowsEasyEmoji.Core.Search;
using WindowsEasyEmoji.Platform.Clipboard;

namespace WindowsEasyEmoji.App;

public partial class MainWindow : Window
{
    private readonly EmojiSearchService searchService;
    private readonly ClipboardPasteService pasteService = new();

    public MainWindow()
    {
        InitializeComponent();

        var dataPath = Path.Combine(AppContext.BaseDirectory, "Data", "emoji.json");
        var records = File.Exists(dataPath)
            ? EmojiRepository.LoadFromJson(File.ReadAllText(dataPath))
            : [];

        searchService = new EmojiSearchService(records);
        SearchBox.Text = "하트";
        RefreshResults();
    }

    public void FocusSearchBox()
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        RefreshResults();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && ResultsList.SelectedItem is SearchResult result)
        {
            if (!pasteService.PasteText(result.Record.Emoji))
            {
                pasteService.CopyText(result.Record.Emoji);
            }

            Hide();
            e.Handled = true;
        }
    }

    private void RefreshResults()
    {
        var results = searchService.Search(SearchBox.Text);
        ResultsList.ItemsSource = results;
        ResultsList.SelectedIndex = results.Count > 0 ? 0 : -1;
    }
}
