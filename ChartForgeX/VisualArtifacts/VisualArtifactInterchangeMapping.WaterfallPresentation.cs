using System;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    private static void CaptureWaterfallPresentation(Chart chart, VisualArtifactInterchangeWaterfallArtifact target) {
        var source = chart.Options;
        target.Orientation = source.Waterfall.Orientation; target.Theme = source.Theme; target.ThemeMode = source.ThemeMode;
        target.Padding = source.ConfiguredPadding; target.ExplicitPadding = source.HasExplicitPadding;
        target.ShowLegend = source.HasExplicitLegend ? source.ShowLegend : null;
        target.LegendPosition = source.HasExplicitLegendPosition ? source.LegendPosition : null;
        target.BarStyle = source.BarVisualStyle.Clone(); target.ExplicitBarCornerRadius = source.HasPreparedBarCornerRadius;
        target.GridStyle = source.GridLineStyle.Clone(); target.ExplicitGridStrokeWidth = source.HasPreparedGridStrokeWidth;
        target.Labels = source.Labels.Capture();
        target.HostOwnsFrame = source.HostOwnsFrame;
        target.ShowHeader = source.ShowHeader;
        target.ShowCard = source.ShowCard;
        target.ShowPlotBackground = source.ShowPlotBackground;
        target.TransparentBackground = source.TransparentBackground;
        target.PinStateColorsInForcedColors = source.PinStateColorsInForcedColors;
        target.ShowPointLegend = source.ShowPointLegend;
        target.LegendMaximumRows = source.LegendMaximumRows;
        target.LegendMaximumHeightFraction = source.LegendMaximumHeightFraction;
        target.ShowAxes = source.ShowAxes;
        target.ShowGrid = source.ShowGrid;
        target.ClipMarksToPlot = source.ClipMarksToPlot;
        target.ShowDataLabels = source.ShowDataLabels;
        target.DataLabelPlacement = source.DataLabelPlacement;
        target.DataLabelConnectorStyle = source.DataLabelConnectorStyle;
        target.DataLabelConnectorOpacity = source.DataLabelConnectorOpacity;
        target.DataLabelConnectorStrokeWidth = source.DataLabelConnectorStrokeWidth;
        target.DataLabelConnectorColor = source.DataLabelConnectorColor;
        target.ValueFormat = source.ValueFormat;
        target.PngOutputScale = source.PngOutputScale;
        target.PngSupersamplingScale = source.PngSupersamplingScale;
        target.PngTextHinting = source.PngTextHinting;
        target.XAxis = source.XAxis.Clone();
        target.YAxis = source.YAxis.Clone();
        target.SecondaryXAxis = source.SecondaryXAxis.Clone();
        target.SecondaryYAxis = source.SecondaryYAxis.Clone();
        target.XAxisTitle = chart.XAxisTitle;
        target.YAxisTitle = chart.YAxisTitle;
        target.SecondaryXAxisTitle = chart.SecondaryXAxisTitle;
        target.SecondaryYAxisTitle = chart.SecondaryYAxisTitle;
        foreach (var role in new[] { ChartTextRole.Title, ChartTextRole.Subtitle, ChartTextRole.AxisTitle,
            ChartTextRole.TickLabel, ChartTextRole.Legend, ChartTextRole.DataLabel }) target.TextStyles.Add(role, source.GetTextStyle(role).Clone());
    }

    private static void ApplyWaterfallPresentation(Chart chart, VisualArtifactInterchangeWaterfallArtifact source) {
        var target = chart.Options;
        target.Waterfall.Orientation = source.Orientation; target.Theme = source.Theme; target.ThemeMode = source.ThemeMode;
        if (source.ExplicitPadding) target.Padding = source.Padding;
        if (source.ShowLegend.HasValue) target.ShowLegend = source.ShowLegend.Value;
        if (source.LegendPosition.HasValue) target.LegendPosition = source.LegendPosition.Value;
        CopyWaterfallBars(source.BarStyle, target.BarVisualStyle, source.ExplicitBarCornerRadius);
        CopyWaterfallGrid(source.GridStyle, target.GridLineStyle, source.ExplicitGridStrokeWidth);
        target.Labels = source.Labels.Capture();
        target.HostOwnsFrame = source.HostOwnsFrame;
        target.ShowHeader = source.ShowHeader;
        target.ShowCard = source.ShowCard;
        target.ShowPlotBackground = source.ShowPlotBackground;
        target.TransparentBackground = source.TransparentBackground;
        target.PinStateColorsInForcedColors = source.PinStateColorsInForcedColors;
        target.ShowPointLegend = source.ShowPointLegend;
        target.LegendMaximumRows = source.LegendMaximumRows;
        target.LegendMaximumHeightFraction = source.LegendMaximumHeightFraction;
        target.ShowAxes = source.ShowAxes;
        target.ShowGrid = source.ShowGrid;
        target.ClipMarksToPlot = source.ClipMarksToPlot;
        target.ShowDataLabels = source.ShowDataLabels;
        target.DataLabelPlacement = source.DataLabelPlacement;
        target.DataLabelConnectorStyle = source.DataLabelConnectorStyle;
        target.DataLabelConnectorOpacity = source.DataLabelConnectorOpacity;
        target.DataLabelConnectorStrokeWidth = source.DataLabelConnectorStrokeWidth;
        target.DataLabelConnectorColor = source.DataLabelConnectorColor;
        target.ValueFormat = source.ValueFormat;
        target.PngOutputScale = source.PngOutputScale;
        target.PngSupersamplingScale = source.PngSupersamplingScale;
        target.PngTextHinting = source.PngTextHinting;
        target.XAxis = source.XAxis.Clone();
        target.YAxis = source.YAxis.Clone();
        target.SecondaryXAxis = source.SecondaryXAxis.Clone();
        target.SecondaryYAxis = source.SecondaryYAxis.Clone();
        chart.XAxisTitle = source.XAxisTitle;
        chart.YAxisTitle = source.YAxisTitle;
        chart.SecondaryXAxisTitle = source.SecondaryXAxisTitle;
        chart.SecondaryYAxisTitle = source.SecondaryYAxisTitle;
        foreach (var pair in source.TextStyles) target.SetTextStyle(pair.Key, pair.Value.Clone());
    }

    // These owners use explicitness to choose authored versus theme geometry. Do not set a default override accidentally.
    private static void CopyWaterfallBars(ChartBarVisualStyle source, ChartBarVisualStyle target, bool explicitRadius) {
        target.Kind = source.Kind; target.BodyOpacity = source.BodyOpacity; target.CapOpacity = source.CapOpacity;
        target.CapThickness = source.CapThickness; target.CapInset = source.CapInset;
        if (explicitRadius || source.HasExplicitCornerRadius) target.CornerRadius = source.CornerRadius;
        target.CapShadowOpacity = source.CapShadowOpacity; target.CapShadowOffset = source.CapShadowOffset;
        target.CapShadowSpread = source.CapShadowSpread; target.CapHighlightOpacity = source.CapHighlightOpacity;
    }

    private static void CopyWaterfallGrid(ChartGridLineStyle source, ChartGridLineStyle target, bool explicitWidth) {
        target.ShowHorizontalLines = source.ShowHorizontalLines; target.ShowVerticalLines = source.ShowVerticalLines;
        target.HorizontalOpacity = source.HorizontalOpacity; target.VerticalOpacity = source.VerticalOpacity;
        if (explicitWidth || source.HasExplicitStrokeWidth) target.StrokeWidth = source.StrokeWidth;
        target.Dash = source.Dash; target.Gap = source.Gap;
    }

    private static void ValidateWaterfallSourceBoundary(Chart chart) {
        var options = chart.Options;
        if (chart.Annotations.Count != 0 || chart.PlotRegions.Count != 0)
            throw new NotSupportedException("Waterfall interchange does not serialize annotations or plot regions; deliver them through a separately supported semantic family.");
        if (options.XAxisLabelHighlights.Count != 0 || options.IsSparkline || options.IsPanel || options.PngFontPath != null || options.PngFontFaceName != null || options.PngFontCollectionIndex != null || options.SvgColorVariables != null)
            throw new NotSupportedException("Waterfall interchange does not serialize compact host state, local font resources or SVG variable resolvers.");
        if (options.Labels.AccessibleTextFormatter != null || options.Labels.DateFormatter != null || options.PieSliceLabelFormatter != null
            || options.RangeBar.EndpointLabelFormatter != null || options.BoxPlot.StatisticLabelFormatter != null || options.Sunburst.SecondaryLabelFormatter != null)
            throw new NotSupportedException("Waterfall interchange requires static localized words, without callbacks.");
        if (chart.Series[0].DecimationMode.HasValue || chart.Series[0].PointSliceOffsets.Any(value => value != 0))
            throw new NotSupportedException("Waterfall interchange does not serialize derived decimation or unrelated slice state.");
        _ = options.ValueFormat.PortablePolicy();
        foreach (var axis in new[] { options.XAxis, options.YAxis, options.SecondaryXAxis, options.SecondaryYAxis }) ValidateWaterfallPortableAxis(axis);
    }

    internal static void ValidateWaterfallPortableAxis(ChartAxis axis) {
        if (axis == null) throw new ArgumentException("Portable Waterfall axes must not be null.");
        if (axis.TimeTickLabelFormatter != null || axis.TimeSessions != null || axis.TimeSpacing != null || axis.TimeZone != null || axis.PreparedTimeDomain != null || axis.PreparedViewportWindow != null)
            throw new NotSupportedException("Waterfall interchange does not serialize callback/time-zone/session/discrete-time or prepared viewport axis state.");
        if (axis.ValueFormat != null) _ = axis.ValueFormat.PortablePolicy();
    }
}
