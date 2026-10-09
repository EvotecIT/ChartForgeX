using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Terminal;

namespace ChartForgeX.Stories;

/// <summary>Bounded, resolved terminal events supplied by a capture host or an author.</summary>
/// <remarks>Commands are submitted observations rather than simulated keystrokes. ANSI controls are stripped; hosts resolve screen operations into explicit events.</remarks>
public sealed partial class StoryReplay {
    private const int MaximumEvents = 4096;
    private const int MaximumCharacters = 4 * 1024 * 1024;
    private readonly List<StoryReplayEvent> _events = new();
    private readonly HashSet<string> _tabIds = new(StringComparer.Ordinal);
    private readonly TerminalTab _initialTab;
    private string _activeTabId = "main";
    private int _characters;
    private bool _edited;

    private StoryReplay(TimeSpan duration, TerminalTab initialTab) {
        if (duration <= TimeSpan.Zero || duration > TimeSpan.FromMinutes(30)) throw new ArgumentOutOfRangeException(nameof(duration));
        Duration = duration; OriginalDuration = duration; _initialTab = initialTab; _tabIds.Add(initialTab.Id);
        _characters = initialTab.Title.Length + initialTab.WorkingDirectory.Length + initialTab.CustomPrompt.Length;
    }
    /// <summary>Creates a recording with an explicit endpoint and initial session.</summary>
    public static StoryReplay Create(TimeSpan duration, TerminalDialect dialect = TerminalDialect.PowerShell,
        string workingDirectory = ".", string title = "PowerShell", string? customPrompt = null) =>
        new(duration, Tab("main", title, dialect, workingDirectory, customPrompt));
    /// <summary>Gets the presentation duration, including recorded idle time.</summary>
    public TimeSpan Duration { get; private set; }
    /// <summary>Gets the original duration before trimming or pause compression.</summary>
    public TimeSpan OriginalDuration { get; private set; }
    /// <summary>Gets the initial recording dialect.</summary>
    public TerminalDialect Dialect => _initialTab.Dialect;
    /// <summary>Gets the initial tab title.</summary>
    public string Title => _initialTab.Title;
    /// <summary>Gets the initial prompt directory.</summary>
    public string WorkingDirectory => _initialTab.WorkingDirectory;
    /// <summary>Gets the initial custom prompt.</summary>
    public string CustomPrompt => _initialTab.CustomPrompt;
    /// <summary>Gets immutable events in stable timestamp order.</summary>
    public IReadOnlyList<StoryReplayEvent> Events => _events.AsReadOnly();
    internal long RetainedCharacters => _characters;

    /// <summary>Adds a submitted command at its recorded timestamp.</summary>
    public StoryReplay Command(TimeSpan at, string command) => Add(at, StoryReplayEventKind.Command, OneLine(command, nameof(command)));
    /// <summary>Adds completed, newline-separated output. Use ReplaceLine for a carriage-return update.</summary>
    public StoryReplay Output(TimeSpan at, string text, TerminalTextTone tone = TerminalTextTone.Default) {
        var normalized = BoundedText(text, nameof(text));
        normalized = TerminalTextSanitizer.Transcript(normalized).Replace("\r\n", "\n");
        if (normalized.IndexOf('\r') >= 0) throw new ArgumentException("Resolve carriage-return updates with ReplaceLine before replay.", nameof(text));
        ValidateTone(tone);
        return Add(at, StoryReplayEventKind.Output, normalized, tone);
    }
    /// <summary>Replaces the active tab's last line with resolved progress text.</summary>
    public StoryReplay ReplaceLine(TimeSpan at, string text, TerminalTextTone tone = TerminalTextTone.Default) {
        ValidateTone(tone); return Add(at, StoryReplayEventKind.ReplaceLine, OneLine(text, nameof(text)), tone);
    }
    /// <summary>Clears displayed lines in the active tab without discarding recorded transcript events.</summary>
    public StoryReplay Clear(TimeSpan at) => Add(at, StoryReplayEventKind.Clear, string.Empty);
    /// <summary>Adds an explanation or chapter marker at a recorded timestamp.</summary>
    public StoryReplay Marker(TimeSpan at, string label) => Add(at, StoryReplayEventKind.Marker, Heading(label, nameof(label)));
    /// <summary>Changes the active tab's prompt directory.</summary>
    public StoryReplay ChangeDirectory(TimeSpan at, string directory) => Add(at, StoryReplayEventKind.Directory, OneLine(directory, nameof(directory)));
    /// <summary>Opens and activates a persistent session.</summary>
    public StoryReplay OpenTab(TimeSpan at, string id, string title, TerminalDialect dialect = TerminalDialect.PowerShell,
        string workingDirectory = ".", string? customPrompt = null) {
        var tab = Tab(id, title, dialect, workingDirectory, customPrompt);
        if (_tabIds.Count >= 8 || _tabIds.Contains(tab.Id)) throw new ArgumentException("Replay supports up to eight unique tabs.", nameof(id));
        CheckEvent(at, string.Empty, tab.Title.Length + tab.WorkingDirectory.Length + tab.CustomPrompt.Length);
        _events.Add(new StoryReplayEvent(at, at, StoryReplayEventKind.OpenTab, string.Empty, tab.Id, tab: tab));
        _tabIds.Add(tab.Id); _activeTabId = tab.Id; _characters += tab.Title.Length + tab.WorkingDirectory.Length + tab.CustomPrompt.Length;
        return this;
    }
    /// <summary>Activates a previously opened session.</summary>
    public StoryReplay SelectTab(TimeSpan at, string id) {
        if (!_tabIds.Contains(id)) throw new ArgumentException("Select an existing replay tab.", nameof(id));
        Add(at, StoryReplayEventKind.SelectTab, string.Empty, tabId: id); _activeTabId = id; return this;
    }

