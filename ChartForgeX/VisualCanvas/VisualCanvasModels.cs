using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;

namespace ChartForgeX.Composition;

/// <summary>
/// Built-in background treatment for fixed-size visual canvases.
/// </summary>
public enum VisualCanvasBackdropStyle {
    /// <summary>Render no canvas background so layers can float over an external image.</summary>
    Transparent,
    /// <summary>Render only the configured background colors.</summary>
    Plain,
    /// <summary>Render a quiet technology-themed wallpaper backdrop.</summary>
    TechHorizon
}

/// <summary>
/// Horizontal text alignment used by visual canvas layers.
/// </summary>
public enum VisualCanvasTextAlignment {
    /// <summary>Align text to the leading edge.</summary>
    Left,
    /// <summary>Center text in the available width.</summary>
    Center,
    /// <summary>Align text to the trailing edge.</summary>
    Right
}

/// <summary>
/// Defines the reference point used when placing visual canvas layers.
/// </summary>
public enum VisualCanvasAnchor {
    /// <summary>Place from the top-left edge.</summary>
    TopLeft,
    /// <summary>Place from the top-center edge.</summary>
    TopCenter,
    /// <summary>Place from the top-right edge.</summary>
    TopRight,
    /// <summary>Place from the middle-left edge.</summary>
    MiddleLeft,
    /// <summary>Place from the center of the container.</summary>
    Center,
    /// <summary>Place from the middle-right edge.</summary>
    MiddleRight,
    /// <summary>Place from the bottom-left edge.</summary>
    BottomLeft,
    /// <summary>Place from the bottom-center edge.</summary>
    BottomCenter,
    /// <summary>Place from the bottom-right edge.</summary>
    BottomRight
}

/// <summary>
/// Describes reusable anchor-based placement inside a canvas or another rectangular region.
/// </summary>
public readonly struct VisualCanvasPlacement {
    /// <summary>Initializes an anchor placement.</summary>
    public VisualCanvasPlacement(VisualCanvasAnchor anchor, double offsetX = 0, double offsetY = 0) {
        VisualCanvas.ValidateEnum(anchor, nameof(anchor));
        ValidateFinite(offsetX, nameof(offsetX));
        ValidateFinite(offsetY, nameof(offsetY));
        Anchor = anchor;
        OffsetX = offsetX;
        OffsetY = offsetY;
    }

    /// <summary>Gets the anchor used as the placement reference.</summary>
    public VisualCanvasAnchor Anchor { get; }

    /// <summary>Gets the horizontal offset. For right anchors, positive values inset from the right edge.</summary>
    public double OffsetX { get; }

    /// <summary>Gets the vertical offset. For bottom anchors, positive values inset from the bottom edge.</summary>
    public double OffsetY { get; }

    /// <summary>Creates an anchor placement.</summary>
    public static VisualCanvasPlacement At(VisualCanvasAnchor anchor, double offsetX = 0, double offsetY = 0) => new(anchor, offsetX, offsetY);

    /// <summary>Resolves this placement inside a zero-origin container.</summary>
    public ChartRect Resolve(double containerWidth, double containerHeight, double width, double height) => Resolve(new ChartRect(0, 0, containerWidth, containerHeight), width, height);

    /// <summary>Resolves this placement inside another rectangular region.</summary>
    public ChartRect Resolve(ChartRect container, double width, double height) {
        ValidatePositive(width, nameof(width));
        ValidatePositive(height, nameof(height));
        var x = ResolveX(container, width);
        var y = ResolveY(container, height);
        return new ChartRect(x, y, width, height);
    }

    private double ResolveX(ChartRect container, double width) {
        switch (Anchor) {
            case VisualCanvasAnchor.TopRight:
            case VisualCanvasAnchor.MiddleRight:
            case VisualCanvasAnchor.BottomRight:
                return container.Right - width - OffsetX;
            case VisualCanvasAnchor.TopCenter:
            case VisualCanvasAnchor.Center:
            case VisualCanvasAnchor.BottomCenter:
                return container.X + (container.Width - width) / 2 + OffsetX;
            default:
                return container.X + OffsetX;
        }
    }

    private double ResolveY(ChartRect container, double height) {
        switch (Anchor) {
            case VisualCanvasAnchor.BottomLeft:
            case VisualCanvasAnchor.BottomCenter:
            case VisualCanvasAnchor.BottomRight:
                return container.Bottom - height - OffsetY;
            case VisualCanvasAnchor.MiddleLeft:
            case VisualCanvasAnchor.Center:
            case VisualCanvasAnchor.MiddleRight:
                return container.Y + (container.Height - height) / 2 + OffsetY;
            default:
                return container.Y + OffsetY;
        }
    }

    private static void ValidateFinite(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(parameterName, value, "Placement values must be finite.");
    }

    private static void ValidatePositive(double value, string parameterName) {
        ValidateFinite(value, parameterName);
        if (value <= 0) throw new ArgumentOutOfRangeException(parameterName, value, "Placement dimensions must be greater than zero.");
    }
}

