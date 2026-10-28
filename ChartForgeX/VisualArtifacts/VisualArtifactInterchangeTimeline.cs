using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Portable editable native timeline semantics and presentation.</summary>
/// <remarks>Version 2 retains native immutable values in detached host collections. Source-language facts,
/// locations and caller metadata remain in the envelope; importing does not reconstruct source text.</remarks>
public sealed class VisualArtifactInterchangeTimelineArtifact {
    /// <summary>Gets source-ordered sections, including repeated labels with distinct stable IDs.</summary>
    public List<TimelineSection> Sections { get; } = new();
    /// <summary>Gets ordered immutable native periods with their exact identities and section membership.</summary>
    public List<TimelinePeriod> Periods { get; } = new();
    /// <summary>Gets or sets the categorical reading direction; period labels remain literal text.</summary>
    public TimelineDiagramDirection Direction { get; set; }
    /// <summary>Gets or sets the immutable native theme, including typography and both palettes.</summary>
    public VisualTheme Theme { get; set; } = VisualTheme.Graphite();
    /// <summary>Gets or sets the native theme mode.</summary>
    public VisualThemeMode ThemeMode { get; set; }
    /// <summary>Gets or sets configured padding, retained while an embedding host owns the frame.</summary>
    public ChartPadding Padding { get; set; } = new(18, 16, 18, 12);
    /// <summary>Gets or sets whether the native background remains transparent.</summary>
    public bool TransparentBackground { get; set; }
    /// <summary>Gets or sets whether the outer native card is drawn.</summary>
    public bool ShowCard { get; set; } = true;
    /// <summary>Gets or sets whether an embedding host owns the frame.</summary>
    public bool HostOwnsFrame { get; set; }
    /// <summary>Gets or sets the output pixel multiplier, from one through four.</summary>
    public int PngOutputScale { get; set; } = 1;

    internal VisualArtifactInterchangeTimelineArtifact Clone() {
        var copy = new VisualArtifactInterchangeTimelineArtifact {
            Theme = Theme, ThemeMode = ThemeMode, Padding = Padding, TransparentBackground = TransparentBackground,
            ShowCard = ShowCard, HostOwnsFrame = HostOwnsFrame, PngOutputScale = PngOutputScale, Direction = Direction
        };
        copy.Sections.AddRange(Sections); copy.Periods.AddRange(Periods);
        return copy; // Native records, their ordered child lists and themes are immutable.
    }
}
