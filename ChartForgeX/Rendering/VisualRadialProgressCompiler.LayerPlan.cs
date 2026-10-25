using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualRadialProgressCompiler {
    /// <summary>Captures the existing typed layer painter's range, shape, paint, identity and center policy.</summary>
    internal sealed class LayerPlan : IVisualValuePlan {
        private sealed class Layer {
            internal string Name = string.Empty, Display = string.Empty;
            internal string? ObservationId, AuthoredLabel;
            internal double Value, Minimum, Maximum, Radius, Stroke, Start, Sweep, SeparatorStroke, SeparatorInset;
            internal int Separators;
            internal bool Round, PinStateColors;
            internal ChartColor Color, SeparatorColor;
            internal SvgPaint Paint;
        }
        private readonly VisualRenderContext _context;
        private readonly Layer[] _layers;
        private readonly string _seriesKey, _caption;
        private readonly bool _local, _showCenter;
        private readonly ChartSeriesState _stateRole;
        private readonly double _cx, _cy, _centerRadius;
        private readonly ChartValueFormat? _format;
        private readonly (string Notation, string Format, string Culture)? _formatKey;
        private readonly TextStyle _valueStyle, _captionStyle;
        private readonly VisualSceneTextFace _valueFace, _captionFace;
        private readonly IReadOnlyList<VisualValueReading> _readings;
        private bool CanTransition => !_local && _formatKey.HasValue && _layers.All(layer => layer.ObservationId != null)
            && _valueStyle.Baseline == TextBaseline.Normal && _captionStyle.Baseline == TextBaseline.Normal;

        internal LayerPlan(Chart chart, VisualRenderContext context, double cx, double cy, double maximum, double outer, VisualThemeColors colors) {
            var series = chart.Series[0]; _context = context; _cx = cx; _cy = cy;
            _seriesKey = series.Id.Value; _local = series.Id.IsAutomatic; _caption = series.Name; _stateRole = series.StateRole;
            _showCenter = series.ResolveDataLabels(chart.Options, defaultVisible: true);
            _layers = new Layer[series.RadialLayers.Count]; var centerRadius = maximum;
            for (var index = 0; index < _layers.Length; index++) {
                var layer = series.RadialLayers[index];
                var radius = outer * layer.RadiusRatio; var stroke = outer * layer.StrokeRatio * chart.Options.RadialProgressStrokeScale;
                centerRadius = Math.Min(centerRadius, Math.Max(0, radius - stroke / 2));
                var color = ChartColorMath.WithOpacity(layer.Color ?? VisualRadialPrimitives.Color(series, index, colors), layer.Opacity);
                var display = ChartNumericFormatter.FormatValue(chart.Options, layer.Value);
                _layers[index] = new Layer {
                    Name = layer.Name, Display = display, Value = layer.Value, Minimum = layer.Minimum, Maximum = layer.Maximum,
                    Radius = radius, Stroke = stroke, Start = (layer.StartAngleDegrees % 360) * Math.PI / 180,
                    Sweep = layer.SweepAngleDegrees * Math.PI / 180, Round = layer.LineCap == ChartRadialLayerCap.Round,
                    Color = color, Paint = VisualChartPaint.Series(series, layer.Color ?? VisualRadialPrimitives.Color(series, index, colors), index, layer.Color.HasValue).WithOpacity(color, layer.Opacity),
                    Separators = layer.SeparatorCount, SeparatorColor = layer.SeparatorColor ?? colors.Surface,
                    SeparatorStroke = layer.SeparatorStrokeWidth, SeparatorInset = layer.SeparatorInsetRatio,
                    PinStateColors = chart.Options.PinStateColorsInForcedColors && series.StateRole != ChartSeriesState.None,
                    ObservationId = series.ObservationIds.TryGetValue(index, out var id) ? id.Value : null,
                    AuthoredLabel = index < series.PointLabels.Count ? series.PointLabels[index] : null
                };
            }
            _centerRadius = centerRadius; _format = chart.Options.ValueFormat.CapturedValuePolicy;
            if (_format != null) {
                try { _formatKey = _format.PortablePolicy(); }
                catch (NotSupportedException) { _formatKey = null; }
                catch (ArgumentException) { _formatKey = null; }
            }
            _valueStyle = CenterStyle(chart, context, colors.Foreground, context.Theme.Typography.CenterValueSize, centerRadius, 700);
            _captionStyle = CenterStyle(chart, context, colors.MutedForeground, context.Theme.Typography.DataLabelSize, centerRadius);
            _valueStyle.Alignment = _captionStyle.Alignment = TextAlignment.Center;
            _valueFace = new VisualSceneTextFace(_valueStyle); _captionFace = new VisualSceneTextFace(_captionStyle);
            _readings = Array.AsReadOnly(_layers.Select(layer => new VisualValueReading(layer.ObservationId, layer.Value, layer.Minimum, layer.Maximum)).ToArray());
        }
        string IVisualValuePlan.Kind => "layered-radial-value";
        string IVisualValuePlan.SeriesKey => _seriesKey;
        VisualSize IVisualValuePlan.Size => _context.Layout.Size;
        FontSpec IVisualValuePlan.Font => _context.Font;
        IReadOnlyList<VisualValueReading> IVisualValuePlan.Readings => _readings;
        bool IVisualValuePlan.AllowsOutOfDomainReadings => true;
        bool IVisualValuePlan.SamePolicy(IVisualValuePlan other) => other is LayerPlan layers && CanTransition && layers.CanTransition
            && _seriesKey == layers._seriesKey && _stateRole == layers._stateRole && _caption == layers._caption
            && _showCenter == layers._showCenter && _cx == layers._cx && _cy == layers._cy && _centerRadius == layers._centerRadius
            && _formatKey.Equals(layers._formatKey) && _layers.Length == layers._layers.Length
            && _layers.Select((layer, index) => SameLayerPolicy(layer, layers._layers[index])).All(same => same)
            && VisualValuePolicy.SameText(_valueFace, _valueStyle, layers._valueFace, layers._valueStyle)
            && VisualValuePolicy.SameText(_captionFace, _captionStyle, layers._captionFace, layers._captionStyle);
        private static bool SameLayerPolicy(Layer a, Layer b) => a.Name == b.Name && a.ObservationId == b.ObservationId
            && a.AuthoredLabel == b.AuthoredLabel && a.Minimum == b.Minimum && a.Maximum == b.Maximum
            && a.Radius == b.Radius && a.Stroke == b.Stroke && a.Start == b.Start && a.Sweep == b.Sweep && a.Round == b.Round
            && a.Separators == b.Separators && a.SeparatorStroke == b.SeparatorStroke && a.SeparatorInset == b.SeparatorInset
            && a.Color.Equals(b.Color) && a.SeparatorColor.Equals(b.SeparatorColor) && a.Paint.Value == b.Paint.Value && a.Paint.IsRaw == b.Paint.IsRaw
            && a.PinStateColors == b.PinStateColors;

        // Ordinary output and every captured intermediate use this one native painter.
        // The only animation input is a replacement raw reading; source clamp and all
        // geometry, separators, caps, label fitting and native text stay in this owner.
        void IVisualValuePlan.Paint(VisualSceneBuilder builder, double[]? readings, bool captured) {
            var centerValue = string.Empty;
            for (var index = 0; index < _layers.Length; index++) {
                var layer = _layers[index]; var raw = readings == null ? layer.Value : readings[index];
                var sweep = layer.Sweep * VisualRadialPrimitives.Clamp((raw - layer.Minimum) / (layer.Maximum - layer.Minimum));
                centerValue = readings == null ? layer.Display : _format!.Format(raw);
                var bounds = new ChartRect(_cx - layer.Radius - layer.Stroke / 2, _cy - layer.Radius - layer.Stroke / 2,
                    layer.Radius * 2 + layer.Stroke, layer.Radius * 2 + layer.Stroke);
                builder.AddRegion(new VisualSemanticRegion(Id(index), "layered-radial-layer", bounds, layer.Name + ": " + centerValue));
                var metadata = new Dictionary<string, string> {
                    ["data-cfx-series"] = "0", ["data-cfx-source-point"] = index.ToString(CultureInfo.InvariantCulture), ["data-cfx-source-count"] = "1",
                    ["data-cfx-pin-state-colors"] = layer.PinStateColors ? "true" : "false", ["data-cfx-point"] = index.ToString(CultureInfo.InvariantCulture),
                    ["data-cfx-label"] = layer.Name, ["data-cfx-value"] = raw.ToString("R", CultureInfo.InvariantCulture),
                    ["data-cfx-min"] = layer.Minimum.ToString("R", CultureInfo.InvariantCulture), ["data-cfx-max"] = layer.Maximum.ToString("R", CultureInfo.InvariantCulture)
                };
                if (captured) {
                    metadata["data-cfx-series-key"] = _seriesKey; metadata["data-cfx-series-scope"] = _local ? "local" : "shared";
                    if (layer.ObservationId != null) metadata["data-cfx-observation-id"] = layer.ObservationId;
                    if (layer.AuthoredLabel != null) metadata["data-cfx-authored-label"] = layer.AuthoredLabel;
                }
                using (builder.PushGroup(Id(index), "layered-radial-point", metadata)) {
                    VisualRadialPrimitives.Arc(builder, _cx, _cy, layer.Radius, layer.Stroke, layer.Start, sweep, layer.Color,
                        "layered-radial-layer", round: layer.Round, paint: layer.Paint);
                    if (sweep <= 0 || layer.Separators <= 0) continue;
                    var inset = Math.Min(Math.Max(0, layer.Stroke / 2 - .5), layer.Stroke * layer.SeparatorInset);
                    var inner = Math.Max(0, layer.Radius - layer.Stroke / 2 + inset); var outside = layer.Radius + layer.Stroke / 2 - inset;
                    for (var separator = 1; separator <= layer.Separators; separator++) {
                        var angle = layer.Start + sweep * separator / (layer.Separators + 1);
                        builder.Line(_cx + Math.Cos(angle) * inner, _cy + Math.Sin(angle) * inner,
                            _cx + Math.Cos(angle) * outside, _cy + Math.Sin(angle) * outside, layer.SeparatorColor,
                            layer.SeparatorStroke, "layered-radial-separator", paint: VisualChartPaint.Stroke(layer.SeparatorColor, SvgColorRole.Surface));
                    }
                }
            }
            if (_showCenter) Center(builder, _context, _cx, _cy, _centerRadius, centerValue, _caption,
                _valueStyle, _captionStyle, "layered-radial", captured ? _valueFace : null, captured ? _captionFace : null);
        }
    }
}
