using System;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    private static void CaptureWaterfallPresentation(Chart chart, VisualArtifactInterchangeWaterfallArtifact target) {
        var source = chart.Options;
        target.Orientation = source.Waterfall.Orientation; target.Theme = source.Theme; target.ThemeMode = source.ThemeMode;
        target.Padding = source.ConfiguredPadding; target.ExplicitPadding = source.HasExplicitPadding;
        target.ShowLegend = source.Legend.HasExplicitVisibility ? source.Legend.Visible : null;
        target.LegendPosition = source.Legend.HasExplicitPosition ? source.Legend.Position : null;
        target.BarStyle = source.Bars.VisualStyle.Clone(); target.ExplicitBarCornerRadius = source.Bars.HasPreparedCornerRadius;
        target.GridStyle = source.GridLineStyle.Clone(); target.ExplicitGridStrokeWidth = source.HasPreparedGridStrokeWidth;
        target.Labels = source.Labels.Capture();
        target.HostOwnsFrame = source.HostOwnsFrame;
        target.ShowHeader = source.ShowHeader;
        target.ShowCard = source.ShowCard;
        target.ShowPlotBackground = source.ShowPlotBackground;
        target.TransparentBackground = source.TransparentBackground;
        target.PinStateColorsInForcedColors = source.StateTimeline.PinColorsInForcedColors;
        target.ShowPointLegend = source.Legend.ShowPoints;
        target.LegendMaximumRows = source.Legend.MaxRows;
        target.LegendMaximumHeightFraction = source.Legend.MaxHeightFraction;
        target.ShowAxes = source.ShowAxes;
        target.ShowGrid = source.ShowGrid;
        target.ClipMarksToPlot = source.ClipMarksToPlot;
        target.ShowDataLabels = source.DataLabels.Visible;
        target.DataLabelPlacement = source.DataLabels.Placement;
        target.DataLabelConnectorStyle = source.DataLabels.ConnectorStyle;
        target.DataLabelConnectorOpacity = source.DataLabels.ConnectorOpacity;
        target.DataLabelConnectorStrokeWidth = source.DataLabels.ConnectorStrokeWidth;
        target.DataLabelConnectorColor = source.DataLabels.ConnectorColor;
        target.ValueFormat = source.ValueFormat;
        // Native views (legend reflow, value replays) keep the authored unit and colour assignment.
        target.ValueUnit = source.ValueUnit;
        target.SeriesColorAssignment = source.SeriesColorAssignment;
        target.RasterScale = source.Raster.OutputScale;
        target.RasterSupersampling = source.Raster.SupersamplingScale;
        target.RasterTextHinting = source.Raster.TextHinting;
        target.XAxis = source.XAxis.Clone();
        target.YAxis = source.YAxis.Clone();
        target.SecondaryXAxis = source.SecondaryXAxis.Clone();
        target.SecondaryYAxis = source.SecondaryYAxis.Clone();
        target.XAxisTitle = chart.Options.XAxis.Title;
        target.YAxisTitle = chart.Options.YAxis.Title;
        target.SecondaryXAxisTitle = chart.Options.SecondaryXAxis.Title;
        target.SecondaryYAxisTitle = chart.Options.SecondaryYAxis.Title;
        foreach (var role in new[] { ChartTextRole.Title, ChartTextRole.Subtitle, ChartTextRole.AxisTitle,
            ChartTextRole.TickLabel, ChartTextRole.Legend, ChartTextRole.DataLabel }) target.TextStyles.Add(role, source.GetTextStyle(role).Clone());
    }

    private static void ApplyWaterfallPresentation(Chart chart, VisualArtifactInterchangeWaterfallArtifact source) {
        var target = chart.Options;
        target.Waterfall.Orientation = source.Orientation; target.Theme = source.Theme; target.ThemeMode = source.ThemeMode;
        if (source.ExplicitPadding) target.Padding = source.Padding;
        if (source.ShowLegend.HasValue) target.Legend.Visible = source.ShowLegend.Value;
        if (source.LegendPosition.HasValue) target.Legend.Position = source.LegendPosition.Value;
        CopyWaterfallBars(source.BarStyle, target.Bars.VisualStyle, source.ExplicitBarCornerRadius);
        CopyWaterfallGrid(source.GridStyle, target.GridLineStyle, source.ExplicitGridStrokeWidth);
        target.Labels = source.Labels.Capture();
        target.HostOwnsFrame = source.HostOwnsFrame;
        target.ShowHeader = source.ShowHeader;
        target.ShowCard = source.ShowCard;
        target.ShowPlotBackground = source.ShowPlotBackground;
        target.TransparentBackground = source.TransparentBackground;
        target.StateTimeline.PinColorsInForcedColors = source.PinStateColorsInForcedColors;
        target.Legend.ShowPoints = source.ShowPointLegend;
        target.Legend.MaxRows = source.LegendMaximumRows;
        target.Legend.MaxHeightFraction = source.LegendMaximumHeightFraction;
        target.ShowAxes = source.ShowAxes;
        target.ShowGrid = source.ShowGrid;
        target.ClipMarksToPlot = source.ClipMarksToPlot;
        target.DataLabels.Visible = source.ShowDataLabels;
        target.DataLabels.Placement = source.DataLabelPlacement;
        target.DataLabels.ConnectorStyle = source.DataLabelConnectorStyle;
        target.DataLabels.ConnectorOpacity = source.DataLabelConnectorOpacity;
        target.DataLabels.ConnectorStrokeWidth = source.DataLabelConnectorStrokeWidth;
        target.DataLabels.ConnectorColor = source.DataLabelConnectorColor;
        target.ValueFormat = source.ValueFormat;
        // Native views (legend reflow, value replays) keep the authored unit and colour assignment.
        target.ValueUnit = source.ValueUnit;
        target.SeriesColorAssignment = source.SeriesColorAssignment;
        target.Raster.OutputScale = source.RasterScale;
        target.Raster.SupersamplingScale = source.RasterSupersampling;
        target.Raster.TextHinting = source.RasterTextHinting;
        target.XAxis = source.XAxis.Clone();
        target.YAxis = source.YAxis.Clone();
        target.SecondaryXAxis = source.SecondaryXAxis.Clone();
        target.SecondaryYAxis = source.SecondaryYAxis.Clone();
        chart.Options.XAxis.Title = source.XAxisTitle;
        chart.Options.YAxis.Title = source.YAxisTitle;
        chart.Options.SecondaryXAxis.Title = source.SecondaryXAxisTitle;
        chart.Options.SecondaryYAxis.Title = source.SecondaryYAxisTitle;
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
        if (options.XAxisLabelHighlights.Count != 0 || options.IsSparkline || options.IsPanel || options.Raster.FontPath != null || options.Raster.FontFaceName != null || options.Raster.FontCollectionIndex != null || options.SvgColorVariables != null)
            throw new NotSupportedException("Waterfall interchange does not serialize compact host state, local font resources or SVG variable resolvers.");
        if (options.Labels.AccessibleTextFormatter != null || options.Labels.DateFormatter != null || options.Pie.SliceLabelFormatter != null
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