/// <summary>
/// Defines how source images are placed inside a visual canvas image layer.
/// </summary>
public enum VisualCanvasImageFit {
    /// <summary>Scale the image independently in both axes to fill the destination rectangle.</summary>
    Stretch,
    /// <summary>Scale the image uniformly so the entire source is visible inside the destination rectangle.</summary>
    Contain,
    /// <summary>Scale and crop the image uniformly so the destination rectangle is fully covered.</summary>
    Cover,
    /// <summary>Draw the image at its source pixel size, centered inside the destination rectangle.</summary>
    Center,
    /// <summary>Repeat the image at its source pixel size, clipped to the destination rectangle.</summary>
    Tile
}

/// <summary>
/// Surface treatment for visual canvas information tiles.
/// </summary>
public enum VisualCanvasInfoTileSurfaceStyle {
    /// <summary>Render a translucent filled panel with an accent border.</summary>
    Glass,
    /// <summary>Render only the tile border and contents over the underlying image.</summary>
    Outline,
    /// <summary>Render a raised panel with depth, glow, and stronger edge highlights.</summary>
    Raised
}

/// <summary>
/// Built-in icon treatment for visual canvas information tiles.
/// </summary>
public enum VisualCanvasInfoTileIconKind {
    /// <summary>Render the tile icon as text.</summary>
    Text,
    /// <summary>Render a computer monitor icon.</summary>
    Computer,
    /// <summary>Render a network/globe icon.</summary>
    Network,
    /// <summary>Render an operating system/window icon.</summary>
    OperatingSystem,
    /// <summary>Render a processor icon.</summary>
    Cpu,
    /// <summary>Render a memory module icon.</summary>
    Memory,
    /// <summary>Render a user icon.</summary>
    User,
    /// <summary>Render a domain/building icon.</summary>
    Domain,
    /// <summary>Render a terminal prompt icon.</summary>
    Terminal,
    /// <summary>Render a storage cylinder icon.</summary>
    Storage,
    /// <summary>Render a shield icon.</summary>
    Shield
}

/// <summary>
/// Compact chart treatment for visual canvas information tiles.
/// </summary>
public enum VisualCanvasInfoTileMiniChartKind {
    /// <summary>Render no mini chart.</summary>
    None,
    /// <summary>Render a compact sparkline on the right side of the tile.</summary>
    Sparkline,
    /// <summary>Render a compact area sparkline on the right side of the tile.</summary>
    Area,
    /// <summary>Render compact bars on the right side of the tile.</summary>
    Bars
}

/// <summary>
/// Theme colors for reusable visual canvas layers.
/// </summary>
public sealed class VisualCanvasTheme {
    /// <summary>Gets or sets the primary accent used by built-in decorative elements.</summary>
    public ChartColor Accent { get; set; } = ChartColor.FromHex("#2F80FF");
    /// <summary>Gets or sets the secondary accent used by hero badges and backdrop highlights.</summary>
    public ChartColor SecondaryAccent { get; set; } = ChartColor.FromHex("#22A7FF");
    /// <summary>Gets or sets the first default hero title color.</summary>
    public ChartColor HeroTitleColor { get; set; } = ChartColor.FromHex("#F8FAFC");
    /// <summary>Gets or sets the secondary default hero title color.</summary>
    public ChartColor HeroTitleAccentColor { get; set; } = ChartColor.FromHex("#2F80FF");
    /// <summary>Gets or sets the subtitle text color.</summary>
    public ChartColor SubtitleColor { get; set; } = ChartColor.FromHex("#D8E3F4");
    /// <summary>Gets or sets the glass tile top color.</summary>
    public ChartColor TileGlassTop { get; set; } = ChartColor.FromRgba(7, 21, 44, 232);
    /// <summary>Gets or sets the glass tile bottom color.</summary>
    public ChartColor TileGlassBottom { get; set; } = ChartColor.FromRgba(3, 10, 23, 224);
    /// <summary>Gets or sets the tile inner highlight stroke color.</summary>
    public ChartColor TileInnerStroke { get; set; } = ChartColor.White.WithOpacity(0.07);
    /// <summary>Gets or sets the tile label text color.</summary>
    public ChartColor TileLabelColor { get; set; } = ChartColor.FromHex("#C4D4EC");
    /// <summary>Gets or sets the tile primary value color.</summary>
    public ChartColor TileValueColor { get; set; } = ChartColor.FromHex("#F8FAFC");
    /// <summary>Gets or sets the tile detail color.</summary>
    public ChartColor TileDetailColor { get; set; } = ChartColor.FromHex("#A8BAD4");
    /// <summary>Gets or sets the progress rail color.</summary>
    public ChartColor TileProgressTrackColor { get; set; } = ChartColor.FromHex("#18345D").WithOpacity(0.92);
    /// <summary>Gets or sets the mini-chart plot fill color.</summary>
    public ChartColor TileMiniChartFillColor { get; set; } = ChartColor.FromHex("#2F80FF").WithOpacity(0.18);
    /// <summary>Gets or sets the mini-chart track/grid color.</summary>
    public ChartColor TileMiniChartTrackColor { get; set; } = ChartColor.FromHex("#8AA9D6").WithOpacity(0.18);
    /// <summary>Gets or sets the hero badge glow color.</summary>
    public ChartColor HeroBadgeGlowColor { get; set; } = ChartColor.FromHex("#22A7FF").WithOpacity(0.12);
    /// <summary>Gets or sets the hero badge top fill color.</summary>
    public ChartColor HeroBadgeTop { get; set; } = ChartColor.FromHex("#0B1C3A");
    /// <summary>Gets or sets the hero badge bottom fill color.</summary>
    public ChartColor HeroBadgeBottom { get; set; } = ChartColor.FromHex("#051021");
    /// <summary>Gets or sets the hero badge symbol color.</summary>
    public ChartColor HeroBadgeTextColor { get; set; } = ChartColor.FromHex("#E8F1FF");
    /// <summary>Gets or sets the image placeholder fill color.</summary>
    public ChartColor ImagePlaceholderFill { get; set; } = ChartColor.FromHex("#0B1B34").WithOpacity(0.68);
    /// <summary>Gets or sets the image placeholder stroke color.</summary>
    public ChartColor ImagePlaceholderStroke { get; set; } = ChartColor.FromHex("#2F80FF").WithOpacity(0.34);
    /// <summary>Gets or sets the feature-strip divider color.</summary>
    public ChartColor FeatureDividerColor { get; set; } = ChartColor.FromHex("#8AA9D6").WithOpacity(0.22);
    /// <summary>Gets or sets the feature-strip label color.</summary>
    public ChartColor FeatureLabelColor { get; set; } = ChartColor.FromHex("#D7E4F8");
    /// <summary>Gets or sets the tech backdrop horizon fill color.</summary>
    public ChartColor TechHorizonFill { get; set; } = ChartColor.FromRgba(6, 18, 37, 198);

