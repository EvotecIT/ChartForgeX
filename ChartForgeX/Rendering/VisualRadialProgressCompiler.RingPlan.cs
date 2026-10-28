using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualRadialProgressCompiler {
    /// <summary>Retains the existing concentric ring painter, measured average center and authored point facts.</summary>
    private sealed class RingPlan : IVisualValuePlan {
        private readonly VisualRenderContext _context;
        private readonly VisualThemeColors _colors;
        private readonly VisualValueTextPolicy _text;
        private readonly string _seriesKey, _caption, _ordinaryAverage;
        private readonly string[] _labels, _displays;
        private readonly string?[] _ids, _authored;
        private readonly double[] _values;
        private readonly ChartColor[] _paints;
        private readonly SvgPaint[] _bindings;
        private readonly bool _local, _showCenter, _pin;
        private readonly ChartSeriesState _state;
        private readonly double _cx, _cy, _outer, _strokeScale;
        private readonly ChartValueFormat? _format;
        private readonly (string Notation, string Format, string Culture)? _formatKey;
        private readonly IReadOnlyList<VisualValueReading> _readings;
        internal RingPlan(Chart chart, VisualRenderContext context, double cx, double cy, double outer, VisualThemeColors colors) {
            var series = chart.Series[0]; _context = context; _colors = colors; _cx = cx; _cy = cy; _outer = outer;
            _seriesKey = series.Id.Value; _local = series.Id.IsAutomatic; _caption = series.Name; _state = series.StateRole;
            _pin = chart.Options.PinStateColorsInForcedColors && _state != ChartSeriesState.None;
            _showCenter = series.ResolveDataLabels(chart.Options, defaultVisible: true) && chart.Options.ShowProgressRingCenterLabel;
            _strokeScale = chart.Options.RadialProgressStrokeScale; _values = series.RenderPoints.Select(point => point.Y).ToArray();
            _ordinaryAverage = ChartNumericFormatter.FormatValue(chart.Options, _values.Average());
            _labels = series.RenderPoints.Select((point, index) => VisualRadialPrimitives.Label(chart, point, index)).ToArray();
            _displays = _values.Select(value => ChartNumericFormatter.FormatValue(chart.Options, value)).ToArray();
            _paints = _values.Select((_, index) => VisualRadialPrimitives.Color(series, index, colors)).ToArray();
            _bindings = _paints.Select((color, index) => VisualChartPaint.Series(series, color, index)).ToArray();
            _ids = _values.Select((_, index) => series.ObservationIds.TryGetValue(index, out var id) ? id.Value : null).ToArray();
            _authored = _values.Select((_, index) => index < series.PointLabels.Count ? series.PointLabels[index] : null).ToArray();
            _text = new VisualValueTextPolicy(chart, series, context); _format = chart.Options.ValueFormat.CapturedValuePolicy;
            if (_format != null) {
                try { _formatKey = _format.PortablePolicy(); } catch (NotSupportedException) { } catch (ArgumentException) { }
            }
            _readings = Array.AsReadOnly(_values.Select((value, index) => new VisualValueReading(_ids[index], value, 0, 100)).ToArray());
        }
        string IVisualValuePlan.Kind => "progress-ring-value";
        string IVisualValuePlan.SeriesKey => _seriesKey;
        VisualSize IVisualValuePlan.Size => _context.Layout.Size;
        FontSpec IVisualValuePlan.Font => _context.Font;
        IReadOnlyList<VisualValueReading> IVisualValuePlan.Readings => _readings;
        bool IVisualValuePlan.AllowsOutOfDomainReadings => false;
        bool IVisualValuePlan.SamePolicy(IVisualValuePlan other) => other is RingPlan ring && !_local && !ring._local
            && _ids.All(id => id != null) && _ids.SequenceEqual(ring._ids) && _authored.SequenceEqual(ring._authored)
            && _seriesKey == ring._seriesKey && _caption == ring._caption && _state == ring._state && _pin == ring._pin
            && _cx == ring._cx && _cy == ring._cy && _outer == ring._outer && _strokeScale == ring._strokeScale && _showCenter == ring._showCenter
            && _labels.SequenceEqual(ring._labels) && _paints.SequenceEqual(ring._paints)
            && _bindings.Select(paint => (paint.Value, paint.IsRaw)).SequenceEqual(ring._bindings.Select(paint => (paint.Value, paint.IsRaw)))
            && _formatKey.HasValue && _formatKey.Equals(ring._formatKey) && _text.SamePolicy(ring._text)
            && VisualValuePolicy.SameNativeTheme(_context, ring._context);
        void IVisualValuePlan.Paint(VisualSceneBuilder builder, double[]? readings, bool captured) {
            var values = readings ?? _values; var value = readings == null ? _ordinaryAverage : _format!.Format(values.Average());
            var style = _text.Style(_colors.Foreground, _context.Theme.Typography.CenterValueSize, 700);
            var captionStyle = _text.Style(_colors.MutedForeground, _context.Theme.Typography.DataLabelSize);
            var requested = _showCenter ? RequestedCenterRadius(builder, _context, value, _caption, style, captionStyle,
                captured ? _text.Face(style) : null, captured ? _text.Face(captionStyle) : null) : 0;
            var layout = ProgressRingLayout.Create(_outer, values.Length, _strokeScale, requested);
            for (var index = 0; index < values.Length; index++) {
                var raw = values[index]; var label = _labels[index]; var formatted = readings == null ? _displays[index] : _format!.Format(raw);
                var radius = layout.RadiusAt(index); var color = _paints[index];
                var bounds = new ChartRect(_cx - radius - layout.StrokeWidth / 2, _cy - radius - layout.StrokeWidth / 2,
                    radius * 2 + layout.StrokeWidth, radius * 2 + layout.StrokeWidth);
                builder.AddRegion(new VisualSemanticRegion(Id(index), "progress-ring-ring", bounds, label + ": " + formatted));
                var metadata = new Dictionary<string, string> {
                    ["data-cfx-series"] = "0", ["data-cfx-source-point"] = index.ToString(CultureInfo.InvariantCulture), ["data-cfx-source-count"] = "1",
                    ["data-cfx-pin-state-colors"] = _pin ? "true" : "false", ["data-cfx-point"] = index.ToString(CultureInfo.InvariantCulture),
                    ["data-cfx-label"] = label, ["data-cfx-value"] = raw.ToString("R", CultureInfo.InvariantCulture), ["data-cfx-min"] = "0", ["data-cfx-max"] = "100"
                };
                if (captured) {
                    metadata["data-cfx-series-key"] = _seriesKey; metadata["data-cfx-series-scope"] = _local ? "local" : "shared";
                    if (_ids[index] != null) metadata["data-cfx-observation-id"] = _ids[index]!;
                    if (_authored[index] != null) metadata["data-cfx-authored-label"] = _authored[index]!;
                }
                using (builder.PushGroup(Id(index), "progress-ring-point", metadata)) {
                    VisualRadialPrimitives.Arc(builder, _cx, _cy, radius, layout.StrokeWidth, -Math.PI / 2, Math.PI * 2, _colors.Neutral2,
                        "progress-ring-track", paint: SvgPaint.Of(_colors.Neutral2, SvgColorRole.Surface));
                    VisualRadialPrimitives.Arc(builder, _cx, _cy, radius, layout.StrokeWidth, -Math.PI / 2, Math.PI * 2 * raw / 100,
                        color, "progress-ring-ring", round: true, paint: _bindings[index]);
                }
            }
            builder.Ellipse(_cx, _cy, layout.CenterRadius, layout.CenterRadius, _colors.Surface, _colors.Border, role: "progress-ring-center",
                paint: new VisualScenePaintBinding(SvgPaint.Of(_colors.Surface, SvgColorRole.Surface), SvgPaint.Of(_colors.Border, SvgColorRole.Surface)));
            if (!_showCenter) return;
            style = _text.Style(_colors.Foreground, Math.Min(_context.Theme.Typography.CenterValueSize, Math.Max(.1, layout.CenterRadius * CenterFontSizePerRadius)), 700);
            captionStyle = _text.Style(_colors.MutedForeground, Math.Min(_context.Theme.Typography.DataLabelSize, Math.Max(.1, layout.CenterRadius * CenterFontSizePerRadius)));
            Center(builder, _context, _cx, _cy, layout.CenterRadius, value, _caption, style, captionStyle, "progress-ring",
                captured ? _text.Face(style) : null, captured ? _text.Face(captionStyle) : null);
        }
    }
}
