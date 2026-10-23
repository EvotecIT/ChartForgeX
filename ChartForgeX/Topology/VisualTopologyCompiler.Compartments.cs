using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    /// <summary>Paints shared separators once on the containing surface; compartments keep semantic bounds only.</summary>
    private void BuildCompartmentSeparators(TopologyGroup parent) {
        var children = _chart.Groups.Where(group => group.ParentGroupId == parent.Id
            && group.Presentation == TopologyGroupPresentation.Compartment).ToArray();
        if (children.Length < 2) return;
        var direction = parent.LayoutDirection;
        var ancestor = parent;
        while (!direction.HasValue && ancestor.ParentGroupId != null) {
            ancestor = _chart.Groups.First(group => group.Id == ancestor.ParentGroupId);
            direction = ancestor.LayoutDirection;
        }
        var horizontalFlow = (direction ?? _chart.LayoutDirection) is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft;
        for (var index = 1; index < children.Length; index++) {
            var previous = children[index - 1];
            var next = children[index];
            var from = horizontalFlow
                ? new ChartPoint(parent.X + 12, (previous.Y + previous.Height + next.Y) / 2)
                : new ChartPoint((previous.X + previous.Width + next.X) / 2, children.Min(group => group.Y));
            var to = horizontalFlow
                ? new ChartPoint(parent.X + parent.Width - 12, from.Y)
                : new ChartPoint(from.X, children.Max(group => group.Y + group.Height));
            from = Point(from);
            to = Point(to);
            _builder.Line(from.X, from.Y, to.X, to.Y, _colors.Border, _context.Theme.AxisStrokeWidth * _scale,
                "topology-compartment-separator", dash: new[] { 5 * _scale, 4 * _scale },
                paint: new VisualScenePaintBinding(stroke: BorderPaint(_colors.Border)));
        }
    }
}
