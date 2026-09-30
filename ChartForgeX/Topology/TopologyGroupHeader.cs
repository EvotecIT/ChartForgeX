using System;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>
/// Owns the geometry of the label block drawn at the top of a group (symbol, label, optional subtitle), so layout
/// normalization, readable dense routing, and diagnostics agree with what the SVG and PNG renderers draw: centered in
/// the group, or left-aligned on the neutral monitoring surface.
/// </summary>
internal static class TopologyGroupHeader {
    private const double GroupPadding = 24;
    private const double TopPadding = 14;

    /// <summary>Returns true when group headers are drawn with the given options.</summary>
    public static bool IsDrawn(TopologyRenderOptions options) => options.IncludeGroups && options.IncludeGroupLabels;

    /// <summary>Returns the rectangle the symbol, label, and subtitle of a group are drawn in.</summary>
    public static ChartRect Bounds(TopologyGroup group, TopologyRenderOptions options, TextMeasurementContext? measurement) =>
        Block(group, options, measurement, string.IsNullOrWhiteSpace(group.Subtitle) ? 24 : 42);

    /// <summary>Returns the header block with the room layout keeps free below it, which nodes are moved out of.</summary>
    public static ChartRect ReservedBounds(TopologyGroup group, TopologyRenderOptions options, TextMeasurementContext? measurement) =>
        Block(group, options, measurement, string.IsNullOrWhiteSpace(group.Subtitle) ? 40 : 60);

    private static ChartRect Block(TopologyGroup group, TopologyRenderOptions options, TextMeasurementContext? measurement, double height) {
        var rendersSymbol = !IsMonitoringDashboardStyle(options) || !string.IsNullOrWhiteSpace(group.Symbol) || ResolveGroupIcon(group, options) != null;
        if (IsMonitoringDashboardStyle(options) && UseNeutralGroupSurface(options)) return NeutralBounds(group, options, measurement, rendersSymbol, height);
        var width = Width(group, options, measurement, rendersSymbol);
        return new ChartRect(group.X + (group.Width - width) / 2, group.Y + TopPadding, width, height);
    }

    // The neutral monitoring surface draws the symbol at the left edge with the label and subtitle beside it.
    private static ChartRect NeutralBounds(TopologyGroup group, TopologyRenderOptions options, TextMeasurementContext? measurement, bool rendersSymbol, double height) {
        var left = group.X + (rendersSymbol ? 12 : 22);
        var textLeft = group.X + (rendersSymbol ? 42 : 22);
        var maxLabelWidth = LabelWidth(group, options, rendersSymbol);
        var labelSize = FitFontSize(group.Label, maxLabelWidth, 15, 12, true, measurement);
        var labelWidth = EstimateTextWidth(TrimToEstimatedWidth(group.Label, maxLabelWidth, labelSize, true, measurement), labelSize, true, measurement);
        var subtitleWidth = string.IsNullOrWhiteSpace(group.Subtitle) ? 0 : EstimateTextWidth(TrimToEstimatedWidth(group.Subtitle!, maxLabelWidth, 11, false, measurement), 11, false, measurement);
        return new ChartRect(left, group.Y + TopPadding, textLeft + Math.Max(labelWidth, subtitleWidth) + 6 - left, height);
    }

    private static double Width(TopologyGroup group, TopologyRenderOptions options, TextMeasurementContext? measurement, bool rendersSymbol) {
        var maxLabelWidth = LabelWidth(group, options, rendersSymbol);
        var labelSize = FitFontSize(group.Label, maxLabelWidth, 16, 12, true, measurement);
        var labelWidth = EstimateTextWidth(TrimToEstimatedWidth(group.Label, maxLabelWidth, labelSize, true, measurement), labelSize, true, measurement) + (rendersSymbol ? 30 : 0);
        var subtitleWidth = string.IsNullOrWhiteSpace(group.Subtitle) ? 0 : EstimateTextWidth(group.Subtitle!, 12, false, measurement);
        return Math.Min(Math.Max(96, Math.Max(labelWidth, subtitleWidth) + 12), Math.Max(96, group.Width - GroupPadding * 2));
    }

    private static double LabelWidth(TopologyGroup group, TopologyRenderOptions options, bool includesLeadingSymbol) {
        var statusReserve = options.IncludeGroupStatusDots && IsMonitoringDashboardStyle(options) && group.Status != TopologyHealthStatus.Unknown ? 38 : 0;
        var symbolReserve = includesLeadingSymbol ? 42 : 0;
        return Math.Max(36, group.Width - 44 - statusReserve - symbolReserve);
    }
}
