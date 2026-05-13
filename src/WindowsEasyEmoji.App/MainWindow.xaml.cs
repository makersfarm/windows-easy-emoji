using System.IO;
using System.Windows;
using System.Windows.Input;
using WindowsEasyEmoji.Core.Emoji;
using WindowsEasyEmoji.Core.Search;
using WindowsEasyEmoji.Core.User;
using WindowsEasyEmoji.Platform.Clipboard;
using WindowsEasyEmoji.Platform.Diagnostics;
using WindowsEasyEmoji.Platform.UserState;

namespace WindowsEasyEmoji.App;

public partial class MainWindow : Window
{
    private readonly EmojiSearchService searchService;
    private readonly PasteCoordinator pasteCoordinator;
    private readonly Dictionary<string, UserEmojiState> userStateByEmojiId;
    private readonly UserEmojiStateStore userEmojiStateStore;
    private PasteOptions pasteOptions;
    private IntPtr targetWindowHandle;

    public MainWindow(
        PasteCoordinator pasteCoordinator,
        PasteOptions pasteOptions,
        Dictionary<string, UserEmojiState> userStateByEmojiId,
        UserEmojiStateStore userEmojiStateStore)
    {
        InitializeComponent();
        this.pasteCoordinator = pasteCoordinator;
        this.pasteOptions = pasteOptions;
        this.userStateByEmojiId = userStateByEmojiId;
        this.userEmojiStateStore = userEmojiStateStore;
        UpdatePasteStatus();

        var dataPath = Path.Combine(AppContext.BaseDirectory, "Data", "emoji.json");
        var records = File.Exists(dataPath)
            ? EmojiRepository.LoadFromJson(File.ReadAllText(dataPath))
            : [];
        DiagnosticLog.Write($"main-window.init dataPath={dataPath} recordCount={records.Count}");

        searchService = new EmojiSearchService(records, userStateByEmojiId);
        EmojiRecordCount = records.Count;
        SearchBox.Text = string.Empty;
        RefreshResults();
    }

    public int EmojiRecordCount { get; }

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
        if (e.Key == Key.D && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            ToggleSelectedFavorite();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            DiagnosticLog.Write("main-window.key escape");
            Hide();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && ResultsList.SelectedItem is EmojiResultItem result)
        {
            DiagnosticLog.Write($"main-window.key enter selected={result.Record.Id}");
            PasteResult(result.Result);
            e.Handled = true;
        }
    }

    private void SearchBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.D && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            ToggleSelectedFavorite();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && ResultsList.SelectedItem is EmojiResultItem result)
        {
            PasteResult(result.Result);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
            return;
        }

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
        if (ResultsList.SelectedItem is EmojiResultItem result)
        {
            PasteResult(result.Result);
        }
    }

    private void RefreshResults(string? preferredEmojiId = null)
    {
        var results = searchService
            .Search(SearchBox.Text)
            .Select(result => new EmojiResultItem(
                result,
                userStateByEmojiId.TryGetValue(result.Record.Id, out var state) && state.Favorite))
            .ToArray();
        ResultsList.ItemsSource = results;
        ResultsList.SelectedIndex = GetSelectedIndex(results, preferredEmojiId);
        var noResults = SearchBox.Text.Length > 0 && results.Length == 0;
        ResultsList.Visibility = noResults ? Visibility.Collapsed : Visibility.Visible;
        NoResultsPanel.Visibility = noResults ? Visibility.Visible : Visibility.Collapsed;
        var selectedResult = ResultsList.SelectedItem as EmojiResultItem;
        var topResultId = selectedResult?.Record.Id ?? "none";
        var topMatchType = selectedResult?.MatchType.ToString() ?? "None";
        var topScore = selectedResult?.Result.Score ?? 0;
        DiagnosticLog.Write($"main-window.refresh queryLength={SearchBox.Text.Length} resultCount={results.Length} selectedIndex={ResultsList.SelectedIndex} topResult={topResultId} topMatch={topMatchType} topScore={topScore} noResults={noResults}");
    }

    private static int GetSelectedIndex(IReadOnlyList<EmojiResultItem> results, string? preferredEmojiId)
    {
        if (results.Count == 0)
        {
            return -1;
        }

        if (!string.IsNullOrWhiteSpace(preferredEmojiId))
        {
            for (var index = 0; index < results.Count; index++)
            {
                if (results[index].Record.Id.Equals(preferredEmojiId, StringComparison.Ordinal))
                {
                    return index;
                }
            }
        }

        return 0;
    }

    private void PasteResult(SearchResult result)
    {
        var target = targetWindowHandle;
        var emoji = result.Record.Emoji;
        var options = pasteOptions;
        DiagnosticLog.Write($"main-window.paste-result begin target={DiagnosticLog.Handle(target)} emojiId={result.Record.Id}");
        RecordSelection(result.Record.Id);

        Hide();
        Dispatcher.BeginInvoke(() =>
        {
            var pasteResult = pasteCoordinator.PasteToTarget(target, emoji, options);
            DiagnosticLog.Write($"main-window.paste-result complete pasted={pasteResult.Pasted} targetActivated={pasteResult.TargetActivated}");
        });
    }

    private void ToggleSelectedFavorite()
    {
        if (ResultsList.SelectedItem is not EmojiResultItem result)
        {
            return;
        }

        var emojiId = result.Record.Id;
        userStateByEmojiId.TryGetValue(emojiId, out var currentState);
        var nextState = UserEmojiStateUpdater.ToggleFavorite(currentState, emojiId);
        userStateByEmojiId[emojiId] = nextState;

        try
        {
            userEmojiStateStore.Save(userStateByEmojiId);
            DiagnosticLog.Write($"main-window.favorite toggled emojiId={emojiId} favorite={nextState.Favorite}");
        }
        catch (IOException exception)
        {
            DiagnosticLog.Write($"main-window.favorite save-failed type={exception.GetType().Name} message={exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            DiagnosticLog.Write($"main-window.favorite save-failed type={exception.GetType().Name} message={exception.Message}");
        }

        RefreshResults(emojiId);
    }

    private void UpdatePasteStatus()
    {
        PasteStatusText.Text = pasteOptions.AutoPaste
            ? pasteOptions.RestoreOriginalClipboard ? "붙여넣기 · 클립보드 복원" : "자동 붙여넣기"
            : "클립보드에 복사";
    }

    private void RecordSelection(string emojiId)
    {
        userStateByEmojiId.TryGetValue(emojiId, out var currentState);
        userStateByEmojiId[emojiId] = UserEmojiStateUpdater.RecordUse(
            currentState,
            emojiId,
            DateTimeOffset.Now);

        try
        {
            userEmojiStateStore.Save(userStateByEmojiId);
            DiagnosticLog.Write($"main-window.user-state saved emojiId={emojiId} stateCount={userStateByEmojiId.Count}");
        }
        catch (IOException exception)
        {
            DiagnosticLog.Write($"main-window.user-state save-failed type={exception.GetType().Name} message={exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            DiagnosticLog.Write($"main-window.user-state save-failed type={exception.GetType().Name} message={exception.Message}");
        }
    }

    public sealed record EmojiResultItem(SearchResult Result, bool Favorite)
    {
        public EmojiRecord Record => Result.Record;

        public SearchMatchType MatchType => Result.MatchType;

        public string FavoriteMarker => Favorite ? "★" : string.Empty;
    }
}
