using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void BuildGeographicFrame(ChartRect map, bool soft) {
        using var frame = _builder.PushGroup(null, "topology-geographic-frame", new Dictionary<string, string> {
            ["data-cfx-projection"] = TopologyMapProjection.ProjectionName,
            ["data-cfx-viewport"] = _chart.MapViewport.Name,
            ["data-cfx-viewport-min-longitude"] = Number(_chart.MapViewport.MinimumLongitude),
            ["data-cfx-viewport-max-longitude"] = Number(_chart.MapViewport.MaximumLongitude),
            ["data-cfx-viewport-min-latitude"] = Number(_chart.MapViewport.MinimumLatitude),
            ["data-cfx-viewport-max-latitude"] = Number(_chart.MapViewport.MaximumLatitude)
        });
        var amount = soft ? .035 : .10;
        var fill = Color(StatusFill(_colors.Accent.ToHexRgba(), _colors.Background.ToHexRgba(), amount), _colors.Surface);
        _builder.Rect(Bounds(map.X, map.Y, map.Width, map.Height), _context.Frame.TransparentBackground ? null : fill,
            _colors.Border, MinimumReadableFineStrokeWidth * _scale, _context.Theme.BarRadius * _scale,
            "topology-map-surface", paint: new ChartForgeX.Rendering.VisualScenePaintBinding(
                _context.Frame.TransparentBackground ? null : SvgPaint.Mix(fill, _colors.Background, SvgColorRole.Surface, _colors.Accent, SvgColorRole.Any, amount),
                SvgPaint.Of(_colors.Border, SvgColorRole.Surface)));
        for (var index = 1; index < 4; index++) {
            var longitude = _chart.MapViewport.MinimumLongitude + (_chart.MapViewport.MaximumLongitude - _chart.MapViewport.MinimumLongitude) * index / 4;
            var projected = TopologyMapProjection.Project(map, _chart.MapViewport, longitude, _chart.MapViewport.MinimumLatitude);
            Line(new ChartPoint(projected.X, map.Top), new ChartPoint(projected.X, map.Bottom), soft ? .22 : .42, "longitude", longitude);
        }
        for (var index = 1; index < 3; index++) {
            var latitude = _chart.MapViewport.MinimumLatitude + (_chart.MapViewport.MaximumLatitude - _chart.MapViewport.MinimumLatitude) * index / 3;
            var projected = TopologyMapProjection.Project(map, _chart.MapViewport, _chart.MapViewport.MinimumLongitude, latitude);
            Line(new ChartPoint(map.Left, projected.Y), new ChartPoint(map.Right, projected.Y), soft ? .18 : .34, "latitude", latitude);
        }
        void Line(ChartPoint first, ChartPoint last, double opacity, string axis, double coordinate) {
            var a = Point(first); var b = Point(last); var stroke = _colors.Border.WithOpacity(opacity);
            using var group = _builder.PushGroup(null, null, new Dictionary<string, string> {
                ["data-cfx-axis"] = axis, ["data-cfx-" + axis] = Number(coordinate)
            });
            _builder.Line(a.X, a.Y, b.X, b.Y, stroke, MinimumReadableFineStrokeWidth * _scale,
                "topology-geographic-graticule", paint: Paint(stroke: stroke, strokeRole: SvgColorRole.Grid));
        }
    }
}
