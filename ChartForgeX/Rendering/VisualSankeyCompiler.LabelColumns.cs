using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualSankeyCompiler {
    private static Dictionary<int, SankeyLabelColumn> LabelColumns(ChartRect plot, ChartSankeyModel model, ChartSankeyOptions options) {
        var layers = model.Nodes.GroupBy(node => node.Layer).OrderBy(group => group.Key).ToArray();
        var sides = layers.Select((_, index) => LabelSide(index, layers.Length, options)).ToArray();
        var result = new Dictionary<int, SankeyLabelColumn>();
        for (int index = 0; index < layers.Length; index++) {
            double x = layers[index].First().X, end = x + model.NodeWidth;
            double previousEnd = index == 0 ? plot.Left : layers[index - 1].First().X + model.NodeWidth;
            double next = index == layers.Length - 1 ? plot.Right : layers[index + 1].First().X;
            var side = sides[index];
            double left, right, anchor;
            if (side == ChartSankeyLabelPlacement.Left) {
                left = index == 0 ? plot.Left : previousEnd + 8;
                if (index > 0 && sides[index - 1] != ChartSankeyLabelPlacement.Left) left = (previousEnd + x) / 2 + 4;
                right = x - 8; anchor = right;
            } else if (side == ChartSankeyLabelPlacement.Right) {
                left = end + 8; right = next - (index == layers.Length - 1 ? 0 : 8);
                if (index < layers.Length - 1 && sides[index + 1] != ChartSankeyLabelPlacement.Right) right = (end + next) / 2 - 4;
                anchor = left;
            } else {
                left = index == 0 ? plot.Left : (previousEnd + x) / 2 + 4;
                right = index == layers.Length - 1 ? plot.Right : (end + next) / 2 - 4;
                anchor = x + model.NodeWidth / 2;
            }
            result.Add(layers[index].Key, new SankeyLabelColumn(new ChartRect(left, plot.Top, Math.Max(0, right - left), plot.Height), side, anchor));
        }
        return result;
    }

    private static ChartSankeyLabelPlacement LabelSide(int index, int count, ChartSankeyOptions options) {
        if (options.EdgeLabelPlacement.HasValue && (index == 0 || index == count - 1)) {
            bool left = options.EdgeLabelPlacement == ChartSankeyEdgeLabelPlacement.Outside ? index == 0 : index != 0;
            return left ? ChartSankeyLabelPlacement.Left : ChartSankeyLabelPlacement.Right;
        }
        return options.LabelPlacement;
    }

    private readonly struct SankeyLabelColumn {
        internal SankeyLabelColumn(ChartRect bounds, ChartSankeyLabelPlacement side, double anchor) { Bounds = bounds; Side = side; Anchor = anchor; }
        internal ChartRect Bounds { get; }
        internal ChartSankeyLabelPlacement Side { get; }
        internal double Anchor { get; }
        internal double Alignment => Side == ChartSankeyLabelPlacement.Left ? 1 : Side == ChartSankeyLabelPlacement.Center ? .5 : 0;
    }
}
