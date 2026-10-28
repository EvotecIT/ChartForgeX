using System;
using System.Collections.Generic;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    private static void PlaceVerticalStack(TopologyChart chart, IList<TopologyNode> nodes, TopologyGroup group) {
        var options = chart.RenderOptions ?? new TopologyRenderOptions();
        var y = TopologyGroupHeader.IsDrawn(options) ? TopologyGroupHeader.ReservedBounds(chart, group, options).Bottom + 12 : group.Y + 24;
        foreach (var node in nodes) {
            if (IsUnset(node.X) && IsUnset(node.Y)) { node.X = group.X + 24; node.Y = y; }
            y = Math.Max(y, node.Y) + TopologyNodeFootprint.Height(chart, node) + 16;
        }
        group.Height = Math.Max(group.Height, y - group.Y + 8);
    }
    private static double DenseHeaderWidth(TopologyChart chart, string value, double size, bool bold, TopologyRenderOptions options) {
        if (!options.AllowMultilineNodeLabels) return EstimateTextWidth(value.Replace('\n', ' ').Replace('\r', ' '), size, bold, chart.TextMeasurement);
        var width = 0.0;
        foreach (var row in value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            width = Math.Max(width, EstimateTextWidth(row, size, bold, chart.TextMeasurement));
        return width;
    }

    private static double DenseStackHeaderHeight(TopologyChart chart, TopologyGroup? group) {
        var options = chart.RenderOptions ?? new TopologyRenderOptions();
        if (group == null || !TopologyGroupHeader.IsDrawn(options)) return 48;
        // Automatic source groups do not have finite positions yet. Measure the same shared
        // header at an origin without mutating the source or caching invalid coordinates.
        var measuring = new TopologyGroup { Id = group.Id, Label = group.Label, Subtitle = group.Subtitle, ShowLabel = group.ShowLabel,
            Symbol = group.Symbol, IconId = group.IconId, X = 0, Y = 0, Width = Math.Max(190, group.Width), Height = 170 };
        return Math.Max(group.ShowLabel ? 110 : 48, TopologyGroupHeader.ReservedBounds(chart, measuring, options).Bottom + 36);
    }

}