    /// <summary>Creates a shallow copy of this theme.</summary>
    public VisualCanvasTheme Clone() => (VisualCanvasTheme)MemberwiseClone();
}

/// <summary>
/// A fixed-size layered visual surface for wallpapers, social images, report covers, and hero graphics.
/// </summary>
public sealed class VisualCanvas {
    private readonly List<VisualCanvasLayer> _layers = new();
    private VisualCanvasTheme _theme = new();
    private int _width;
    private int _height;
    private int _pngOutputScale = 1;
    private string _title = "ChartForgeX visual canvas";

    private VisualCanvas(int width, int height) {
        Width = width;
        Height = height;
    }

    /// <summary>Gets or sets the canvas width in pixels.</summary>
    public int Width {
        get => _width;
        set {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Visual canvas width must be positive.");
            _width = value;
        }
    }

    /// <summary>Gets or sets the canvas height in pixels.</summary>
    public int Height {
        get => _height;
        set {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Visual canvas height must be positive.");
            _height = value;
        }
    }

    /// <summary>Gets or sets the accessibility title used by SVG output.</summary>
    public string Title { get => _title; set => _title = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the top background color.</summary>
    public ChartColor BackgroundTop { get; set; } = ChartColor.FromHex("#030712");

    /// <summary>Gets or sets the bottom background color.</summary>
    public ChartColor BackgroundBottom { get; set; } = ChartColor.FromHex("#07182F");

    /// <summary>Gets or sets the optional backdrop decoration style.</summary>
    public VisualCanvasBackdropStyle BackdropStyle { get; set; } = VisualCanvasBackdropStyle.Plain;

    /// <summary>Gets or sets the theme used by built-in visual canvas layers.</summary>
    public VisualCanvasTheme Theme { get => _theme; set => _theme = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the PNG output pixel multiplier.</summary>
    public int PngOutputScale {
        get => _pngOutputScale;
        set {
            if (value < 1 || value > 4) throw new ArgumentOutOfRangeException(nameof(value), value, "PNG output scale must be between one and four.");
            _pngOutputScale = value;
        }
    }

    /// <summary>Gets the ordered visual canvas layers.</summary>
    public IReadOnlyList<VisualCanvasLayer> Layers => _layers;

    /// <summary>Creates a fixed-size visual canvas.</summary>
    public static VisualCanvas Create(int width, int height) => new(width, height);

    /// <summary>Creates a 1200 by 630 social preview canvas.</summary>
    public static VisualCanvas CreateSocialPreview() => new(1200, 630);

    /// <summary>Creates a 1920 by 1080 desktop wallpaper canvas.</summary>
    public static VisualCanvas CreateDesktopWallpaper() => new(1920, 1080);

    /// <summary>Sets the accessibility title used by SVG output.</summary>
    public VisualCanvas WithTitle(string title) { Title = title ?? throw new ArgumentNullException(nameof(title)); return this; }

    /// <summary>Sets a solid background color.</summary>
    public VisualCanvas WithBackground(ChartColor color) { BackgroundTop = color; BackgroundBottom = color; return this; }

    /// <summary>Sets a vertical background gradient.</summary>
    public VisualCanvas WithBackground(ChartColor top, ChartColor bottom) { BackgroundTop = top; BackgroundBottom = bottom; return this; }

    /// <summary>Sets the built-in backdrop decoration style.</summary>
    public VisualCanvas WithBackdrop(VisualCanvasBackdropStyle style) { ValidateEnum(style, nameof(style)); BackdropStyle = style; return this; }

    /// <summary>Sets the theme used by built-in visual canvas layers.</summary>
    public VisualCanvas WithTheme(VisualCanvasTheme theme) { Theme = theme ?? throw new ArgumentNullException(nameof(theme)); return this; }

    /// <summary>Sets the PNG output pixel multiplier.</summary>
    public VisualCanvas WithPngOutputScale(int scale) { PngOutputScale = scale; return this; }

    /// <summary>Adds a layer to the canvas.</summary>
    public VisualCanvas AddLayer(VisualCanvasLayer layer) {
        if (layer == null) throw new ArgumentNullException(nameof(layer));
        _layers.Add(layer);
        return this;
    }

    /// <summary>Resolves anchor-based placement against the full canvas.</summary>
    public ChartRect ResolvePlacement(VisualCanvasPlacement placement, double width, double height) => placement.Resolve(Width, Height, width, height);

    /// <summary>Adds a plain text layer.</summary>
    public VisualCanvas AddText(double x, double y, double width, string text, double fontSize, ChartColor color, VisualCanvasTextAlignment alignment = VisualCanvasTextAlignment.Left, bool emphasized = false) =>
        AddLayer(new VisualCanvasTextLayer(x, y, width, text, fontSize, color) { Alignment = alignment, Emphasized = emphasized });

    /// <summary>Adds a plain text layer using anchor-based placement.</summary>
    public VisualCanvas AddText(VisualCanvasPlacement placement, double width, string text, double fontSize, ChartColor color, VisualCanvasTextAlignment alignment = VisualCanvasTextAlignment.Left, bool emphasized = false) {
        var bounds = ResolvePlacement(placement, width, Math.Max(1, fontSize * 1.25));
        return AddText(bounds.X, bounds.Y, bounds.Width, text, fontSize, color, alignment, emphasized);
    }

    /// <summary>Adds a multi-color hero title layer.</summary>
    public VisualCanvas AddHeroTitle(double x, double y, double width, double fontSize, IEnumerable<VisualCanvasTextRun> runs, VisualCanvasTextAlignment alignment = VisualCanvasTextAlignment.Center) =>
        AddLayer(new VisualCanvasHeroTitleLayer(x, y, width, fontSize, runs) { Alignment = alignment });

    /// <summary>Adds a multi-color hero title layer using anchor-based placement.</summary>
    public VisualCanvas AddHeroTitle(VisualCanvasPlacement placement, double width, double fontSize, IEnumerable<VisualCanvasTextRun> runs, VisualCanvasTextAlignment alignment = VisualCanvasTextAlignment.Center) {
        var bounds = ResolvePlacement(placement, width, Math.Max(1, fontSize * 1.25));
        return AddHeroTitle(bounds.X, bounds.Y, bounds.Width, fontSize, runs, alignment);
    }

    /// <summary>Adds a key/value text block with measured columns and wrapped values.</summary>
    public VisualCanvas AddKeyValueBlock(double x, double y, double width, IEnumerable<VisualCanvasKeyValueItem> items, double labelFontSize = 16, double valueFontSize = 16, double columnGap = 24, double rowGap = 4, double? labelWidth = null, double? valueWrapWidth = null, ChartColor? labelColor = null, ChartColor? valueColor = null, string? fontFamilyName = null) {
        var layer = new VisualCanvasKeyValueBlockLayer(x, y, width, 1, items) {
            LabelFontSize = labelFontSize,
            ValueFontSize = valueFontSize,
            ColumnGap = columnGap,
            RowGap = rowGap,
            LabelWidth = labelWidth,
            ValueWrapWidth = valueWrapWidth,
            LabelColorOverride = labelColor,
            ValueColorOverride = valueColor,
            FontFamilyName = fontFamilyName ?? string.Empty
        };
        layer.Height = layer.MeasureHeight();
        return AddLayer(layer);
    }

    /// <summary>Adds a key/value text block with measured columns and wrapped values using anchor-based placement.</summary>
    public VisualCanvas AddKeyValueBlock(VisualCanvasPlacement placement, double width, IEnumerable<VisualCanvasKeyValueItem> items, double labelFontSize = 16, double valueFontSize = 16, double columnGap = 24, double rowGap = 4, double? labelWidth = null, double? valueWrapWidth = null, ChartColor? labelColor = null, ChartColor? valueColor = null, string? fontFamilyName = null) {
        var layer = new VisualCanvasKeyValueBlockLayer(0, 0, width, 1, items) {
            LabelFontSize = labelFontSize,
            ValueFontSize = valueFontSize,
            ColumnGap = columnGap,
            RowGap = rowGap,
            LabelWidth = labelWidth,
            ValueWrapWidth = valueWrapWidth,
            LabelColorOverride = labelColor,
            ValueColorOverride = valueColor,
            FontFamilyName = fontFamilyName ?? string.Empty
        };
        var bounds = ResolvePlacement(placement, width, layer.MeasureHeight());
        layer.X = bounds.X;
        layer.Y = bounds.Y;
        layer.Height = bounds.Height;
        return AddLayer(layer);
    }

    /// <summary>Adds a reusable information tile.</summary>
    public VisualCanvas AddInfoTile(double x, double y, double width, double height, string icon, string label, string value, string? detail = null, ChartColor? accent = null, double? progress = null, VisualCanvasInfoTileSurfaceStyle surfaceStyle = VisualCanvasInfoTileSurfaceStyle.Glass, VisualCanvasInfoTileIconKind iconKind = VisualCanvasInfoTileIconKind.Text, VisualCanvasInfoTileMiniChartKind miniChartKind = VisualCanvasInfoTileMiniChartKind.None, IEnumerable<double>? miniChartValues = null, double? miniChartMaximum = null) =>
        AddLayer(new VisualCanvasInfoTileLayer(x, y, width, height, icon, label, value) { Detail = detail ?? string.Empty, AccentOverride = accent, Progress = progress, SurfaceStyle = surfaceStyle, IconKind = iconKind }.WithMiniChart(miniChartKind, miniChartValues, miniChartMaximum));

    /// <summary>Adds a reusable information tile using anchor-based placement.</summary>
    public VisualCanvas AddInfoTile(VisualCanvasPlacement placement, double width, double height, string icon, string label, string value, string? detail = null, ChartColor? accent = null, double? progress = null, VisualCanvasInfoTileSurfaceStyle surfaceStyle = VisualCanvasInfoTileSurfaceStyle.Glass, VisualCanvasInfoTileIconKind iconKind = VisualCanvasInfoTileIconKind.Text, VisualCanvasInfoTileMiniChartKind miniChartKind = VisualCanvasInfoTileMiniChartKind.None, IEnumerable<double>? miniChartValues = null, double? miniChartMaximum = null) {
        var bounds = ResolvePlacement(placement, width, height);
        return AddInfoTile(bounds.X, bounds.Y, bounds.Width, bounds.Height, icon, label, value, detail, accent, progress, surfaceStyle, iconKind, miniChartKind, miniChartValues, miniChartMaximum);
    }

    /// <summary>Adds a central icon or logo badge.</summary>
    public VisualCanvas AddHeroBadge(double x, double y, double width, double height, string symbol, ChartColor? accent = null) =>
        AddLayer(new VisualCanvasHeroBadgeLayer(x, y, width, height, symbol) { AccentOverride = accent });

    /// <summary>Adds a central icon or logo badge using anchor-based placement.</summary>
    public VisualCanvas AddHeroBadge(VisualCanvasPlacement placement, double width, double height, string symbol, ChartColor? accent = null) {
        var bounds = ResolvePlacement(placement, width, height);
        return AddHeroBadge(bounds.X, bounds.Y, bounds.Width, bounds.Height, symbol, accent);
    }

    /// <summary>Adds an image layer. SVG output uses <paramref name="href"/>; PNG output uses <paramref name="rgba"/> when supplied.</summary>
    public VisualCanvas AddImage(double x, double y, double width, double height, string? href = null, byte[]? rgba = null, int sourceWidth = 0, int sourceHeight = 0, double opacity = 1, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch) {
        if (rgba != null && (sourceWidth <= 0 || sourceHeight <= 0)) throw new ArgumentOutOfRangeException(nameof(sourceWidth), "RGBA image layers require positive sourceWidth and sourceHeight.");
        ValidateEnum(fit, nameof(fit));
        return AddLayer(new VisualCanvasImageLayer(x, y, width, height) { Href = href ?? string.Empty, Rgba = rgba, SourceWidth = sourceWidth, SourceHeight = sourceHeight, Opacity = opacity, Fit = fit });
    }

    /// <summary>Adds an image layer using anchor-based placement.</summary>
    public VisualCanvas AddImage(VisualCanvasPlacement placement, double width, double height, string? href = null, byte[]? rgba = null, int sourceWidth = 0, int sourceHeight = 0, double opacity = 1, VisualCanvasImageFit fit = VisualCanvasImageFit.Stretch) {
        var bounds = ResolvePlacement(placement, width, height);
        return AddImage(bounds.X, bounds.Y, bounds.Width, bounds.Height, href, rgba, sourceWidth, sourceHeight, opacity, fit);
    }

    /// <summary>Adds a compact feature strip.</summary>
    public VisualCanvas AddFeatureStrip(double x, double y, double width, double height, IEnumerable<VisualCanvasFeatureItem> items) =>
        AddLayer(new VisualCanvasFeatureStripLayer(x, y, width, height, items));

    /// <summary>Adds a compact feature strip using anchor-based placement.</summary>
    public VisualCanvas AddFeatureStrip(VisualCanvasPlacement placement, double width, double height, IEnumerable<VisualCanvasFeatureItem> items) {
        var bounds = ResolvePlacement(placement, width, height);
        return AddFeatureStrip(bounds.X, bounds.Y, bounds.Width, bounds.Height, items);
    }

    internal static void ValidateEnum<TEnum>(TEnum value, string parameterName) where TEnum : struct {
        if (!Enum.IsDefined(typeof(TEnum), value)) throw new ArgumentOutOfRangeException(parameterName, value, "Unknown " + typeof(TEnum).Name + " value.");
    }

    internal static string FormatValue(object? value, string? format = null) {
        if (value == null) return string.Empty;
        if (value is IFormattable formattable && !string.IsNullOrEmpty(format)) return formattable.ToString(format, CultureInfo.InvariantCulture);
        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }
}

/// <summary>
/// Base type for visual canvas layers.
/// </summary>
public abstract class VisualCanvasLayer {
    private double _x;
    private double _y;
    private double _width;
    private double _height;

    /// <summary>Initializes a new visual canvas layer.</summary>
    /// <param name="x">The layer X coordinate.</param>
    /// <param name="y">The layer Y coordinate.</param>
    /// <param name="width">The layer width.</param>
    /// <param name="height">The layer height.</param>
    protected VisualCanvasLayer(double x, double y, double width, double height) {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>Gets or sets the layer X coordinate.</summary>
    public double X { get => _x; set { ValidateFinite(value, nameof(value)); _x = value; } }

    /// <summary>Gets or sets the layer Y coordinate.</summary>
    public double Y { get => _y; set { ValidateFinite(value, nameof(value)); _y = value; } }

    /// <summary>Gets or sets the layer width.</summary>
    public double Width { get => _width; set { ValidatePositive(value, nameof(value)); _width = value; } }

    /// <summary>Gets or sets the layer height.</summary>
    public double Height { get => _height; set { ValidatePositive(value, nameof(value)); _height = value; } }

    /// <summary>Gets the current layer bounds.</summary>
    public ChartRect Bounds => new(X, Y, Width, Height);

    /// <summary>Validates that a numeric value is finite.</summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="parameterName">The parameter name used when throwing.</param>
    protected static void ValidateFinite(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite.");
    }

    /// <summary>Validates that a numeric value is finite and positive.</summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="parameterName">The parameter name used when throwing.</param>
    protected static void ValidatePositive(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite and greater than zero.");
    }

    /// <summary>Validates that a numeric value is finite and not negative.</summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="parameterName">The parameter name used when throwing.</param>
    protected static void ValidateNonNegative(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite and greater than or equal to zero.");
    }
}

/// <summary>Single-color text run used by hero title layers.</summary>
public sealed class VisualCanvasTextRun {
    private string _text;

    /// <summary>Initializes a hero title text run.</summary>
    /// <param name="text">The text to render.</param>
    /// <param name="color">The text color.</param>
    public VisualCanvasTextRun(string text, ChartColor color) {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        Color = color;
    }

    /// <summary>Gets or sets the run text.</summary>
    public string Text { get => _text; set => _text = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the run color.</summary>
    public ChartColor Color { get; set; }
}

/// <summary>Plain text layer.</summary>
public sealed class VisualCanvasTextLayer : VisualCanvasLayer {
    private string _text;
    private double _fontSize;

    /// <summary>Initializes a plain text layer.</summary>
    /// <param name="x">The text X coordinate.</param>
    /// <param name="y">The text Y coordinate.</param>
    /// <param name="width">The text width used for alignment and clipping.</param>
    /// <param name="text">The text to render.</param>
    /// <param name="fontSize">The font size.</param>
    /// <param name="color">The text color.</param>
    public VisualCanvasTextLayer(double x, double y, double width, string text, double fontSize, ChartColor color) : base(x, y, width, Math.Max(1, fontSize * 1.25)) {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        FontSize = fontSize;
        Color = color;
    }

    /// <summary>Gets or sets the text.</summary>
    public string Text { get => _text; set => _text = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Gets or sets the font size.</summary>
    public double FontSize { get => _fontSize; set { ValidatePositive(value, nameof(value)); _fontSize = value; Height = Math.Max(Height, value * 1.25); } }
    /// <summary>Gets or sets the text color.</summary>
    public ChartColor Color { get; set; }
    /// <summary>Gets or sets the text alignment.</summary>
    public VisualCanvasTextAlignment Alignment { get; set; }
    /// <summary>Gets or sets whether the text should use the emphasized raster/SVG treatment.</summary>
    public bool Emphasized { get; set; }
}

/// <summary>Large multi-color headline layer.</summary>
public sealed class VisualCanvasHeroTitleLayer : VisualCanvasLayer {
    private readonly List<VisualCanvasTextRun> _runs = new();
    private double _fontSize;

    /// <summary>Initializes a large multi-run hero title layer.</summary>
    /// <param name="x">The title X coordinate.</param>
    /// <param name="y">The title Y coordinate.</param>
    /// <param name="width">The title width used for alignment.</param>
    /// <param name="fontSize">The title font size.</param>
    /// <param name="runs">The colored text runs.</param>
    public VisualCanvasHeroTitleLayer(double x, double y, double width, double fontSize, IEnumerable<VisualCanvasTextRun> runs) : base(x, y, width, Math.Max(1, fontSize * 1.25)) {
        FontSize = fontSize;
        if (runs == null) throw new ArgumentNullException(nameof(runs));
        foreach (var run in runs) _runs.Add(run ?? throw new ArgumentException("Hero title runs cannot contain null values.", nameof(runs)));
        if (_runs.Count == 0) throw new ArgumentException("Hero title layers require at least one text run.", nameof(runs));
    }

    /// <summary>Gets the colored title runs.</summary>
    public IReadOnlyList<VisualCanvasTextRun> Runs => _runs;
    /// <summary>Gets or sets the title font size.</summary>
    public double FontSize { get => _fontSize; set { ValidatePositive(value, nameof(value)); _fontSize = value; Height = Math.Max(Height, value * 1.25); } }
    /// <summary>Gets or sets the title alignment.</summary>
    public VisualCanvasTextAlignment Alignment { get; set; } = VisualCanvasTextAlignment.Center;
}

/// <summary>Reusable information tile layer.</summary>
public sealed class VisualCanvasInfoTileLayer : VisualCanvasLayer {
    private readonly List<double> _miniChartValues = new();
    private string _icon;
    private string _label;
    private string _value;
    private string _detail = string.Empty;
    private double? _progress;
    private double? _miniChartMaximum;

    /// <summary>Initializes a reusable information tile layer.</summary>
    /// <param name="x">The tile X coordinate.</param>
    /// <param name="y">The tile Y coordinate.</param>
    /// <param name="width">The tile width.</param>
    /// <param name="height">The tile height.</param>
    /// <param name="icon">The compact icon or symbol.</param>
    /// <param name="label">The tile label.</param>
    /// <param name="value">The primary tile value.</param>
    public VisualCanvasInfoTileLayer(double x, double y, double width, double height, string icon, string label, string value) : base(x, y, width, height) {
        _icon = icon ?? throw new ArgumentNullException(nameof(icon));
        _label = label ?? throw new ArgumentNullException(nameof(label));
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets the compact icon or symbol.</summary>
    public string Icon { get => _icon; set => _icon = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Gets or sets the tile label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Gets or sets the primary value.</summary>
    public string Value { get => _value; set => _value = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Gets or sets optional detail text.</summary>
    public string Detail { get => _detail; set => _detail = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Gets or sets the accent color.</summary>
    public ChartColor Accent { get => AccentOverride ?? ChartColor.FromHex("#2F80FF"); set => AccentOverride = value; }
    /// <summary>Gets or sets an explicit accent color. When empty, renderers use the current theme accent.</summary>
    public ChartColor? AccentOverride { get; set; }
    /// <summary>Gets or sets the tile surface treatment.</summary>
    public VisualCanvasInfoTileSurfaceStyle SurfaceStyle { get; set; }
    /// <summary>Gets or sets the tile icon treatment.</summary>
    public VisualCanvasInfoTileIconKind IconKind { get; set; }
    /// <summary>Gets or sets the compact chart kind rendered inside the tile.</summary>
    public VisualCanvasInfoTileMiniChartKind MiniChartKind { get; set; }
    /// <summary>Gets compact chart values rendered inside the tile.</summary>
    public IReadOnlyList<double> MiniChartValues => _miniChartValues;
    /// <summary>Gets or sets the optional compact chart maximum. When empty, the values define the scale.</summary>
    public double? MiniChartMaximum {
        get => _miniChartMaximum;
        set {
            if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value <= 0)) throw new ArgumentOutOfRangeException(nameof(value), value, "Mini chart maximum must be finite and greater than zero.");
            _miniChartMaximum = value;
        }
    }

    /// <summary>Gets or sets optional progress from zero to one.</summary>
    public double? Progress {
        get => _progress;
        set {
            if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value < 0 || value.Value > 1)) throw new ArgumentOutOfRangeException(nameof(value), value, "Progress must be between zero and one.");
            _progress = value;
        }
    }

    /// <summary>Sets the compact chart rendered inside the tile.</summary>
    public VisualCanvasInfoTileLayer WithMiniChart(VisualCanvasInfoTileMiniChartKind kind, IEnumerable<double>? values, double? maximum = null) {
        VisualCanvas.ValidateEnum(kind, nameof(kind));
        _miniChartValues.Clear();
        if (values != null) {
            foreach (var value in values) {
                if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(values), value, "Mini chart values must be finite.");
                _miniChartValues.Add(value);
            }
        }

        MiniChartKind = _miniChartValues.Count == 0 ? VisualCanvasInfoTileMiniChartKind.None : kind;
        MiniChartMaximum = maximum;
        return this;
    }
}

/// <summary>Central logo or icon badge layer.</summary>
public sealed class VisualCanvasHeroBadgeLayer : VisualCanvasLayer {
    private string _symbol;

    /// <summary>Initializes a hero badge layer.</summary>
    /// <param name="x">The badge X coordinate.</param>
    /// <param name="y">The badge Y coordinate.</param>
    /// <param name="width">The badge width.</param>
    /// <param name="height">The badge height.</param>
    /// <param name="symbol">The badge symbol.</param>
    public VisualCanvasHeroBadgeLayer(double x, double y, double width, double height, string symbol) : base(x, y, width, height) {
        _symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
    }

    /// <summary>Gets or sets the badge symbol.</summary>
    public string Symbol { get => _symbol; set => _symbol = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Gets or sets the badge accent color.</summary>
    public ChartColor Accent { get => AccentOverride ?? ChartColor.FromHex("#22A7FF"); set => AccentOverride = value; }
    /// <summary>Gets or sets an explicit badge accent color. When empty, renderers use the current theme secondary accent.</summary>
    public ChartColor? AccentOverride { get; set; }
}

/// <summary>Image layer for host-provided bitmap or SVG-compatible image references.</summary>
public sealed class VisualCanvasImageLayer : VisualCanvasLayer {
    private string _href = string.Empty;
    private double _opacity = 1;

    /// <summary>Initializes an image layer.</summary>
    /// <param name="x">The image X coordinate.</param>
    /// <param name="y">The image Y coordinate.</param>
    /// <param name="width">The rendered image width.</param>
    /// <param name="height">The rendered image height.</param>
    public VisualCanvasImageLayer(double x, double y, double width, double height) : base(x, y, width, height) { }

    /// <summary>Gets or sets the SVG image href.</summary>
    public string Href { get => _href; set => _href = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Gets or sets optional source RGBA pixels for PNG output.</summary>
    public byte[]? Rgba { get; set; }
    /// <summary>Gets or sets the source bitmap width for PNG output.</summary>
    public int SourceWidth { get; set; }
    /// <summary>Gets or sets the source bitmap height for PNG output.</summary>
    public int SourceHeight { get; set; }
    /// <summary>Gets or sets how the source image is placed inside the destination rectangle.</summary>
    public VisualCanvasImageFit Fit { get; set; }

    /// <summary>Gets or sets image opacity from zero to one.</summary>
    public double Opacity {
        get => _opacity;
        set {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value), value, "Opacity must be between zero and one.");
            _opacity = value;
        }
    }
}

/// <summary>Compact feature-strip item.</summary>
public sealed class VisualCanvasFeatureItem {
    private string _icon;
    private string _label;

    /// <summary>Initializes a feature strip item.</summary>
    /// <param name="icon">The compact item icon or symbol.</param>
    /// <param name="label">The item label.</param>
    public VisualCanvasFeatureItem(string icon, string label) {
        _icon = icon ?? throw new ArgumentNullException(nameof(icon));
        _label = label ?? throw new ArgumentNullException(nameof(label));
    }

    /// <summary>Gets or sets the compact item icon or symbol.</summary>
    public string Icon { get => _icon; set => _icon = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Gets or sets the item label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }
}

/// <summary>Feature-strip layer.</summary>
public sealed class VisualCanvasFeatureStripLayer : VisualCanvasLayer {
    private readonly List<VisualCanvasFeatureItem> _items = new();

    /// <summary>Initializes a feature strip layer.</summary>
    /// <param name="x">The strip X coordinate.</param>
    /// <param name="y">The strip Y coordinate.</param>
    /// <param name="width">The strip width.</param>
    /// <param name="height">The strip height.</param>
    /// <param name="items">The strip items.</param>
    public VisualCanvasFeatureStripLayer(double x, double y, double width, double height, IEnumerable<VisualCanvasFeatureItem> items) : base(x, y, width, height) {
        if (items == null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) _items.Add(item ?? throw new ArgumentException("Feature strips cannot contain null items.", nameof(items)));
        if (_items.Count == 0) throw new ArgumentException("Feature strips require at least one item.", nameof(items));
    }

    /// <summary>Gets the feature strip items.</summary>
    public IReadOnlyList<VisualCanvasFeatureItem> Items => _items;
    /// <summary>Gets or sets the feature strip accent color.</summary>
    public ChartColor Accent { get; set; } = ChartColor.FromHex("#2F80FF");
}
