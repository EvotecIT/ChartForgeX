using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Themes;

namespace ChartForgeX.VisualArtifacts;

public sealed partial class VisualArtifactInterchangeWaterfallArtifact {
    /// <summary>Gets or sets the physical direction; source accumulation order is unchanged.</summary>
    public ChartOrientation Orientation { get; set; }
    /// <summary>Gets or sets the immutable native theme, including status paints, connector ink and typography.</summary>
    public VisualTheme Theme { get; set; } = VisualTheme.Graphite();
    /// <summary>Gets or sets the explicit native theme mode.</summary>
    public VisualThemeMode ThemeMode { get; set; }
    /// <summary>Gets or sets the configured padding, retained while the host owns the frame.</summary>
    public ChartPadding Padding { get; set; } = ChartPadding.All(24);
    /// <summary>Gets or sets whether padding was explicitly authored; false retains native compact-frame fallback.</summary>
    public bool ExplicitPadding { get; set; }
    /// <summary>Gets or sets whether the host owns the native header and surface.</summary>
    public bool HostOwnsFrame { get; set; }
    /// <summary>Gets or sets whether native title and subtitle are displayed.</summary>
    public bool ShowHeader { get; set; } = true;
    /// <summary>Gets or sets whether the native card is painted.</summary>
    public bool ShowCard { get; set; } = true;
    /// <summary>Gets or sets whether the plot surface is painted.</summary>
    public bool ShowPlotBackground { get; set; }
    /// <summary>Gets or sets the native declarative forced-colour policy for state paints.</summary>
    public bool PinStateColorsInForcedColors { get; set; }
    /// <summary>Gets or sets whether the outer background is transparent.</summary>
    public bool TransparentBackground { get; set; } = true;
    /// <summary>Gets or sets explicit legend visibility; null retains the native family default.</summary>
    public bool? ShowLegend { get; set; }
    /// <summary>Gets or sets explicit legend position; null retains native placement.</summary>
    public ChartLegendPosition? LegendPosition { get; set; }
    /// <summary>Gets or sets whether individual items contribute legend entries.</summary>
    public bool ShowPointLegend { get; set; }
    /// <summary>Gets or sets the optional native legend row limit.</summary>
    public int? LegendMaximumRows { get; set; }
    /// <summary>Gets or sets the native legend height budget.</summary>
    public double LegendMaximumHeightFraction { get; set; } = .35;
    /// <summary>Gets or sets whether physical axes are displayed.</summary>
    public bool ShowAxes { get; set; } = true;
    /// <summary>Gets or sets whether the native grid is painted.</summary>
    public bool ShowGrid { get; set; } = true;
    /// <summary>Gets or sets whether marks are clipped to the plot.</summary>
    public bool ClipMarksToPlot { get; set; } = true;
    /// <summary>Gets or sets chart-level data-caption visibility; null retains defaults.</summary>
    public bool? ShowDataLabels { get; set; }
    /// <summary>Gets or sets the native chart caption placement.</summary>
    public ChartDataLabelPlacement DataLabelPlacement { get; set; }
    /// <summary>Gets or sets the complete shared bar treatment; the native owner paints it.</summary>
    public ChartBarVisualStyle BarStyle { get; set; } = ChartBarVisualStyle.Flat();
    /// <summary>Gets or sets whether the bar radius overrides the theme radius.</summary>
    public bool ExplicitBarCornerRadius { get; set; }
    /// <summary>Gets or sets the complete shared Cartesian grid treatment.</summary>
    public ChartGridLineStyle GridStyle { get; set; } = ChartGridLineStyle.Default();
    /// <summary>Gets or sets whether grid stroke width overrides the theme width.</summary>
    public bool ExplicitGridStrokeWidth { get; set; }
    /// <summary>Gets or sets the built-in numeric policy; callback-backed policies reject during interchange validation.</summary>
    public ChartValueFormat ValueFormat { get; set; } = ChartValueFormat.ExistingValue;
    /// <summary>Gets or sets the native physical X-axis options within the portable axis subset.</summary>
    public ChartAxis XAxis { get; set; } = new();
    /// <summary>Gets or sets the native physical Y-axis options within the portable axis subset.</summary>
    public ChartAxis YAxis { get; set; } = new();
    /// <summary>Gets or sets the native secondary physical X-axis options.</summary>
    public ChartAxis SecondaryXAxis { get; set; } = new();
    /// <summary>Gets or sets the native secondary physical Y-axis options.</summary>
    public ChartAxis SecondaryYAxis { get; set; } = new();
    /// <summary>Gets or sets the complete physical X-axis title.</summary>
    public string XAxisTitle { get; set; } = string.Empty;
    /// <summary>Gets or sets the complete physical Y-axis title.</summary>
    public string YAxisTitle { get; set; } = string.Empty;
    /// <summary>Gets or sets the complete secondary X-axis title.</summary>
    public string SecondaryXAxisTitle { get; set; } = string.Empty;
    /// <summary>Gets or sets the complete secondary Y-axis title.</summary>
    public string SecondaryYAxisTitle { get; set; } = string.Empty;
    /// <summary>Gets or sets native static localized readout and accessibility words; callback fields reject.</summary>
    public ChartLabels Labels { get; set; } = new();
    /// <summary>Gets or sets the output pixel multiplier, one through four.</summary>
    public int PngOutputScale { get; set; } = 1;
    /// <summary>Gets or sets the native internal raster supersampling scale, one through four.</summary>
    public int PngSupersamplingScale { get; set; } = 2;
    /// <summary>Gets or sets the native raster text-hinting policy.</summary>
    public TextHinting PngTextHinting { get; set; }
    /// <summary>Gets or sets the shared native data-caption connector shape.</summary>
    public ChartDataLabelConnectorStyle DataLabelConnectorStyle { get; set; } = ChartDataLabelConnectorStyle.Elbow;
    /// <summary>Gets or sets data-caption connector opacity.</summary>
    public double DataLabelConnectorOpacity { get; set; } = .55;
    /// <summary>Gets or sets data-caption connector stroke width.</summary>
    public double DataLabelConnectorStrokeWidth { get; set; } = 1.2;
    /// <summary>Gets or sets explicit data-caption connector ink.</summary>
    public ChartColor? DataLabelConnectorColor { get; set; }
    /// <summary>Gets detached native typography by chart text role.</summary>
    public Dictionary<ChartTextRole, TextStyleOverride> TextStyles { get; } = new();

