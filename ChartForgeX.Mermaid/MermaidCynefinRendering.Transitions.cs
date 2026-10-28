using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

public static partial class MermaidCynefinRendering {
    // The pinned family uses one quadratic between domain centers with a perpendicular
    // midpoint offset of .15 * length. Degree elevation preserves that exact curve in
    // the canonical cubic owner; no new curve evaluator or renderer is introduced.
    private static void ApplyTransitionPath(TopologyChart chart, TopologyEdge edge) {
        var from = chart.Nodes.Single(node => node.Id == edge.SourceNodeId); var to = chart.Nodes.Single(node => node.Id == edge.TargetNodeId);
        var x1 = from.X + from.Width / 2; var y1 = from.Y + from.Height / 2;
        var x2 = to.X + to.Width / 2; var y2 = to.Y + to.Height / 2;
        var cx = (x1 + x2) / 2 - (y2 - y1) * .15; var cy = (y1 + y2) / 2 + (x2 - x1) * .15;
        string N(double value) => value.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
        edge.PathGeometry = new TopologyPathGeometry("M " + N(x1) + " " + N(y1) + " C " +
            N(x1 + (cx - x1) * (2d / 3)) + " " + N(y1 + (cy - y1) * (2d / 3)) + " " +
            N(x2 + (cx - x2) * (2d / 3)) + " " + N(y2 + (cy - y2) * (2d / 3)) + " " + N(x2) + " " + N(y2));
        edge.Routing = TopologyEdgeRouting.Curved;
        edge.LabelPosition = new TopologyLabelPosition(new ChartForgeX.Primitives.ChartPoint(cx, cy - 6), TopologyLabelVerticalAnchor.TextBaseline);
        chart.DefaultRenderOptions!.IncludeEdgeLabelBackplates = false;
        edge.TextStyle ??= new ChartForgeX.Typography.TextStyleOverride(); edge.TextStyle.FontWeight = "400";
        edge.TargetMarkerStyle = new TopologyMarkerStyle { Scale = .75 * (edge.StrokeWidth ?? 2), Offset = .6 * (edge.StrokeWidth ?? 2), StrokeWidth = 0 };

    }
}
