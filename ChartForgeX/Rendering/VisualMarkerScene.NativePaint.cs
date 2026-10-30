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
        private readonly bool _ring;
        internal NativeMarkerPaint(ChartMarkerOptions markers, ChartColor fill, ChartColor? stroke, double width,
            VisualScenePaintBinding paint, double[]? dash, string role, ChartFillPattern pattern, ChartColor backdrop, string patternRole, bool ring = false) {
            _markers = markers; _fill = fill; _stroke = stroke; _width = width; _paint = paint; _dash = dash;
            _role = role; _pattern = pattern; _backdrop = backdrop; _patternRole = patternRole; _ring = ring;
        }
        internal NativeMarkerPaint Detached() => new(_markers.Clone(), _fill, _stroke, _width, _paint,
            _dash == null ? null : (double[])_dash.Clone(), _role, _pattern, _backdrop, _patternRole, _ring);
        internal bool Same(NativeMarkerPaint other) => _markers.Shape == other._markers.Shape && _markers.Enabled == other._markers.Enabled
            && _markers.StrokeDashOffset == other._markers.StrokeDashOffset
            && (_markers.CustomPath == null || other._markers.CustomPath == null ? _markers.CustomPath == null && other._markers.CustomPath == null
                : _markers.CustomPath.Commands.SequenceEqual(other._markers.CustomPath.Commands))
            && _fill.Equals(other._fill) && _stroke.Equals(other._stroke) && _width == other._width && _paint.Fill.Equals(other._paint.Fill)
            && _paint.Stroke.Equals(other._paint.Stroke) && (_dash ?? System.Array.Empty<double>()).SequenceEqual(other._dash ?? System.Array.Empty<double>())
            && _role == other._role && _pattern == other._pattern && _patternRole == other._patternRole && _backdrop.Equals(other._backdrop) && _ring == other._ring;
        // Matches OutlineExtent: an unoutlined marker's extent includes its surface ring.
        internal ChartRect Bounds(double x, double y, double radius) => ResolvedBounds(_markers, x, y, radius, _ring ? ChartMarkSurface.MarkerRing : _width / 2);
        internal void Paint(VisualSceneBuilder builder, double x, double y, double radius) {
            if (radius <= 0 || _markers.Enabled == false) return;
            var contour = ChartMarkerGeometry.Path(_markers, x, y, radius);
            // Design rule 5: a marker without an authored outline sits on a 2px ring of the surface behind it, so it reads
            // against a line or a neighbouring marker in every theme.
            if (_ring) {
                var ring = radius + ChartMarkSurface.MarkerRing;
                var surface = VisualChartPaint.Fill(SvgPaint.Of(_backdrop, SvgColorRole.Surface));
                if (_markers.CustomPath == null && _markers.Shape == ChartMarkerShape.Circle) builder.Ellipse(x, y, ring, ring, _backdrop, null, 0, _role + "-ring", paint: surface);
                else builder.Path(ChartMarkerGeometry.Path(_markers, x, y, ring), _backdrop, role: _role + "-ring", close: true, paint: surface);
            }
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
