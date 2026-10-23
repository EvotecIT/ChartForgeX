using System;
using System.Linq;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    /// <summary>Places ordered compartments across the flow axis without introducing graph edges or rank facts.</summary>
    private static void PlaceCompartments(TopologyChart chart, TopologyGroup parent, TopologyGroup[] children,
        int directNodeCount, TopologyLayoutDirection direction, TopologyRenderOptions options,
        Action<TopologyGroup, double, double> shift) {
        if (directNodeCount != 0 || children.Any(group => group.Presentation != TopologyGroupPresentation.Compartment))
            throw new ArgumentException("A compartmented group contains only compartments; put nodes and other groups inside them.");
        const double padding = 24;
        const double gap = 24;
        var horizontalFlow = direction is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft;
        var contentWidth = horizontalFlow ? children.Max(group => group.Width)
            : children.Sum(group => group.Width) + (children.Length - 1) * gap;
        var contentHeight = horizontalFlow ? children.Sum(group => group.Height) + (children.Length - 1) * gap
            : children.Max(group => group.Height);
        var labelWidth = parent.Presentation == TopologyGroupPresentation.Compartment ? 0
            : EstimateTextWidth(parent.Label, 12, true, chart.TextMeasurement) + padding * 2;
        parent.X = 0;
        parent.Y = 0;
        parent.Width = Math.Max(Math.Max(190, contentWidth + padding * 2), labelWidth);
        var insetY = parent.Presentation != TopologyGroupPresentation.Compartment && TopologyGroupHeader.IsDrawn(options)
            ? TopologyGroupHeader.ReservedBounds(chart, parent, options).Bottom + 12 : padding;
        parent.Height = Math.Max(80, contentHeight + insetY + padding);
        var cursor = horizontalFlow ? insetY : padding;
        foreach (var child in children) {
            // Reversing flow reverses ranks within each compartment, never their authored order.
            if (horizontalFlow) child.Width = contentWidth;
            else child.Height = contentHeight;
            shift(child, horizontalFlow ? padding : cursor, horizontalFlow ? cursor : insetY);
            cursor += (horizontalFlow ? child.Height : child.Width) + gap;
        }
    }
}