    /// <summary>Returns a detached replay with an explanation inserted on the presentation clock, without inventing a recorded timestamp.</summary>
    public StoryReplay Explain(TimeSpan at, string label) {
        if (at < TimeSpan.Zero || at > Duration) throw new ArgumentOutOfRangeException(nameof(at));
        var text = Heading(label, nameof(label));
        if (_events.Count >= MaximumEvents || _characters + text.Length > MaximumCharacters) throw new InvalidOperationException("Replay payload budget exceeded.");
        var copy = Capture(); var index = copy._events.FindIndex(item => item.Timestamp > at);
        copy._events.Insert(index < 0 ? copy._events.Count : index, new StoryReplayEvent(at, null, StoryReplayEventKind.Marker, text, "main"));
        copy._characters += text.Length; copy._edited = true; return copy;
    }

    /// <summary>Returns a detached presentation interval. Earlier events are retained at zero to reconstruct tab and screen state.</summary>
    /// <remarks>Trimming is not redaction: the transcript retains the context used to reconstruct the first frame.</remarks>
    public StoryReplay Trim(TimeSpan start, TimeSpan end) {
        if (start < TimeSpan.Zero || end > Duration || end <= start) throw new ArgumentOutOfRangeException(nameof(start));
        var copy = NewCopy(end - start);
        foreach (var item in _events) {
            if (item.Timestamp > end) break;
            copy.AppendCaptured(item.At(item.Timestamp <= start ? TimeSpan.Zero : item.Timestamp - start));
        }
        copy._edited = true; return copy;
    }
    /// <summary>Returns a detached presentation with every idle gap, including leading and trailing gaps, capped explicitly.</summary>
    /// <remarks>OriginalTimestamp and OriginalDuration retain the recording's timing for host captions and manifests.</remarks>
    public StoryReplay CompressPauses(TimeSpan maximumGap) {
        if (maximumGap <= TimeSpan.Zero || maximumGap > TimeSpan.FromMinutes(30)) throw new ArgumentOutOfRangeException(nameof(maximumGap));
        var copy = NewCopy(Duration); var previous = TimeSpan.Zero; var mapped = TimeSpan.Zero;
        foreach (var item in _events) {
            var gap = item.Timestamp - previous; mapped += gap > maximumGap ? maximumGap : gap;
            copy.AppendCaptured(item.At(mapped)); previous = item.Timestamp;
        }
        var trailing = Duration - previous;
        copy.Duration = mapped + (trailing > maximumGap ? maximumGap : trailing);
        copy._edited = true;
        return copy;
    }

