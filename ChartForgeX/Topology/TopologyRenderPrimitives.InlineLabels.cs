using System;
using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    /// <summary>Metric projections cannot accidentally inherit formatting from different authored text.</summary>
    internal static TextContent EdgeCaptionContent(TopologyEdge edge, string? selected) =>
        edge.LabelContent is { IsStructured: true } content && string.Equals(content.PlainText, selected, StringComparison.Ordinal)
            ? content : TextContent.Plain(selected ?? string.Empty);

    /// <summary>Reserves complete captured sibling-face rows before rank spacing, obstacles and painting.</summary>
    internal static (double Width, double Height) InlineEdgeLabelExtent(TopologyChart chart, TopologyEdge edge,
        string? primary, string? secondary, string? tertiary, TopologyRenderOptions options, double width, double height) {
        var content = EdgeCaptionContent(edge, primary);
        if (!content.IsStructured) return (width, height);
        var measurement = options.TextMeasurement ?? chart.TextMeasurement
            ?? new TextMeasurementContext(chart.Theme.Typography.ToFontSpec(), options.TextMeasurementMode);
        var units = TopologyPreparedEdgePlan.Units(edge);
        var size = edge.TextStyle?.FontSize ?? chart.Theme.Typography.DataLabelSize * units;
        var style = TopologyTextPolicy.ResolveStructured(edge.TextStyle, measurement.Font, size); style.FontSize = size;
        var metrics = TextLayoutEngine.Measure(TopologyTextLayout.DisplayRows(content, options.AllowMultilineNodeLabels), style);
        var rowHeight = metrics.LineHeight;
        foreach (var value in new[] { secondary, tertiary }) if (!string.IsNullOrWhiteSpace(value)) {
            var plain = TopologyTextPolicy.Resolve(edge.TextStyle, measurement.Font, size * .85, 400); plain.FontSize = size * .85;
            rowHeight = Math.Max(rowHeight, TextLayoutEngine.Measure(value!, plain).LineHeight);
        }
        return (Math.Max(width, metrics.Width + 18 * units), Math.Max(height,
            EdgeLabelRows(primary, secondary, tertiary, options) * rowHeight + 4 * units));
    }
}
