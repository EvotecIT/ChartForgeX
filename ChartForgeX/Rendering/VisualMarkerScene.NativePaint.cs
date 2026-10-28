using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualMarkerScene {
    /// <summary>Resolved native marker policy. Capture detaches options; ordinary paint uses the same stack value directly.</summary>
    internal readonly struct NativeMarkerPaint {
        private readonly ChartMarkerOptions _markers;
        private readonly ChartColor _fill, _backdrop;
        private readonly ChartColor? _stroke;
        private readonly double _width;
        private readonly VisualScenePaintBinding _paint;
        private readonly double[]? _dash;
        private readonly string _role, _patternRole;
        private readonly ChartFillPattern _pattern;
        internal NativeMarkerPaint(ChartMarkerOptions markers, ChartColor fill, ChartColor? stroke, double width,
            VisualScenePaintBinding paint, double[]? dash, string role, ChartFillPattern pattern, ChartColor backdrop, string patternRole) {
            _markers = markers; _fill = fill; _stroke = stroke; _width = width; _paint = paint; _dash = dash;
            _role = role; _pattern = pattern; _backdrop = backdrop; _patternRole = patternRole;
        }
        internal NativeMarkerPaint Detached() => new(_markers.Clone(), _fill, _stroke, _width, _paint,
            _dash == null ? null : (double[])_dash.Clone(), _role, _pattern, _backdrop, _patternRole);
        internal bool Same(NativeMarkerPaint other) => _markers.Shape == other._markers.Shape && _markers.Enabled == other._markers.Enabled
            && _markers.StrokeDashOffset == other._markers.StrokeDashOffset
            && (_markers.CustomPath == null || other._markers.CustomPath == null ? _markers.CustomPath == null && other._markers.CustomPath == null
                : _markers.CustomPath.Commands.SequenceEqual(other._markers.CustomPath.Commands))
            && _fill.Equals(other._fill) && _stroke.Equals(other._stroke) && _width == other._width && _paint.Fill.Equals(other._paint.Fill)
            && _paint.Stroke.Equals(other._paint.Stroke) && (_dash ?? System.Array.Empty<double>()).SequenceEqual(other._dash ?? System.Array.Empty<double>())
            && _role == other._role && _pattern == other._pattern && _patternRole == other._patternRole && _backdrop.Equals(other._backdrop);
        internal ChartRect Bounds(double x, double y, double radius) => ResolvedBounds(_markers, x, y, radius, _width / 2);
        internal void Paint(VisualSceneBuilder builder, double x, double y, double radius) {
            if (radius <= 0 || _markers.Enabled == false) return;
            var contour = ChartMarkerGeometry.Path(_markers, x, y, radius);
            if (_markers.CustomPath == null && _markers.Shape == ChartMarkerShape.Circle && (_dash == null || _dash.Length == 0))
                builder.Ellipse(x, y, radius, radius, _fill, _stroke, _width, _role, paint: _paint);
            else builder.Path(contour, _fill, _stroke, _width, _role, close: true, dash: _dash, paint: _paint, dashOffset: _markers.StrokeDashOffset);
            if (_pattern == ChartFillPattern.None) return;
            var background = ChartColorMath.Blend(_backdrop, ChartColor.FromRgb(_fill.R, _fill.G, _fill.B), _fill.A / 255d);
            var ink = ChartColorMath.AccessibleTextOnBackground(background).WithOpacity(ChartMarkSurface.HatchOpacity(_pattern));
            builder.Pattern(contour, _pattern, ink, role: _patternRole);
        }
    }
}
