using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualScalarProgressCompiler {
    /// <summary>Retains the native circle's typed reading, ratio-dependent status and paint, fixed layout and factual text.</summary>
    internal sealed class CirclePlan : IVisualValuePlan {
        private readonly VisualRenderContext _context;
        private readonly VisualThemeColors _colors;
        private readonly ChartRect _plot, _valueArea, _titleArea, _statusArea;
        private readonly string _seriesKey, _caption, _display;
        private readonly string? _observationId, _authoredLabel;
        private readonly bool _local, _showLabels, _showStatus;
        private readonly ChartSeriesState _stateRole;
        private readonly ChartColor? _pointColor, _seriesColor;
        private readonly double _raw, _minimum, _maximum, _cx, _cy, _radius, _stroke, _inner, _statusHeight, _marker, _radiusScale, _strokeScale;
        private readonly ChartValueFormat? _format;
        private readonly (string Notation, string Format, string Culture)? _formatKey;
        private readonly TextStyle _valueStyle, _titleStyle, _statusStyle;
        private readonly VisualSceneTextFace _valueFace, _titleFace, _statusFace;
        private readonly IReadOnlyList<VisualValueReading> _readings;
        private bool CanTransition => !_local && _observationId != null && _formatKey.HasValue
            && _valueStyle.Baseline == TextBaseline.Normal && _titleStyle.Baseline == TextBaseline.Normal && _statusStyle.Baseline == TextBaseline.Normal;

        internal CirclePlan(Chart chart, VisualRenderContext context, ChartRect plot, VisualThemeColors colors) {
            var series = chart.Series[0]; _context = context; _colors = colors; _plot = plot;
            Range(series, out var min, out var max); _minimum = min; _maximum = max; _raw = series.RenderPoints[0].Y;
            _seriesKey = series.Id.Value; _local = series.Id.IsAutomatic; _caption = series.Name; _display = Value(chart, series, 0, _raw);
            _observationId = series.ObservationIds.TryGetValue(0, out var id) ? id.Value : null;
            _authoredLabel = series.PointLabels.Count > 0 ? series.PointLabels[0] : null;
            _pointColor = series.PointColors.Count > 0 ? series.PointColors[0] : null; _seriesColor = series.Color; _stateRole = series.StateRole;
            _showLabels = series.ResolveDataLabels(chart.Options, defaultVisible: true); _showStatus = chart.Options.ShowCircleStatusLabel;
            _statusHeight = _showLabels && _showStatus ? Math.Min(plot.Height * .2, context.Theme.Typography.DataLabelSize * 1.5 + context.Theme.Spacing) : 0;
            var radius = Math.Max(0, Math.Min(plot.Width / 2 - context.Theme.Spacing, (plot.Height - _statusHeight) / 2 - context.Theme.Spacing));
            _radiusScale = chart.Options.CircleRadiusScale; _strokeScale = chart.Options.CircleStrokeScale;
            var requested = Math.Min(plot.Width, plot.Height) * .28 * _radiusScale;
            var halfStrokeFactor = .08 * _strokeScale;
            _radius = Math.Min(requested, radius / (1 + halfStrokeFactor)); _stroke = _radius * .16 * _strokeScale;
            _cx = plot.Left + plot.Width / 2; _cy = plot.Top + (plot.Height - _statusHeight) / 2;
            _inner = Math.Max(0, _radius - _stroke * .82); _marker = Math.Min(context.Theme.MarkerRadius, _statusHeight / 4);
            _valueArea = new ChartRect(_cx - _inner * .8, _cy - _inner * .7, _inner * 1.6, _inner * .7);
            _titleArea = new ChartRect(_cx - _inner * .8, _cy, _inner * 1.6, _inner * .7);
            _statusArea = new ChartRect(_cx - plot.Width * .1, plot.Bottom - _statusHeight, plot.Width * .3, _statusHeight);
            _valueStyle = CaptionStyle(_valueArea, colors.Foreground, context.Theme.Typography.TitleSize, 700);
            _titleStyle = CaptionStyle(_titleArea, colors.MutedForeground, context.Theme.Typography.DataLabelSize);
            _statusStyle = CaptionStyle(_statusArea, colors.MutedForeground, context.Theme.Typography.DataLabelSize);
            _valueFace = new VisualSceneTextFace(_valueStyle); _titleFace = new VisualSceneTextFace(_titleStyle); _statusFace = new VisualSceneTextFace(_statusStyle);
            _format = chart.Options.ValueFormat.CapturedValuePolicy;
            if (_format != null) {
                try { _formatKey = _format.PortablePolicy(); }
                catch (NotSupportedException) { _formatKey = null; }
                catch (ArgumentException) { _formatKey = null; }
            }
            _readings = Array.AsReadOnly(new[] { new VisualValueReading(_observationId, _raw, _minimum, _maximum) });
            TextStyle CaptionStyle(ChartRect area, ChartColor color, double size, int weight = 400) =>
                Style(chart, context, series, 0, color, Math.Min(size, Math.Max(.1, area.Height / 1.3)), weight);
        }

        string IVisualValuePlan.Kind => "circle-value";
        string IVisualValuePlan.SeriesKey => _seriesKey;
        VisualSize IVisualValuePlan.Size => _context.Layout.Size;
        FontSpec IVisualValuePlan.Font => _context.Font;
        IReadOnlyList<VisualValueReading> IVisualValuePlan.Readings => _readings;
        bool IVisualValuePlan.AllowsOutOfDomainReadings => true;
        bool IVisualValuePlan.SamePolicy(IVisualValuePlan other) => other is CirclePlan circle && CanTransition && circle.CanTransition
            && _seriesKey == circle._seriesKey && _observationId == circle._observationId && _caption == circle._caption && _authoredLabel == circle._authoredLabel
            && _minimum == circle._minimum && _maximum == circle._maximum && _stateRole == circle._stateRole
            && Nullable.Equals(_pointColor, circle._pointColor) && Nullable.Equals(_seriesColor, circle._seriesColor)
            && _showLabels == circle._showLabels && _showStatus == circle._showStatus && _plot.Equals(circle._plot)
            && _cx == circle._cx && _cy == circle._cy && _radius == circle._radius && _stroke == circle._stroke && _inner == circle._inner
            && _statusHeight == circle._statusHeight && _marker == circle._marker && _radiusScale == circle._radiusScale && _strokeScale == circle._strokeScale
            && _formatKey.Equals(circle._formatKey) && SameColors(circle)
            && VisualValuePolicy.SameText(_valueFace, _valueStyle, circle._valueFace, circle._valueStyle)
            && VisualValuePolicy.SameText(_titleFace, _titleStyle, circle._titleFace, circle._titleStyle)
            && VisualValuePolicy.SameText(_statusFace, _statusStyle, circle._statusFace, circle._statusStyle);

        // Check the palette between endpoints as well: both endpoint values can skip
        // the warning interval even though an intermediate reading must paint it.
        private bool SameColors(CirclePlan other) {
            if (!_colors.Surface.Equals(other._colors.Surface) || !_colors.Border.Equals(other._colors.Border) || !_colors.Accent.Equals(other._colors.Accent)) return false;
            foreach (var state in new[] { ChartSeriesState.Danger, ChartSeriesState.Warning, ChartSeriesState.Success, _stateRole })
                if (!ChartSeriesColours.State(state, _colors, _colors.Accent).Equals(ChartSeriesColours.State(state, other._colors, other._colors.Accent))) return false;
            return true;
        }

        // Ordinary output and captured observations share the original native painter.
        // Only the raw typed reading changes; status text, color, clamping, geometry
        // and text fitting remain native and use the retained policy and faces.
        void IVisualValuePlan.Paint(VisualSceneBuilder builder, double[]? readings, bool captured) {
            if (_radius <= 0) { builder.AddDiagnostic(new VisualDiagnostic("circle.insufficient-space", "No radius remains for the circle chart.")); return; }
            var raw = readings == null ? _raw : readings[0];
            var ratio = VisualRadialPrimitives.Clamp((VisualRadialPrimitives.Clamp(raw, _minimum, _maximum) - _minimum) / (_maximum - _minimum));
            var status = ratio < .6 ? "negative" : ratio < .8 ? "warning" : "positive";
            var state = _stateRole == ChartSeriesState.None ? RatioState(ratio) : _stateRole;
            var color = CircleColor(ratio, _stateRole, _pointColor, _seriesColor, _colors);
            var value = readings == null ? _display : _authoredLabel ?? _format!.Format(raw);
            builder.AddRegion(new VisualSemanticRegion(Id(0), "circle", new ChartRect(_cx - _radius - _stroke / 2, _cy - _radius - _stroke / 2, _radius * 2 + _stroke, _radius * 2 + _stroke), _caption + ": " + value));
            var metadata = new Dictionary<string, string> {
                ["data-cfx-series"] = "0", ["data-cfx-source-point"] = "0", ["data-cfx-source-count"] = "1",
                ["data-cfx-value"] = N(raw), ["data-cfx-min"] = N(_minimum), ["data-cfx-max"] = N(_maximum), ["data-cfx-percent"] = N(ratio),
                ["data-cfx-status"] = status, ["data-cfx-state"] = state.ToString(), ["data-cfx-full-label"] = value,
                ["data-cfx-radius-scale"] = N(_radiusScale), ["data-cfx-stroke-scale"] = N(_strokeScale)
            };
            if (captured) {
                metadata["data-cfx-series-key"] = _seriesKey; metadata["data-cfx-series-scope"] = _local ? "local" : "shared";
                if (_observationId != null) metadata["data-cfx-observation-id"] = _observationId;
            }
            using (builder.PushGroup(Id(0), "circle-chart", metadata)) {
                VisualRadialPrimitives.Arc(builder, _cx, _cy, _radius, _stroke, -Math.PI / 2, Math.PI * 2, _colors.Border, "circle-track", paint: SvgPaint.Of(_colors.Border, SvgColorRole.Surface));
                var valuePaint = SvgPaint.Of(color, _pointColor.HasValue || _seriesColor.HasValue ? SvgColorRole.Series : SvgColorRole.Status);
                VisualRadialPrimitives.Arc(builder, _cx, _cy, _radius, _stroke, -Math.PI / 2, Math.PI * 2 * ratio, color, "circle-value", round: true, paint: valuePaint);
                builder.Ellipse(_cx, _cy, _inner, _inner, _colors.Surface, _colors.Border, role: "circle-center", paint: new VisualScenePaintBinding(SvgPaint.Of(_colors.Surface, SvgColorRole.Surface), SvgPaint.Of(_colors.Border, SvgColorRole.Surface)));
                if (!_showLabels) return;
                Caption(value, _valueArea, _valueStyle, _valueFace, "circle-label", "-value");
                Caption(_caption, _titleArea, _titleStyle, _titleFace, "circle-title", "-title");
                if (_showStatus) {
                    var markerColor = ChartSeriesColours.State(state, _colors, color);
                    builder.Ellipse(_cx - _plot.Width * .15, _plot.Bottom - _statusHeight + _statusHeight / 2, _marker, _marker, markerColor, role: "circle-status-marker", paint: VisualChartPaint.Fill(markerColor, SvgColorRole.Status));
                    Caption(status, _statusArea, _statusStyle, _statusFace, "circle-status-label", "-status");
                }
            }
            void Caption(string text, ChartRect area, TextStyle style, VisualSceneTextFace face, string role, string suffix) =>
                VisualRadialPrimitives.Text(builder, text, area, style, role, Id(0) + suffix, capturedFace: captured ? face : null);
        }
    }
}
