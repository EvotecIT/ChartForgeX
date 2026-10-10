using System;
using ChartForgeX.Terminal;

namespace ChartForgeX.Stories;

/// <summary>Shows sanitized recorded events on the scene clock in a bounded terminal viewport.</summary>
public sealed class VisualStoryReplaySurface : VisualStorySurface {
    private readonly string _heading;
    /// <summary>Creates a replay surface with optional accessibility heading, viewport and terminal palette.</summary>
    public VisualStoryReplaySurface(StoryReplay replay, string? accessibleText = null, VisualStoryTerminalOptions? options = null, TerminalTheme? theme = null)
        : base(VisualStorySurfaceKind.Terminal, Transcript(replay, accessibleText), preserveAccessibleWhitespace: true) {
        Replay = replay ?? throw new ArgumentNullException(nameof(replay)); Options = options ?? new VisualStoryTerminalOptions();
        UsesStoryTheme = theme == null;
        Theme = theme ?? TerminalTheme.GraphiteDark(); _heading = accessibleText ?? string.Empty;
    }
    /// <summary>Gets the resolved replay, which does not execute commands.</summary>
    public StoryReplay Replay { get; }
    /// <summary>Gets fixed viewport settings.</summary>
    public VisualStoryTerminalOptions Options { get; }
    /// <summary>Gets the terminal palette. An omitted palette follows desktop story themes during rendering.</summary>
    public TerminalTheme Theme { get; }
    /// <summary>Gets the complete sanitized replay transcript.</summary>
    public override string AccessibleText => Transcript(Replay, _heading);
    internal bool UsesStoryTheme { get; }
    internal VisualStoryReplaySurface Capture() => new(Replay.Capture(), _heading, Options, Theme.Copy(), UsesStoryTheme);
    private VisualStoryReplaySurface(StoryReplay replay, string heading, VisualStoryTerminalOptions options, TerminalTheme theme, bool usesStoryTheme)
        : this(replay, heading, options, theme) {
        UsesStoryTheme = usesStoryTheme;
    }
    private static string Transcript(StoryReplay replay, string? heading) {
        if (replay == null) throw new ArgumentNullException(nameof(replay));
        return string.IsNullOrWhiteSpace(heading) ? replay.ToTranscript() : RequireText(heading!, nameof(heading)) + Environment.NewLine + replay.ToTranscript();
    }
}
