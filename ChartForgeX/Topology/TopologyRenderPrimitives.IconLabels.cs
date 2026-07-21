using System;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public static string IconLabelText(TopologyNode node) {
        return TrimToEstimatedWidth(TrimTo(node.Label, NodeTitleMaxLength(node, TopologyNodeDisplayMode.Icon)), IconLabelMaxWidth(node), 10.5, true);
    }

    public static double IconLabelMaxWidth(TopologyNode node) => Math.Max(node.Width + 46, 72);

    public static double IconLabelPlateWidth(TopologyNode node) => Math.Max(34, EstimateTextWidth(IconLabelText(node), 10.5, true) + 12);

    public static double IconLabelPlateY(TopologyNode node) => node.Y + node.Height + (string.IsNullOrWhiteSpace(NodeBadge(node)) ? 5 : 25);
}
