using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Portable native directory rows and presentation, using immutable TreeViewItem values.</summary>
/// <remarks>This optional version-1 family requires a tree-aware reader. Older readers reject the unknown family;
/// they do not reconstruct editable rows from SVG. Source-language facts and caller metadata remain in the envelope.
/// Item IDs obey the existing interchange 512-character limit, which is stricter than the native authoring limit.</remarks>
public sealed class VisualArtifactInterchangeTreeViewArtifact {
    /// <summary>Gets immutable rows in contiguous parent-before-child preorder.</summary>
    public List<TreeViewItem> Items { get; } = new();
    /// <summary>Gets or sets the immutable native theme, including typography and both palettes.</summary>
    public VisualTheme Theme { get; set; } = VisualTheme.Graphite();
    /// <summary>Gets or sets the native theme mode.</summary>
    public VisualThemeMode ThemeMode { get; set; }
    /// <summary>Gets or sets configured content padding, retained even while the host owns the frame.</summary>
    public ChartPadding Padding { get; set; } = new(18, 16, 18, 12);
    /// <summary>Gets or sets whether the native background is transparent.</summary>
    public bool TransparentBackground { get; set; }
    /// <summary>Gets or sets whether the native card surface is painted.</summary>
    public bool ShowCard { get; set; } = true;
    /// <summary>Gets or sets whether the host owns the frame and suppresses its padding and surface.</summary>
    public bool HostOwnsFrame { get; set; }
    /// <summary>Gets or sets the PNG output scale, from one through four.</summary>
    public int PngOutputScale { get; set; } = 1;
    /// <summary>Gets or sets whether automatic native file/folder icons are enabled.</summary>
    public bool ShowIcons { get; set; }
    /// <summary>Gets or sets logical indentation per level, from four through 256 pixels.</summary>
    public double RowIndent { get; set; } = 18;
    /// <summary>Gets or sets logical vertical padding on each side of a row, from zero through 64 pixels.</summary>
    public double RowPadding { get; set; } = 5;

    internal VisualArtifactInterchangeTreeViewArtifact Clone() {
        var copy = new VisualArtifactInterchangeTreeViewArtifact {
            Theme = Theme, ThemeMode = ThemeMode, Padding = Padding, TransparentBackground = TransparentBackground,
            ShowCard = ShowCard, HostOwnsFrame = HostOwnsFrame, PngOutputScale = PngOutputScale,
            ShowIcons = ShowIcons, RowIndent = RowIndent, RowPadding = RowPadding
        };
        copy.Items.AddRange(Items); // Native rows, their class collections and the theme are immutable.
        return copy;
    }
}
