using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualScalarProgressCompiler {
    /// <summary>Detached Progress policy shared by ordinary paint and bounded native value frames.</summary>
    internal sealed class ProgressPlan : IVisualValuePlan {
        internal sealed class Row {
            internal string Label = string.Empty, Display = string.Empty;
            internal string? FixedDisplay, ObservationId;
            internal double Value, Minimum, Maximum;
            internal bool ExplicitDomain;
            internal ChartColor Color;
            internal SvgPaint Paint;
            internal TextStyle MeasureStyle = null!, PaintStyle = null!;
            internal VisualSceneTextFace MeasureFace = null!, PaintFace = null!;
        }
        private readonly VisualRenderContext _context;
        private readonly ChartRect _plot;
        private readonly VisualThemeColors _colors;
        private readonly string _seriesKey;
        private readonly bool _local, _showValues, _showHandles, _typed;
        private readonly double _maximum, _thicknessRatio, _trackOpacity;
        private readonly ChartValueFormat? _format;
        private readonly (string Notation, string Format, string Culture)? _formatKey;
        private readonly TextStyle _tickMeasureStyle, _tickPaintStyle;
        private readonly VisualSceneTextFace _tickMeasureFace, _tickPaintFace;
        internal readonly Row[] Rows;
        private readonly IReadOnlyList<VisualValueReading> _readings;
        string IVisualValuePlan.Kind => "progress-value";
        string IVisualValuePlan.SeriesKey => SeriesKey;
        VisualSize IVisualValuePlan.Size => Size;
        FontSpec IVisualValuePlan.Font => Font;
        IReadOnlyList<VisualValueReading> IVisualValuePlan.Readings => _readings;
        bool IVisualValuePlan.AllowsOutOfDomainReadings => false;
        bool IVisualValuePlan.SamePolicy(IVisualValuePlan other) => other is ProgressPlan progress && SamePolicy(progress);
        void IVisualValuePlan.Paint(VisualSceneBuilder builder, double[]? readings, bool captured) => Paint(builder, readings, captured);
        internal VisualSize Size => _context.Layout.Size;
        internal FontSpec Font => _context.Font;
        internal string SeriesKey => _seriesKey;
        internal bool CanTransition => _typed && !_local && _formatKey.HasValue
            && Rows.All(row => row.ObservationId != null && row.MeasureStyle.Baseline == TextBaseline.Normal
                && row.PaintStyle.Baseline == TextBaseline.Normal)
            && _tickMeasureStyle.Baseline == TextBaseline.Normal && _tickPaintStyle.Baseline == TextBaseline.Normal;

        internal ProgressPlan(Chart chart, VisualRenderContext context, ChartRect plot, VisualThemeColors colors) {
            var series = chart.Series[0];
            // Preserve the incumbent callback/order boundary before freezing resolved policy.
            var labels = series.IsTypedProgress ? series.ProgressItems.Select(item => item.Label).ToArray()
                : series.RenderPoints.Select(point => Category(chart, point.X)).ToArray();
            var displays = series.RenderPoints.Select((point, index) => Value(chart, series, index, point.Y)).ToArray();
            _context = context; _plot = plot; _colors = colors;
            _seriesKey = series.Id.Value; _local = series.Id.IsAutomatic; _typed = series.IsTypedProgress;
            _maximum = chart.Options.ProgressMaximum; _thicknessRatio = chart.Options.ProgressBarThicknessRatio;
            _trackOpacity = chart.Options.ProgressTrackOpacity; _showHandles = chart.Options.ShowProgressHandles;
            _showValues = series.ResolveDataLabels(chart.Options, defaultVisible: true) && chart.Options.ShowProgressValues;
            _format = chart.Options.ValueFormat.CapturedValuePolicy;
            if (_format != null) {
                try { _formatKey = _format.PortablePolicy(); }
                catch (NotSupportedException) { _formatKey = null; }
                catch (ArgumentException) { _formatKey = null; }
            }
            var rowHeight = plot.Height / series.RenderPoints.Count;
            _tickMeasureStyle = Style(chart, context, series, -1, colors.MutedForeground, context.Theme.Typography.AxisSize, 400, true);
            _tickPaintStyle = Style(chart, context, series, -1, colors.MutedForeground,
                Math.Min(context.Theme.Typography.AxisSize, Math.Max(.1, rowHeight / 1.3)), 400, true);
            _tickPaintStyle.Alignment = TextAlignment.Center;
            _tickMeasureFace = new VisualSceneTextFace(_tickMeasureStyle); _tickPaintFace = new VisualSceneTextFace(_tickPaintStyle);
            Rows = new Row[series.RenderPoints.Count];
            for (var index = 0; index < Rows.Length; index++) {
                var raw = series.RenderPoints[index].Y;
                var item = series.IsTypedProgress ? series.ProgressItems[index] : (ChartProgressItem?)null;
                var color = index < series.PointColors.Count && series.PointColors[index].HasValue ? series.PointColors[index]!.Value
                    : item?.Color ?? ChartSeriesColours.CategoryPoint(series, index, colors);
                var measured = Style(chart, context, series, index, colors.Foreground, context.Theme.Typography.DataLabelSize, 700);
                var painted = Style(chart, context, series, index, colors.Foreground,
                    Math.Min(context.Theme.Typography.DataLabelSize, Math.Max(.1, rowHeight / 1.3)), 700);
                painted.Alignment = TextAlignment.Center;
                Rows[index] = new Row {
                    Label = labels[index],
                    Display = displays[index],
                    FixedDisplay = index < series.PointLabels.Count ? series.PointLabels[index] : null,
                    Value = raw, Minimum = item?.Minimum ?? 0, Maximum = item?.Maximum ?? _maximum,
                    ExplicitDomain = item?.Maximum != null, Color = color,
                    Paint = VisualChartPaint.Series(series, color, index, explicitOverride: item?.Color.HasValue == true),
                    ObservationId = series.ObservationIds.TryGetValue(index, out var id) ? id.Value : null,
                    MeasureStyle = measured, PaintStyle = painted,
                    MeasureFace = new VisualSceneTextFace(measured), PaintFace = new VisualSceneTextFace(painted)
                };
            }
            _readings = System.Array.AsReadOnly(Rows.Select(row => new VisualValueReading(row.ObservationId, row.Value, row.Minimum, row.Maximum)).ToArray());
        }

        internal double[] Values() => Rows.Select(row => row.Value).ToArray();
        internal string Display(int index, double value) => Rows[index].FixedDisplay ?? _format!.Format(value);
        internal bool SamePolicy(ProgressPlan other) => CanTransition && other.CanTransition && _seriesKey == other._seriesKey
            && _formatKey.Equals(other._formatKey) && Rows.Length == other.Rows.Length
            && Rows.Select((row, index) => row.ObservationId == other.Rows[index].ObservationId
                && row.Label == other.Rows[index].Label && row.Minimum == other.Rows[index].Minimum
                && row.Maximum == other.Rows[index].Maximum && row.ExplicitDomain == other.Rows[index].ExplicitDomain
                && row.FixedDisplay == other.Rows[index].FixedDisplay
                && VisualValuePolicy.SameText(row.MeasureFace, row.MeasureStyle, other.Rows[index].MeasureFace, other.Rows[index].MeasureStyle)
                && VisualValuePolicy.SameText(row.PaintFace, row.PaintStyle, other.Rows[index].PaintFace, other.Rows[index].PaintStyle)).All(value => value)
            && VisualValuePolicy.SameText(_tickMeasureFace, _tickMeasureStyle, other._tickMeasureFace, other._tickMeasureStyle)
            && VisualValuePolicy.SameText(_tickPaintFace, _tickPaintStyle, other._tickPaintFace, other._tickPaintStyle);


        // This is the incumbent Progress allocation/painter. Frames supply only detached row
        // values; range normalization, track/handle geometry and complete text stay here.
        internal void Paint(VisualSceneBuilder builder, double[]? readings = null, bool captured = false) {
            var values = readings == null ? Rows.Select(row => row.Display).ToArray()
                : readings.Select((value, index) => Display(index, value)).ToArray();
            var gap = _context.Theme.Spacing; var rowHeight = ChartRowDensity.Pitch(_plot.Height, Rows.Length); // design rule 10
            double Measure(string text, TextStyle style, VisualSceneTextFace face) => captured
                ? face.Prepare(text, style.EffectiveFontSize).Metrics.Width : builder.MeasureText(text, style).Width;
            var labelWidth = Math.Min(_plot.Width * .3, Rows.Max(row => Measure(row.Label, _tickMeasureStyle, _tickMeasureFace)) + gap);
            var valueWidth = _showValues ? Math.Min(_plot.Width * .25, values.Select((value, index) =>
                Measure(value, Rows[index].MeasureStyle, Rows[index].MeasureFace)).Max() + gap) : 0;
            var thickness = Math.Min(_context.Theme.Typography.DataLabelSize * 2, rowHeight * _thicknessRatio);
            var handleStroke = Math.Min(Math.Max(1, thickness * .18), rowHeight * .2);
            var handleRadius = _showHandles ? Math.Max(0, Math.Min(thickness * .62, rowHeight / 2 - handleStroke / 2)) : 0;
            var handleClearance = handleRadius + (_showHandles ? handleStroke / 2 : 0);
            var left = _plot.Left + labelWidth + handleClearance;
            var width = Math.Max(0, _plot.Width - labelWidth - valueWidth - handleClearance * 2);
            if (width <= 0 || thickness <= 0) { builder.AddDiagnostic(new VisualDiagnostic("progress.insufficient-space", "No space remains for progress tracks and handles.")); return; }
            var seriesMetadata = new Dictionary<string, string> {
                ["data-cfx-series"] = "0", ["data-cfx-maximum"] = N(_maximum), ["data-cfx-show-values"] = _showValues ? "true" : "false",
                ["data-cfx-show-handles"] = _showHandles ? "true" : "false", ["data-cfx-bar-thickness-ratio"] = N(_thicknessRatio),
                ["data-cfx-track-opacity"] = N(_trackOpacity), ["data-cfx-series-key"] = _seriesKey
            };
            if (captured) seriesMetadata["data-cfx-series-scope"] = _local ? "local" : "shared";
            using (builder.PushGroup(Id(0), "progress-bar-chart", seriesMetadata)) for (var index = 0; index < Rows.Length; index++) {
                var row = Rows[index]; var raw = readings == null ? row.Value : readings[index];
                var ratio = row.Minimum == 0 ? VisualRadialPrimitives.Clamp(raw / row.Maximum)
                    : VisualRadialPrimitives.Clamp(ChartMath.Normalize(raw, row.Minimum, row.Maximum));
                var y = _plot.Top + rowHeight * (index + .5);
                var bounds = new ChartRect(_plot.Left, _plot.Top + rowHeight * index, _plot.Width, rowHeight);
                builder.AddRegion(new VisualSemanticRegion(Id(0, index), "progress-row", bounds, row.Label + ": " + values[index]));
                var metadata = new Dictionary<string, string> {
                    ["data-cfx-series"] = "0", ["data-cfx-point"] = N(index), ["data-cfx-label"] = row.Label,
                    ["data-cfx-value"] = N(raw), ["data-cfx-ratio"] = N(ratio), ["data-cfx-full-label"] = values[index]
                };
                if (row.ExplicitDomain) { metadata["data-cfx-min"] = N(row.Minimum); metadata["data-cfx-max"] = N(row.Maximum); }
                if (captured) {
                    metadata["data-cfx-series-key"] = _seriesKey; metadata["data-cfx-series-scope"] = _local ? "local" : "shared";
                    if (row.ObservationId != null) metadata["data-cfx-observation-id"] = row.ObservationId;
                    if (row.FixedDisplay != null) metadata["data-cfx-authored-label"] = row.FixedDisplay;
                }
                using (builder.PushGroup(Id(0, index), "progress-row", metadata)) {
                    VisualRadialPrimitives.Text(builder, row.Label, new ChartRect(_plot.Left, bounds.Top, Math.Max(0, labelWidth - gap / 2), rowHeight),
                        _tickPaintStyle, "progress-label", Id(0, index) + "-label", capturedFace: captured ? _tickPaintFace : null);
                    var corner = Math.Min(ChartMarkSurface.DataEndRadius, thickness / 2);
                    builder.Rect(new ChartRect(left, y - thickness / 2, width, thickness), ChartColorMath.WithOpacity(_colors.Border, _trackOpacity), radius: corner, role: "progress-track",
                        paint: VisualChartPaint.Fill(SvgPaint.Of(_colors.Border, SvgColorRole.Surface).WithOpacity(ChartColorMath.WithOpacity(_colors.Border, _trackOpacity), _trackOpacity)));
                    builder.Rect(new ChartRect(left, y - thickness / 2, width * ratio, thickness), row.Color,
                        radius: Math.Min(corner, width * ratio / 2), role: "progress-fill", paint: VisualChartPaint.Fill(row.Paint));
                    if (_showHandles) builder.Ellipse(left + width * ratio, y, handleRadius, handleRadius, _colors.Surface, row.Color,
                        handleStroke, "progress-handle", paint: new VisualScenePaintBinding(SvgPaint.Of(_colors.Surface, SvgColorRole.Surface), row.Paint));
                    if (_showValues) VisualRadialPrimitives.Text(builder, values[index],
                        new ChartRect(left + width + handleClearance, bounds.Top, valueWidth, rowHeight), row.PaintStyle,
                        "progress-value", Id(0, index) + "-value", capturedFace: captured ? row.PaintFace : null);
                }
            }
        }
    }
}
