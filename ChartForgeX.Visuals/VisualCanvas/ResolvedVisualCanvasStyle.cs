using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Composition;

/// <summary>
/// Theme colors for reusable visual canvas layers.
/// </summary>
internal sealed class ResolvedVisualCanvasStyle {
    private TextMeasurementMode _textMeasurementMode = TextMeasurementMode.PortableEstimate;
    /// <summary>Controls canvas fitting and wrapping. The default uses portable estimates or explicitly registered faces, independently of installed fonts; InstalledFonts opts into host-dependent exact metrics.</summary>
    public TextMeasurementMode TextMeasurementMode {
        get => _textMeasurementMode;
        set { VisualCanvas.ValidateEnum(value, nameof(value)); _textMeasurementMode = value; }
    }
    /// <summary>Gets or sets the primary font family stack used by SVG and PNG canvas text.</summary>
    public string FontFamily { get; set; } = "Segoe UI, Arial, sans-serif";
    /// <summary>Gets or sets the monospace font family stack used by symbolic badge text in SVG and PNG output.</summary>
    public string MonospaceFontFamily { get; set; } = "Cascadia Mono, Consolas, monospace";
    public double TileLabelSize { get; set; } = 14;
    public double TileValueSize { get; set; } = 22;
    public double TileDetailSize { get; set; } = 13;
    public double TileTextScale { get; set; } = 1;
    /// <summary>Gets or sets the primary accent used by built-in decorative elements.</summary>
    public ChartColor Accent { get; set; } = ChartColor.FromHex("#2F80FF");
    /// <summary>Gets or sets the secondary accent used by hero badges and backdrop highlights.</summary>
    public ChartColor SecondaryAccent { get; set; } = ChartColor.FromHex("#22A7FF");
    /// <summary>Gets or sets the primary canvas foreground used by default hero titles and text layers without an explicit color.</summary>
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
    public ResolvedVisualCanvasStyle Clone() => (ResolvedVisualCanvasStyle)MemberwiseClone();
}
