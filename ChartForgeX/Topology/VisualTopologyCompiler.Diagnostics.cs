using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void BuildDiagnostics() {
        var report = TopologyLayoutDiagnostics.AnalyzePrepared(_chart, _options);
        using (_builder.PushGroup(null, "topology-layout-diagnostics")) {
            foreach (var group in report.Groups) Rectangle(group.Bounds, _colors.Accent, "topology-layout-group");
            foreach (var node in report.Nodes) {
                Rectangle(node.Bounds, _colors.Foreground, "topology-layout-node");
                foreach (var port in node.Ports) {
                    var point = Point(port.Position);
                    _builder.Ellipse(point.X, point.Y, 4 * _scale, 4 * _scale, _colors.Status.Medium.Fill, role: "topology-layout-port");
                }
            }
            foreach (var route in _routes.Values) foreach (var point in route) {
                var p = Point(point);
                _builder.Ellipse(p.X, p.Y, 2.5 * _scale, 2.5 * _scale, _colors.Status.Pass.Fill, role: "topology-layout-route-point");
            }
            foreach (var collision in report.Collisions) Rectangle(collision.Bounds, _colors.Status.Critical.Fill, "topology-layout-collision");
        }

        void Rectangle(ChartRect rectangle, ChartColor color, string role) =>
            _builder.Rect(Bounds(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height), fill: null, stroke: color, strokeWidth: _context.Theme.AxisStrokeWidth * _scale, role: role);
    }
}
