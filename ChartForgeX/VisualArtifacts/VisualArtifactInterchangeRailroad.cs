using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Portable railroad rules and native block presentation, using the existing immutable grammar expression model.</summary>
/// <remarks>This optional version-1 envelope family requires a reader with railroad support. Earlier readers reject the
/// unknown family token; they do not reconstruct a grammar from rendered SVG. Authoring dialect metadata remains in the
/// envelope's extension bag; these semantics do not reconstruct grammar text or source locations.</remarks>
public sealed class VisualArtifactInterchangeRailroadArtifact {
    /// <summary>Gets rules in authored order. Rule IDs, labels and immutable expressions are retained without expansion.</summary>
    public List<RailroadRule> Rules { get; } = new();
    /// <summary>Gets or sets the comparison policy used for rule identities.</summary>
    public RailroadRuleComparison RuleComparison { get; set; }
    /// <summary>Gets or sets the immutable native theme, including typography and both palettes.</summary>
    public VisualTheme Theme { get; set; } = VisualTheme.Graphite();
    /// <summary>Gets or sets the native theme mode.</summary>
    public VisualThemeMode ThemeMode { get; set; }
    /// <summary>Gets or sets the effective native content padding.</summary>
    public ChartPadding Padding { get; set; } = new(18, 16, 18, 12);
    /// <summary>Gets or sets whether the full background is transparent.</summary>
    public bool TransparentBackground { get; set; }
    /// <summary>Gets or sets whether the outer card is drawn.</summary>
    public bool ShowCard { get; set; } = true;
    /// <summary>Gets or sets whether the embedding host owns the frame.</summary>
    public bool HostOwnsFrame { get; set; }
    /// <summary>Gets or sets the native PNG output scale, from one through four.</summary>
    public int PngOutputScale { get; set; } = 1;

    internal VisualArtifactInterchangeRailroadArtifact Clone() {
        var copy = new VisualArtifactInterchangeRailroadArtifact {
            RuleComparison = RuleComparison, Theme = Theme, ThemeMode = ThemeMode, Padding = Padding,
            TransparentBackground = TransparentBackground, ShowCard = ShowCard, HostOwnsFrame = HostOwnsFrame,
            PngOutputScale = PngOutputScale
        };
        copy.Rules.AddRange(Rules); // Rules and their expressions are immutable; the host collection is detached.
        return copy;
    }
}
