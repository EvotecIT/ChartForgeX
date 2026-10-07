using System;
using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public static string IconLabelText(TopologyNode node, TextMeasurementContext? measurement = null, double fontSize = 10.5) {
        return TrimToEstimatedWidth(TrimTo(node.Label, NodeTitleMaxLength(node, TopologyNodeDisplayMode.Icon)), IconLabelMaxWidth(node), fontSize, true, measurement);
    }

    public static double IconLabelMaxWidth(TopologyNode node) => Math.Max(node.Width + 46, 72);

    public static double IconLabelPlateWidth(TopologyNode node, TextMeasurementContext? measurement = null, double fontSize = 10.5) => Math.Max(34, EstimateTextWidth(IconLabelText(node, measurement, fontSize), fontSize, true, measurement) + 12);

    public static double IconLabelPlateHeight(TextMeasurementContext? measurement = null, double fontSize = 10.5) =>
        Math.Max(15, measurement?.MeasureLineHeight(fontSize, true) ?? fontSize * 1.5);

    public static double IconLabelPlateY(TopologyNode node) => node.Y + node.Height + (string.IsNullOrWhiteSpace(NodeBadge(node)) ? 5 : 25);
}
