using System;
using ChartForgeX.Terminal;

namespace ChartForgeX.Stories;

/// <summary>Displays exact source text with optional renderer-neutral syntax spans.</summary>
public sealed class VisualStorySourceSurface : VisualStorySurface {
    private readonly string _caption;

    /// <summary>Initializes a source surface.</summary>
    public VisualStorySourceSurface(StorySourceText source, string? caption = null)
        : base(VisualStorySurfaceKind.Source, AccessibleSourceText(source, caption), preserveAccessibleWhitespace: true) {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        _caption = string.IsNullOrWhiteSpace(caption)
            ? string.Empty
            : RequireText(caption!, nameof(caption));
    }

    /// <summary>Gets the exact source and semantic syntax spans.</summary>
    public StorySourceText Source { get; }

    /// <summary>Gets accessibility text derived from the current source metadata.</summary>
    public override string AccessibleText => AccessibleSourceText(Source, _caption);

    private static string AccessibleSourceText(StorySourceText source, string? caption) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var accessibleHeading = string.IsNullOrWhiteSpace(caption)
            ? string.Empty
            : RequireText(caption!, nameof(caption));
        if (source.Language.Length > 0) {
            if (accessibleHeading.Length > 0) accessibleHeading += Environment.NewLine;
            accessibleHeading += "Language: " + source.Language;
        }
        return accessibleHeading.Length == 0
            ? source.Text
            : accessibleHeading + Environment.NewLine + source.Text;
    }
}

/// <summary>Displays a deterministic terminal story without executing its commands.</summary>
public sealed class VisualStoryTerminalSurface : VisualStorySurface {
    private readonly string _accessibleHeading;

    /// <summary>Initializes a terminal surface.</summary>
    public VisualStoryTerminalSurface(TerminalStory terminal, string? accessibleText = null)
        : base(
            VisualStorySurfaceKind.Terminal,
            AccessibleTerminalText(terminal, accessibleText),
            preserveAccessibleWhitespace: true) {
        Terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _accessibleHeading = string.IsNullOrWhiteSpace(accessibleText)
            ? string.Empty
            : RequireText(accessibleText!, nameof(accessibleText));
    }

    /// <summary>Gets the resolved terminal presentation.</summary>
    public TerminalStory Terminal { get; }

    /// <summary>Gets an accessibility transcript derived from the current terminal state.</summary>
    public override string AccessibleText => AccessibleTerminalText(Terminal, _accessibleHeading);

    private static string AccessibleTerminalText(TerminalStory terminal, string? heading) {
        if (terminal == null) throw new ArgumentNullException(nameof(terminal));
        var transcript = string.Join(Environment.NewLine, TerminalStoryLayout.Build(terminal).TranscriptLines);
        if (string.IsNullOrWhiteSpace(heading)) return transcript;
        return RequireText(heading!, nameof(heading)) + Environment.NewLine + transcript;
    }
}
