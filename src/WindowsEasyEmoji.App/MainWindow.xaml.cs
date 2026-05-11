using System.IO;
using System.Windows;
using System.Windows.Input;
using WindowsEasyEmoji.Core.Emoji;
using WindowsEasyEmoji.Core.Search;
using WindowsEasyEmoji.Platform.Clipboard;
using WindowsEasyEmoji.Platform.Diagnostics;

namespace WindowsEasyEmoji.App;

public partial class MainWindow : Window
{
    private readonly EmojiSearchService searchService;
    private readonly PasteCoordinator pasteCoordinator;
    private PasteOptions pasteOptions;
    private IntPtr targetWindowHandle;

    public MainWindow(PasteCoordinator pasteCoordinator, PasteOptions pasteOptions)
    {
        InitializeComponent();
        this.pasteCoordinator = pasteCoordinator;
        this.pasteOptions = pasteOptions;
        UpdatePasteStatus();

        var dataPath = Path.Combine(AppContext.BaseDirectory, "Data", "emoji.json");
        var records = File.Exists(dataPath)
            ? EmojiRepository.LoadFromJson(File.ReadAllText(dataPath))
            : [];
        DiagnosticLog.Write($"main-window.init dataPath={dataPath} recordCount={records.Count}");

        searchService = new EmojiSearchService(records);
        SearchBox.Text = "하트";
        RefreshResults();
    }

    public void FocusSearchBox()
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
        DiagnosticLog.Write($"main-window.focus-search-box isKeyboardFocusWithin={SearchBox.IsKeyboardFocusWithin}");
    }

    public void RememberTargetWindow(IntPtr windowHandle)
    {
        targetWindowHandle = windowHandle;
        DiagnosticLog.Write($"main-window.remember-target target={DiagnosticLog.Handle(targetWindowHandle)}");
    }

    public void UpdatePasteOptions(PasteOptions pasteOptions)
    {
        this.pasteOptions = pasteOptions;
        UpdatePasteStatus();
    }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        RefreshResults();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DiagnosticLog.Write("main-window.key escape");
            Hide();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && ResultsList.SelectedItem is SearchResult result)
        {
            DiagnosticLog.Write($"main-window.key enter selected={result.Record.Id}");
            PasteResult(result);
            e.Handled = true;
        }
    }

    private void SearchBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key is not (Key.Up or Key.Down) || ResultsList.Items.Count == 0)
        {
            return;
        }

        var direction = e.Key == Key.Down ? 1 : -1;
        var nextIndex = ResultsList.SelectedIndex + direction;
        if (nextIndex < 0)
        {
            nextIndex = ResultsList.Items.Count - 1;
        }
        else if (nextIndex >= ResultsList.Items.Count)
        {
            nextIndex = 0;
        }

        ResultsList.SelectedIndex = nextIndex;
        ResultsList.ScrollIntoView(ResultsList.SelectedItem);
        e.Handled = true;
    }

    private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ResultsList.SelectedItem is SearchResult result)
        {
            PasteResult(result);
        }
    }

    private void RefreshResults()
    {
        var results = searchService.Search(SearchBox.Text);
        ResultsList.ItemsSource = results;
        ResultsList.SelectedIndex = results.Count > 0 ? 0 : -1;
        DiagnosticLog.Write($"main-window.refresh queryLength={SearchBox.Text.Length} resultCount={results.Count} selectedIndex={ResultsList.SelectedIndex}");
    }

    private void PasteResult(SearchResult result)
    {
        DiagnosticLog.Write($"main-window.paste-result begin target={DiagnosticLog.Handle(targetWindowHandle)} emojiId={result.Record.Id}");
        Hide();
        var pasteResult = pasteCoordinator.PasteToTarget(targetWindowHandle, result.Record.Emoji, pasteOptions);
        DiagnosticLog.Write($"main-window.paste-result complete pasted={pasteResult.Pasted} targetActivated={pasteResult.TargetActivated}");
    }

    private void UpdatePasteStatus()
    {
        PasteStatusText.Text = pasteOptions.AutoPaste
            ? pasteOptions.RestoreOriginalClipboard ? "붙여넣기 · 클립보드 복원" : "자동 붙여넣기"
            : "클립보드에 복사";
    }
}