    /// <summary>Gets sanitized chronological output, including cleared lines and both timestamps.</summary>
    public string ToTranscript() {
        var state = new ReplayState(_initialTab, 1); var output = new StringBuilder();
        output.AppendLine("Replay: " + _initialTab.Title);
        foreach (var item in _events) {
            output.Append('[').Append(item.Timestamp.ToString("c")).Append("; ").Append(item.OriginalTimestamp.HasValue ? "recorded " + item.OriginalTimestamp.Value.ToString("c") : "authored explanation").Append("] ");
            if (item.Kind == StoryReplayEventKind.Command) output.Append(state.Active.Tab.Prompt());
            output.Append(item.Kind).Append(": ");
            if (item.Kind == StoryReplayEventKind.OpenTab || item.Kind == StoryReplayEventKind.SelectTab) {
                state.Apply(item);
                output.AppendLine(state.Active.Tab.Title + " [" + item.TabId + "]");
            } else {
                output.AppendLine(item.Text);
                state.Apply(item);
            }
        }
        return output.ToString();
    }

    internal StoryReplay Capture() {
        var copy = NewCopy(Duration); foreach (var item in _events) copy.AppendCaptured(item); return copy;
    }
    private StoryReplay NewCopy(TimeSpan duration) => new(duration, _initialTab) { OriginalDuration = OriginalDuration, _edited = _edited };
    private void AppendCaptured(StoryReplayEvent item) {
        _events.Add(item); _characters += item.Text.Length;
        if (item.Kind == StoryReplayEventKind.OpenTab) { _tabIds.Add(item.TabId); _characters += item.Tab!.Title.Length + item.Tab.WorkingDirectory.Length + item.Tab.CustomPrompt.Length; }
        if (item.Kind == StoryReplayEventKind.OpenTab || item.Kind == StoryReplayEventKind.SelectTab) _activeTabId = item.TabId;
    }
    private StoryReplay Add(TimeSpan at, StoryReplayEventKind kind, string text, TerminalTextTone tone = TerminalTextTone.Default, string? tabId = null) {
        CheckEvent(at, text); _events.Add(new StoryReplayEvent(at, at, kind, text, tabId ?? _activeTabId, tone)); _characters += text.Length; return this;
    }
    private void CheckEvent(TimeSpan at, string text, int metadataCharacters = 0) {
        if (_edited) throw new InvalidOperationException("Record observations before editing the presentation. Use Explain to insert presentation text.");
        if (at < TimeSpan.Zero || at > Duration || _events.Count > 0 && at < _events[_events.Count - 1].Timestamp)
            throw new ArgumentOutOfRangeException(nameof(at), "Events must be ordered within the declared replay duration.");
        if (_events.Count >= MaximumEvents || _characters + text.Length + metadataCharacters > MaximumCharacters) throw new InvalidOperationException("Replay exceeds its 4096-event or 4 Mi-character payload budget.");
    }
    private static string BoundedText(string text, string name) {
        if (text == null) throw new ArgumentNullException(name);
        if (text.Length > 65536) throw new ArgumentOutOfRangeException(name, "Split output into events of at most 65536 UTF-16 characters.");
        return text;
    }
    private static string OneLine(string text, string name) => TerminalTextSanitizer.OneLine(BoundedText(text, name), name, "    ", false);
    private static string Heading(string text, string name) => VisualStorySurface.RequireHeading(OneLine(text, name), name);
    private static void ValidateTone(TerminalTextTone tone) { if (!Enum.IsDefined(typeof(TerminalTextTone), tone)) throw new ArgumentOutOfRangeException(nameof(tone)); }
    private static TerminalTab Tab(string id, string title, TerminalDialect dialect, string directory, string? customPrompt) {
        id = VisualStorySurface.RequireIdentifier(id, nameof(id));
        if (id.Length > 40) throw new ArgumentOutOfRangeException(nameof(id));
        if (!Enum.IsDefined(typeof(TerminalDialect), dialect)) throw new ArgumentOutOfRangeException(nameof(dialect));
        var prompt = customPrompt == null ? string.Empty : OneLine(customPrompt, nameof(customPrompt));
        if (dialect == TerminalDialect.Custom && prompt.Length == 0) throw new ArgumentException("A custom dialect requires its prompt.", nameof(customPrompt));
        return new TerminalTab(id, Heading(title, nameof(title)), dialect, OneLine(directory, nameof(directory)), prompt, TerminalTheme.GraphiteDark(), TerminalTabIcon.Terminal);
    }
}
