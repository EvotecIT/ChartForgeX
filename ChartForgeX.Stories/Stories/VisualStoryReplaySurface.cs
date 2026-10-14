using ChartForgeX.Themes;
using System;
using ChartForgeX.Terminal;

namespace ChartForgeX.Stories;

/// <summary>Shows sanitized recorded events on the scene clock in a bounded terminal viewport.</summary>
public sealed class VisualStoryReplaySurface : VisualStorySurface {
    private readonly string _heading;
    /// <summary>Creates a replay surface with optional accessibility heading, viewport and terminal palette. An omitted palette inherits the containing story theme.</summary>
    public VisualStoryReplaySurface(StoryReplay replay, string? accessibleText = null, VisualStoryTerminalOptions? options = null, VisualTheme? theme = null, VisualThemeMode themeMode = VisualThemeMode.Dark, TerminalStyleOptions? style = null)
        : base(VisualStorySurfaceKind.Terminal, Transcript(replay, accessibleText), preserveAccessibleWhitespace: true) {
        Replay = replay ?? throw new ArgumentNullException(nameof(replay)); Options = options ?? new VisualStoryTerminalOptions();
        if (!Enum.IsDefined(typeof(VisualThemeMode), themeMode)) throw new ArgumentOutOfRangeException(nameof(themeMode));
        Theme = theme; ThemeMode = themeMode; Style = style?.Clone() ?? new TerminalStyleOptions(); _heading = accessibleText ?? string.Empty;
    }
    /// <summary>Gets the resolved replay, which does not execute commands.</summary>
    public StoryReplay Replay { get; }
    /// <summary>Gets fixed viewport settings.</summary>
    public VisualStoryTerminalOptions Options { get; }
    /// <summary>Gets the explicit terminal palette, or null to inherit the containing story theme independently of its window chrome.</summary>
    public VisualTheme? Theme { get; }
    /// <summary>Gets the explicit appearance for the optional replay theme.</summary>
    public VisualThemeMode ThemeMode { get; }
    /// <summary>Gets local replay title-bar and cursor styling.</summary>
    public TerminalStyleOptions Style { get; }
    /// <summary>Gets the complete sanitized replay transcript.</summary>
    public override string AccessibleText => Transcript(Replay, _heading);
    internal VisualStoryReplaySurface Capture() => new(Replay.Capture(), _heading, Options, Theme, ThemeMode, Style);
    private static string Transcript(StoryReplay replay, string? heading) {
        if (replay == null) throw new ArgumentNullException(nameof(replay));
        return string.IsNullOrWhiteSpace(heading) ? replay.ToTranscript() : RequireText(heading!, nameof(heading)) + Environment.NewLine + replay.ToTranscript();
    }
}
