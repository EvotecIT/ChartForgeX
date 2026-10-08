using System;
using ChartForgeX.Core;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Compiles a model into a detached visual with completed layout.</summary>
public interface IVisualRenderable {
    /// <summary>Prepares geometry, text, semantics and diagnostics once for static export.</summary>
    PreparedVisual Prepare(VisualRenderContext context);
}

/// <summary>A positive logical viewport, independent of raster scale.</summary>
public readonly struct VisualSize {
    /// <summary>Creates a finite positive viewport.</summary>
    public VisualSize(double width, double height) {
        Positive(width, nameof(width)); Positive(height, nameof(height)); Width = width; Height = height;
    }
    /// <summary>Gets the logical width.</summary>
    public double Width { get; }
    /// <summary>Gets the logical height.</summary>
    public double Height { get; }
    internal static void Positive(double value, string name) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(name, "Value must be finite and positive.");
    }
}

/// <summary>The resolved viewport and outer padding used by every prepared visual.</summary>
public sealed class VisualLayoutOptions {
    /// <summary>Creates a fixed viewport. Hosts implement fit sizing by supplying their measured viewport.</summary>
    public VisualLayoutOptions(VisualSize size, double padding = 24) : this(size, ChartPadding.All(padding)) { }
    /// <summary>Creates a fixed viewport with independently configured outer edge insets.</summary>
    public VisualLayoutOptions(VisualSize size, ChartPadding padding) {
        VisualSize.Positive(size.Width, nameof(size)); VisualSize.Positive(size.Height, nameof(size));
        if (padding.Left + padding.Right >= size.Width || padding.Top + padding.Bottom >= size.Height)
            throw new ArgumentOutOfRangeException(nameof(padding));
        Size = size; PaddingEdges = padding;
    }
    /// <summary>Gets the resolved logical size.</summary>
    public VisualSize Size { get; }
    /// <summary>Gets the smallest outer edge inset. Uniform layouts return their configured scalar padding.</summary>
    /// <remarks>Use <see cref="PaddingEdges"/> when placing content; asymmetric layouts have four independent insets.</remarks>
    public double Padding => Math.Min(Math.Min(PaddingEdges.Left, PaddingEdges.Right), Math.Min(PaddingEdges.Top, PaddingEdges.Bottom));
    /// <summary>Gets the outer edge insets in logical units.</summary>
    public ChartPadding PaddingEdges { get; }
}

