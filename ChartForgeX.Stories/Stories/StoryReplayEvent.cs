using System;
using ChartForgeX.Terminal;

namespace ChartForgeX.Stories;

/// <summary>Resolved changes in a recorded terminal session. These events never execute commands.</summary>
public enum StoryReplayEventKind {
    /// <summary>A submitted command, including its prompt.</summary>
    Command,
    /// <summary>One or more completed output lines.</summary>
    Output,
    /// <summary>Replaces the last displayed line, for resolved progress updates.</summary>
    ReplaceLine,
    /// <summary>Clears the active tab's visible history while retaining its transcript.</summary>
    Clear,
    /// <summary>Opens and activates a persistent tab.</summary>
    OpenTab,
    /// <summary>Activates an existing tab.</summary>
    SelectTab,
    /// <summary>Changes the active tab's working directory.</summary>
    Directory,
    /// <summary>Adds a chapter marker or explanation.</summary>
    Marker
}

/// <summary>An immutable replay observation with recorded and presentation timestamps.</summary>
public sealed class StoryReplayEvent {
    internal StoryReplayEvent(TimeSpan timestamp, TimeSpan? originalTimestamp, StoryReplayEventKind kind, string text,
        string tabId, TerminalTextTone tone = TerminalTextTone.Default, TerminalTab? tab = null) {
        Timestamp = timestamp; OriginalTimestamp = originalTimestamp; Kind = kind; Text = text; TabId = tabId; Tone = tone; Tab = tab;
    }
    /// <summary>Gets the event position in the edited presentation.</summary>
    public TimeSpan Timestamp { get; }
    /// <summary>Gets the unmodified position in the original recording, or null for an inserted explanation.</summary>
    public TimeSpan? OriginalTimestamp { get; }
    /// <summary>Gets the resolved operation.</summary>
    public StoryReplayEventKind Kind { get; }
    /// <summary>Gets sanitized text, a directory or a marker label.</summary>
    public string Text { get; }
    /// <summary>Gets the affected persistent tab identifier.</summary>
    public string TabId { get; }
    /// <summary>Gets the output tone.</summary>
    public TerminalTextTone Tone { get; }
    /// <summary>Gets the title of a tab-opening event.</summary>
    public string? TabTitle => Tab?.Title;
    /// <summary>Gets the dialect of a tab-opening event.</summary>
    public TerminalDialect? TabDialect => Tab?.Dialect;
    /// <summary>Gets the directory of a tab-opening event.</summary>
    public string? TabWorkingDirectory => Tab?.WorkingDirectory;
    /// <summary>Gets the custom prompt of a tab-opening event.</summary>
    public string? TabCustomPrompt => Tab?.CustomPrompt;
    internal TerminalTab? Tab { get; }
    internal StoryReplayEvent At(TimeSpan timestamp) => new(timestamp, OriginalTimestamp, Kind, Text, TabId, Tone, Tab);
}