    private static void CopyPresentation(VisualArtifactInterchangeWaterfallArtifact source, VisualArtifactInterchangeWaterfallArtifact copy) {
        copy.Orientation = source.Orientation;
        copy.Theme = source.Theme;
        copy.ThemeMode = source.ThemeMode;
        copy.Padding = source.Padding;
        copy.ExplicitPadding = source.ExplicitPadding;
        copy.HostOwnsFrame = source.HostOwnsFrame;
        copy.ShowHeader = source.ShowHeader;
        copy.ShowCard = source.ShowCard;
        copy.ShowPlotBackground = source.ShowPlotBackground;
        copy.TransparentBackground = source.TransparentBackground;
        copy.PinStateColorsInForcedColors = source.PinStateColorsInForcedColors;
        copy.ShowLegend = source.ShowLegend;
        copy.LegendPosition = source.LegendPosition;
        copy.ShowPointLegend = source.ShowPointLegend;
        copy.LegendMaximumRows = source.LegendMaximumRows;
        copy.LegendMaximumHeightFraction = source.LegendMaximumHeightFraction;
        copy.ShowAxes = source.ShowAxes;
        copy.ShowGrid = source.ShowGrid;
        copy.ClipMarksToPlot = source.ClipMarksToPlot;
        copy.ShowDataLabels = source.ShowDataLabels;
        copy.DataLabelPlacement = source.DataLabelPlacement;
        copy.BarStyle = source.BarStyle.Clone();
        copy.ExplicitBarCornerRadius = source.ExplicitBarCornerRadius;
        copy.GridStyle = source.GridStyle.Clone();
        copy.ExplicitGridStrokeWidth = source.ExplicitGridStrokeWidth;
        copy.ValueFormat = source.ValueFormat;
        copy.XAxis = source.XAxis.Clone();
        copy.YAxis = source.YAxis.Clone();
        copy.SecondaryXAxis = source.SecondaryXAxis.Clone();
        copy.SecondaryYAxis = source.SecondaryYAxis.Clone();
        copy.XAxisTitle = source.XAxisTitle;
        copy.YAxisTitle = source.YAxisTitle;
        copy.SecondaryXAxisTitle = source.SecondaryXAxisTitle;
        copy.SecondaryYAxisTitle = source.SecondaryYAxisTitle;
        copy.Labels = source.Labels.Capture();
        copy.PngOutputScale = source.PngOutputScale;
        copy.PngSupersamplingScale = source.PngSupersamplingScale;
        copy.PngTextHinting = source.PngTextHinting;
        copy.DataLabelConnectorStyle = source.DataLabelConnectorStyle;
        copy.DataLabelConnectorOpacity = source.DataLabelConnectorOpacity;
        copy.DataLabelConnectorStrokeWidth = source.DataLabelConnectorStrokeWidth;
        copy.DataLabelConnectorColor = source.DataLabelConnectorColor;
        foreach (var pair in source.TextStyles) copy.TextStyles.Add(pair.Key, pair.Value.Clone());
    }
}