/// <summary>Common title, subtitle, legend and surface configuration.</summary>
public sealed class VisualFrame {
    private readonly TextStyle? _titleStyle, _subtitleStyle, _legendStyle;
    /// <summary>Creates an immutable frame configuration.</summary>
    /// <remarks>Omitted legend visibility and placement use the producer's data-aware policy and model configuration.
    /// Supply explicit values to override them, including <c>showLegend: true</c> for a redundant single-series legend.</remarks>
    public VisualFrame(string? title = null, string? subtitle = null, bool? showLegend = null,
        ChartLegendPosition? legendPosition = null, bool showSurface = false, bool transparentBackground = false,
        TextStyle? titleStyle = null, TextStyle? subtitleStyle = null, TextStyle? legendStyle = null,
        int? legendMaximumRows = null, double legendMaximumHeightFraction = 0.35, bool showCard = false, string? legendTitle = null) {
        if (legendPosition.HasValue && !Enum.IsDefined(typeof(ChartLegendPosition), legendPosition.Value)) throw new ArgumentOutOfRangeException(nameof(legendPosition));
        if (legendMaximumRows.HasValue && legendMaximumRows.Value < 1) throw new ArgumentOutOfRangeException(nameof(legendMaximumRows));
        if (double.IsNaN(legendMaximumHeightFraction) || double.IsInfinity(legendMaximumHeightFraction) || legendMaximumHeightFraction <= 0 || legendMaximumHeightFraction > 1)
            throw new ArgumentOutOfRangeException(nameof(legendMaximumHeightFraction));
        Title = title; Subtitle = subtitle; ShowLegend = showLegend ?? true; LegendPosition = legendPosition ?? ChartLegendPosition.TopLeft;
        HasExplicitLegend = showLegend.HasValue; HasExplicitLegendPosition = legendPosition.HasValue;
        ShowSurface = showSurface; TransparentBackground = transparentBackground;
        _titleStyle = titleStyle?.Clone(); _subtitleStyle = subtitleStyle?.Clone(); _legendStyle = legendStyle?.Clone();
        LegendMaximumRows = legendMaximumRows; LegendMaximumHeightFraction = legendMaximumHeightFraction;
        ShowCard = showCard;
        LegendTitle = legendTitle;
    }
    /// <summary>Gets the title.</summary>
    public string? Title { get; }
    /// <summary>Gets the subtitle.</summary>
    public string? Subtitle { get; }
    /// <summary>Gets the requested legend visibility. When omitted, the producer resolves its data-aware default.</summary>
    public bool ShowLegend { get; }
    internal bool HasExplicitLegend { get; }
    internal bool HasExplicitLegendPosition { get; }
    /// <summary>Gets an optional measured heading above the legend entries. Null uses the producer's source title.</summary>
    public string? LegendTitle { get; }
    /// <summary>Gets the legend placement.</summary>
    public ChartLegendPosition LegendPosition { get; }
    /// <summary>Gets whether the content surface is filled.</summary>
    public bool ShowSurface { get; }
    /// <summary>Gets whether the complete frame has an elevated card, independently of the content surface.</summary>
    public bool ShowCard { get; }
    /// <summary>Gets whether the outer canvas has no background paint, for overlays and embedding.</summary>
    public bool TransparentBackground { get; }
    /// <summary>Gets a defensive copy of the explicit heading typography.</summary>
    public TextStyle? TitleStyle => _titleStyle?.Clone();
    /// <summary>Gets a defensive copy of the explicit subtitle typography.</summary>
    public TextStyle? SubtitleStyle => _subtitleStyle?.Clone();
    /// <summary>Gets a defensive copy of the explicit legend typography.</summary>
    public TextStyle? LegendStyle => _legendStyle?.Clone();
    /// <summary>Gets the additional legend row limit, including any overflow summary row. Null uses only the height budget.</summary>
    public int? LegendMaximumRows { get; }
    /// <summary>Gets the maximum fraction of the full viewport height occupied by the legend and its spacing.</summary>
    public double LegendMaximumHeightFraction { get; }
    internal VisualFrame WithHeadings(string? title, string? subtitle) => new(title, subtitle, HasExplicitLegend ? ShowLegend : null, HasExplicitLegendPosition ? LegendPosition : null,
        ShowSurface, TransparentBackground, _titleStyle, _subtitleStyle, _legendStyle, LegendMaximumRows, LegendMaximumHeightFraction, ShowCard, LegendTitle);
    internal VisualFrame WithLegendTitle(string? legendTitle) => new(Title, Subtitle, HasExplicitLegend ? ShowLegend : null, HasExplicitLegendPosition ? LegendPosition : null,
        ShowSurface, TransparentBackground, _titleStyle, _subtitleStyle, _legendStyle, LegendMaximumRows, LegendMaximumHeightFraction, ShowCard, legendTitle);
}

/// <summary>A complete immutable request for shared static rendering.</summary>
public sealed class VisualRenderContext {
    private readonly FontSpec _font;
    /// <summary>Creates a context with the canonical paired palette and Graphite geometry by default.</summary>
    public VisualRenderContext(VisualLayoutOptions? layout = null, VisualTheme? theme = null,
        VisualThemeMode themeMode = VisualThemeMode.Light, VisualFrame? frame = null, FontSpec? font = null) {
        if (!Enum.IsDefined(typeof(VisualThemeMode), themeMode)) throw new ArgumentOutOfRangeException(nameof(themeMode));
        Layout = layout ?? new VisualLayoutOptions(new VisualSize(640, 400));
        Theme = theme ?? VisualTheme.Graphite(); ThemeMode = themeMode; Frame = frame ?? new VisualFrame();
        _font = font?.Clone() ?? new FontSpec { Family = Theme.Typography.Family };
    }
    /// <summary>Gets the resolved layout.</summary>
    public VisualLayoutOptions Layout { get; }
    /// <summary>Gets the paired theme.</summary>
    public VisualTheme Theme { get; }
    /// <summary>Gets the selected palette mode.</summary>
    public VisualThemeMode ThemeMode { get; }
    /// <summary>Gets the common frame.</summary>
    public VisualFrame Frame { get; }
    /// <summary>Gets an independent copy of the font request.</summary>
    public FontSpec Font => _font.Clone();
}
