using System;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    /// <summary>Fits the caption once to the icon width policy shared by painting and obstacle bounds.</summary>
    public static double IconLabelFontSize(TopologyNode node, TextMeasurementContext? measurement, double preferred) {
        var label = TrimTo(node.Label, NodeTitleMaxLength(node, TopologyNodeDisplayMode.Icon));
        var maximumWidth = IconLabelMaxWidth(node);
        return ChartTextFitting.FitFontSize(Math.Min(10.5, preferred), preferred,
            size => EstimateTextWidth(label, size, true, measurement) <= maximumWidth);
    }

    public static string IconLabelText(TopologyNode node, TextMeasurementContext? measurement = null, double fontSize = 10.5) {
        fontSize = IconLabelFontSize(node, measurement, fontSize);
        return TrimToEstimatedWidth(TrimTo(node.Label, NodeTitleMaxLength(node, TopologyNodeDisplayMode.Icon)), IconLabelMaxWidth(node), fontSize, true, measurement);
    }

    public static double IconLabelMaxWidth(TopologyNode node) => Math.Max(node.Width + 46, 72);

    public static double IconLabelPlateWidth(TopologyNode node, TextMeasurementContext? measurement = null, double fontSize = 10.5) {
        fontSize = IconLabelFontSize(node, measurement, fontSize);
        return Math.Max(34, EstimateTextWidth(IconLabelText(node, measurement, fontSize), fontSize, true, measurement) + 12);
    }

    public static double IconLabelPlateHeight(TopologyNode node, TextMeasurementContext? measurement = null, double fontSize = 10.5) {
        fontSize = IconLabelFontSize(node, measurement, fontSize);
        return Math.Max(15, measurement?.MeasureLineHeight(fontSize, true) ?? fontSize * 1.5);
    }

    public static double IconLabelPlateY(TopologyNode node) => node.Y + node.Height + (string.IsNullOrWhiteSpace(NodeBadge(node)) ? 5 : 25);
}
