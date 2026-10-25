using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void BuildGlyphSurface(TopologyNode node, TopologyIconShape shape, double x, double y,
        ChartColor accent, double scale, SvgColorRole role, SvgPaint sourcePaint, ChartColor baseColor, double opacity, ChartColor fallback, ChartPoint? canonicalAnchor = null) {
        var baseTint = ChartForgeX.Rendering.ChartColorMath.BlendPremultiplied(_colors.Background, baseColor, .10);
        var tint = ChartForgeX.Rendering.ChartColorMath.WithOpacity(baseTint, opacity);
        var policy = GlyphAccentSource(node);
        var authored = TopologyNodeAccent.Authored(node, ResolveNodeIcon(node, _options), policy);
        var source = string.IsNullOrWhiteSpace(authored) ? sourcePaint : SourcePaint(authored, baseColor, role, fallback: fallback);
        var background = BackgroundPaint(_colors.Background);
        var tintPaint = TopologyNodeAccent.UsesPalette(node, policy) || source.HasCssVariable || background.HasCssVariable ? SvgPaint.Mix(baseTint, background, source, .10)
            : SvgPaint.Mix(baseTint, _colors.Background, SvgColorRole.Surface, baseColor, role, .10);
        var paint = new VisualScenePaintBinding(opacity == 1 ? tintPaint : tintPaint.WithOpacity(tint, opacity), sourcePaint);
        var center = Point(new ChartPoint(x, y));
        var s = scale * _scale;
        foreach (var part in TopologyGlyphSurfaceGeometry.Parts(node,shape,_options,x,y,scale,_context.Theme.BarRadius,canonicalAnchor)) {
            if (part.Type == TopologyGlyphSurfaceGeometry.Kind.Ellipse)
                _builder.Ellipse(part.X == 0 ? center.X : center.X + part.X * s, part.Y == 0 ? center.Y : center.Y + part.Y * s,
                    part.Width * s, part.Height * s, tint, accent, _context.Theme.AxisStrokeWidth * s, "topology-icon-surface", paint:paint);
            else if (part.Type == TopologyGlyphSurfaceGeometry.Kind.Path)
                _builder.Path(Transform(part.Path!), tint, accent, _context.Theme.AxisStrokeWidth * s, "topology-icon-surface", close:true, paint:paint);
            else {
                var size = part.Width * s;
                _builder.Rect(new ChartRect(center.X - size / 2, center.Y - size / 2, size, size), tint, accent,
                    _context.Theme.AxisStrokeWidth * s, part.Radius * s, "topology-icon-surface", paint:paint);
            }
        }
    }
}
