using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualGaugeCompiler {
    /// <summary>Captures gauge source policy, all anatomy options, numeric formatting and native faces without retaining a Chart.</summary>
    private sealed class GaugePlan : IVisualValuePlan {
        internal readonly VisualRenderContext Context;
        internal readonly ChartRect Plot;
        internal readonly ChartGaugeOptions Options;
        internal readonly string SeriesKey, Name, Caption, LabelTarget;
        internal readonly string? ObservationId, AuthoredValue;
        internal readonly bool Local, ShowValue, ShowAxes, PinStateColors;
        internal readonly VisualValueTextPolicy TextPolicy;
        private readonly ChartColor? _pointColor, _seriesColor;
        private readonly ChartSeriesState _state;
        private readonly GaugeData _data;
        private readonly ChartValueFormat? _format;
        private readonly (string Notation, string Format, string Culture)? _formatKey;
        private readonly GaugePresentation _ordinary;
        private readonly IReadOnlyList<VisualValueReading> _readings;
        internal GaugePlan(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
            var series = chart.Series[0]; Context = context; Plot = plot; Options = chart.Options.Gauge.Clone();
            SeriesKey = series.Id.Value; Local = series.Id.IsAutomatic; Name = series.Name; Caption = Options.Caption ?? Name;
            ObservationId = series.ObservationIds.TryGetValue(0, out var id) ? id.Value : null;
            AuthoredValue = series.PointLabels.Count > 0 ? series.PointLabels[0] : null;
            LabelTarget = chart.Options.Labels.Target; PinStateColors = chart.Options.PinStateColorsInForcedColors;
            ShowValue = series.ResolveDataLabels(chart.Options, defaultVisible: true); ShowAxes = chart.Options.ShowAxes;
            _pointColor = series.PointColors.Count > 0 ? series.PointColors[0] : null; _seriesColor = series.Color; _state = series.StateRole;
            _data = Read(chart, context.Theme.Resolve(context.ThemeMode)); TextPolicy = new VisualValueTextPolicy(chart, series, context);
            _format = chart.Options.ValueFormat.CapturedValuePolicy;
            if (_format != null) {
                try { _formatKey = _format.PortablePolicy(); } catch (NotSupportedException) { } catch (ArgumentException) { }
            }
            _ordinary = Presentation(new GaugePaintContext(this, false), context, builder, plot, _data, chart.Options.ValueFormat);
            _readings = Array.AsReadOnly(new[] { new VisualValueReading(ObservationId, _data.Raw, _data.Min, _data.Max) });
        }
        string IVisualValuePlan.Kind => "gauge-value";
        string IVisualValuePlan.SeriesKey => SeriesKey;
        VisualSize IVisualValuePlan.Size => Context.Layout.Size;
        FontSpec IVisualValuePlan.Font => Context.Font;
        IReadOnlyList<VisualValueReading> IVisualValuePlan.Readings => _readings;
        bool IVisualValuePlan.AllowsOutOfDomainReadings => true;
        bool IVisualValuePlan.SamePolicy(IVisualValuePlan other) => other is GaugePlan gauge && !Local && !gauge.Local
            && ObservationId != null && ObservationId == gauge.ObservationId && SeriesKey == gauge.SeriesKey && Name == gauge.Name
            && Caption == gauge.Caption && AuthoredValue == gauge.AuthoredValue && LabelTarget == gauge.LabelTarget
            && ShowValue == gauge.ShowValue && ShowAxes == gauge.ShowAxes && PinStateColors == gauge.PinStateColors
            && _state == gauge._state && Nullable.Equals(_pointColor, gauge._pointColor) && Nullable.Equals(_seriesColor, gauge._seriesColor)
            && _data.Min == gauge._data.Min && _data.Max == gauge._data.Max && Plot.Equals(gauge.Plot)
            && _data.Targets.Select(target => (target.Value, target.Label)).SequenceEqual(gauge._data.Targets.Select(target => (target.Value, target.Label)))
            && SameOptions(Options, gauge.Options) && _formatKey.HasValue && _formatKey.Equals(gauge._formatKey)
            && TextPolicy.SamePolicy(gauge.TextPolicy) && VisualValuePolicy.SameNativeTheme(Context, gauge.Context);
        private static bool SameOptions(ChartGaugeOptions a, ChartGaugeOptions b) => a.Form == b.Form && a.Orientation == b.Orientation
            && a.StartAngleDegrees == b.StartAngleDegrees && a.SweepAngleDegrees == b.SweepAngleDegrees && a.TickCount == b.TickCount
            && a.BandGap == b.BandGap && Nullable.Equals(a.NeedleColor, b.NeedleColor) && a.Caption == b.Caption
            && a.TickLabels.Select(label => (label.Value, label.Text)).SequenceEqual(b.TickLabels.Select(label => (label.Value, label.Text)))
            && a.Bands.Select(band => (band.Minimum, band.Maximum, band.State, band.Color, band.Opacity))
                .SequenceEqual(b.Bands.Select(band => (band.Minimum, band.Maximum, band.State, band.Color, band.Opacity)));
        internal GaugeData Data(double raw) => ReadData(_data.Min, _data.Max, raw, _state, _pointColor, _seriesColor,
            _data.Bands, _data.Targets, Context.Theme.Resolve(Context.ThemeMode));
        internal SvgPaint ValuePaint(GaugeData data) => GaugePaint(_seriesColor.HasValue || _pointColor.HasValue, _state, data);
        void IVisualValuePlan.Paint(VisualSceneBuilder builder, double[]? readings, bool captured) {
            var chart = new GaugePaintContext(this, captured); var data = readings == null ? _data : Data(readings[0]);
            var presentation = readings == null ? _ordinary : Presentation(chart, Context, builder, Plot, data, _format!);
            Paint(chart, Context, builder, Plot, data, presentation);
        }
    }

    /// <summary>Passes detached policy and captured-face mode to the incumbent native gauge painter.</summary>
    private sealed class GaugePaintContext {
        private readonly GaugePlan _plan;
        internal GaugePaintContext(GaugePlan plan, bool captured) { _plan = plan; Captured = captured; }
        internal bool Captured { get; }
        internal ChartGaugeOptions Options => _plan.Options;
        internal bool ShowValue => _plan.ShowValue;
        internal bool ShowAxes => _plan.ShowAxes;
        internal bool Local => _plan.Local;
        internal bool PinStateColors => _plan.PinStateColors;
        internal string Name => _plan.Name;
        internal string Caption => _plan.Caption;
        internal string SeriesKey => _plan.SeriesKey;
        internal string LabelTarget => _plan.LabelTarget;
        internal string? ObservationId => _plan.ObservationId;
        internal string? AuthoredValue => _plan.AuthoredValue;
        internal TextStyle Style(ChartColor color, double size, int weight = 400, int point = -1, bool ticks = false) => _plan.TextPolicy.Style(color, size, weight, point, ticks);
        internal TextMetrics Measure(VisualSceneBuilder builder, string text, TextStyle style) => _plan.TextPolicy.Measure(builder, text, style, Captured);
        internal double Ascent(VisualSceneBuilder builder, TextStyle style) => Captured ? _plan.TextPolicy.Face(style).Ascent(style.EffectiveFontSize) : builder.TextAscent(style);
        internal double AscentEnvelope(VisualSceneBuilder builder, TextStyle style) => Captured ? _plan.TextPolicy.Face(style).AscentEnvelope(style.EffectiveFontSize) : builder.TextAscentEnvelope(style);
        internal SvgPaint ValuePaint(GaugeData data) => _plan.ValuePaint(data);
        internal void Text(VisualSceneBuilder builder, string text, ChartRect area, TextStyle style, string role, string id,
            SvgPaint? paint = null, TextAlignment alignment = TextAlignment.Center, LabelFallbackRule fallback = LabelFallbackRule.EllipsisThenDrop) =>
            _plan.TextPolicy.Text(builder, text, area, style, role, id, Captured, paint, alignment, fallback);
    }
    private sealed class GaugePresentation {
        internal string Value = string.Empty;
        internal double[] ScaleValues = Array.Empty<double>();
        internal string[] ScaleCaptions = Array.Empty<string>(), TargetCaptions = Array.Empty<string>();
        internal Dictionary<double, string> Captions = new();
    }
    private static GaugePresentation Presentation(GaugePaintContext chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect plot, GaugeData data, ChartValueFormat format) {
        var options = chart.Options; var authoredValue = chart.AuthoredValue; var showValue = chart.ShowValue;
        var scaleValues = ScaleValues(chart, data);
        var retainedValues = new List<double>(data.Targets.Select(target => target.Value));
        if (authoredValue == null && showValue) retainedValues.Add(data.Raw);
        var requestedValues = scaleValues.Where(number => ExplicitScaleLabel(options, number) == null).Concat(retainedValues);
        // One caption per requested numeric fact keeps a shared value/target/tick consistent and resolves callbacks once.
        var distinctValues = requestedValues.Distinct().ToArray();
        var formatted = distinctValues.Length == 0 ? Array.Empty<string>() : ChartNumericFormatter.FormatScaleValues(format, distinctValues);
        var captions = new Dictionary<double, string>();
        for (var index = 0; index < distinctValues.Length; index++) captions[distinctValues[index]] = formatted[index];
        var scaleCaptions = scaleValues.Select(number => ExplicitScaleLabel(options, number) ?? captions[number]).ToArray();
        FitGeneratedScale(chart, context, builder, plot, data, format, retainedValues, captions, ref scaleValues, ref scaleCaptions);
        var value = authoredValue ?? (showValue ? captions[data.Raw] : N(data.Raw));
        var targetCaptions = data.Targets.Select(target => target.Label ?? captions[target.Value]).ToArray();
        return new GaugePresentation { Value = value, ScaleValues = scaleValues, ScaleCaptions = scaleCaptions, TargetCaptions = targetCaptions, Captions = captions };
    }
}
