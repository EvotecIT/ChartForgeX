using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void BuildGlyphSurface(TopologyNode node, TopologyIconShape shape, double x, double y,
        ChartColor accent, double scale, SvgColorRole role) {
        var tint = Color(StatusFill(accent.ToHexRgba(), _colors.Background.ToHexRgba(), .10), _colors.Surface);
        var paint = new VisualScenePaintBinding(SvgPaint.Mix(tint, _colors.Background, SvgColorRole.Surface, accent, role, .10), SvgPaint.Of(accent, role));
        var center = Point(new ChartPoint(x, y));
        var s = scale * _scale;
        if (shape == TopologyIconShape.Cloud) {
            _builder.Ellipse(center.X - 5 * s, center.Y, 7 * s, 7 * s, tint, accent, _context.Theme.AxisStrokeWidth * s, "topology-icon-surface", paint: paint);
            _builder.Ellipse(center.X + 4 * s, center.Y - 2 * s, 8 * s, 8 * s, tint, accent, _context.Theme.AxisStrokeWidth * s, "topology-icon-surface", paint: paint);
        } else if (shape == TopologyIconShape.Database || node.Kind == TopologyNodeKind.Database) {
            _builder.Ellipse(center.X, center.Y - 7 * s, 10 * s, 4 * s, tint, accent, _context.Theme.AxisStrokeWidth * s, "topology-icon-surface", paint: paint);
            foreach (var contour in ChartMapPathParser.ParseSubpaths(TopologyInfrastructureGlyphs.DatabaseBodyPath(x, y), 2)) {
                var commands = contour.Points.Select((p, i) => {
                    var point = Point(new ChartPoint(x + (p.X - x) * scale, y + (p.Y - y) * scale));
                    return i == 0 ? ChartPathCommand.MoveTo(point.X, point.Y) : ChartPathCommand.LineTo(point.X, point.Y);
                }).ToArray();
                _builder.Path(new ChartPath(commands), tint, accent, _context.Theme.AxisStrokeWidth * s, "topology-icon-surface", close: true, paint: paint);
            }
        } else {
            var mode = EffectiveNodeDisplayMode(node, _options);
            var size = (mode == TopologyNodeDisplayMode.Pill ? 18 : mode == TopologyNodeDisplayMode.Icon ? 26 : mode == TopologyNodeDisplayMode.Tile ? 24 : 22) * s;
            _builder.Rect(new ChartRect(center.X - size / 2, center.Y - size / 2, size, size), tint, accent,
                _context.Theme.AxisStrokeWidth * s, _context.Theme.BarRadius * s, "topology-icon-surface", paint: paint);
        }
    }
}
