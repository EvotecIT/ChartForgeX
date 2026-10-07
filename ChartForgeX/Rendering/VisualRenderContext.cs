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
    public VisualLayoutOptions(VisualSize size, double padding = 24) {
        VisualSize.Positive(size.Width, nameof(size)); VisualSize.Positive(size.Height, nameof(size));
        if (double.IsNaN(padding) || double.IsInfinity(padding) || padding < 0 || padding * 2 >= Math.Min(size.Width, size.Height))
            throw new ArgumentOutOfRangeException(nameof(padding));
        Size = size; Padding = padding;
    }
    /// <summary>Gets the resolved logical size.</summary>
    public VisualSize Size { get; }
    /// <summary>Gets the outer padding in logical units.</summary>
    public double Padding { get; }
}

/// <summary>Common title, subtitle, legend and surface configuration.</summary>
public sealed class VisualFrame {
    private readonly TextStyle? _titleStyle, _subtitleStyle, _legendStyle;
    /// <summary>Creates an immutable frame configuration.</summary>
    public VisualFrame(string? title = null, string? subtitle = null, bool showLegend = true,
        ChartLegendPosition legendPosition = ChartLegendPosition.Bottom, bool showSurface = false, bool transparentBackground = false,
        TextStyle? titleStyle = null, TextStyle? subtitleStyle = null, TextStyle? legendStyle = null) {
        if (!Enum.IsDefined(typeof(ChartLegendPosition), legendPosition)) throw new ArgumentOutOfRangeException(nameof(legendPosition));
        Title = title; Subtitle = subtitle; ShowLegend = showLegend; LegendPosition = legendPosition; ShowSurface = showSurface; TransparentBackground = transparentBackground;
        _titleStyle = titleStyle?.Clone(); _subtitleStyle = subtitleStyle?.Clone(); _legendStyle = legendStyle?.Clone();
    }
    /// <summary>Gets the title.</summary>
    public string? Title { get; }
    /// <summary>Gets the subtitle.</summary>
    public string? Subtitle { get; }
    /// <summary>Gets whether the legend is shown.</summary>
    public bool ShowLegend { get; }
    /// <summary>Gets the legend placement.</summary>
    public ChartLegendPosition LegendPosition { get; }
    /// <summary>Gets whether the content surface is filled.</summary>
    public bool ShowSurface { get; }
    /// <summary>Gets whether the outer canvas has no background paint, for overlays and embedding.</summary>
    public bool TransparentBackground { get; }
    /// <summary>Gets a defensive copy of the explicit heading typography.</summary>
    public TextStyle? TitleStyle => _titleStyle?.Clone();
    /// <summary>Gets a defensive copy of the explicit subtitle typography.</summary>
    public TextStyle? SubtitleStyle => _subtitleStyle?.Clone();
    /// <summary>Gets a defensive copy of the explicit legend typography.</summary>
    public TextStyle? LegendStyle => _legendStyle?.Clone();
    internal VisualFrame WithHeadings(string? title, string? subtitle) => new(title, subtitle, ShowLegend, LegendPosition,
        ShowSurface, TransparentBackground, _titleStyle, _subtitleStyle, _legendStyle);
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
