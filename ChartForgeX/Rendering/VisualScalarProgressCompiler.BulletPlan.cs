using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualScalarProgressCompiler {
    /// <summary>Retains ordered typed bullet rows, domains, transforms and native paint/text policy for both existing owners.</summary>
    private sealed class BulletPlan : IVisualValuePlan {
        internal readonly VisualRenderContext Context;
        internal readonly ChartRect Plot;
        internal readonly VisualThemeColors Colors;
        internal readonly BulletRow[] Rows;
        internal readonly ChartAxis XAxis;
        internal readonly bool Independent, ShowAxes;
        internal readonly ChartOrientation Orientation;
        internal readonly string LabelTarget;
        private ChartValueFormat? _ordinaryFormat;
        private readonly ChartValueFormat? _format, _axisFormat;
        private readonly (string Notation, string Format, string Culture)? _formatKey, _axisFormatKey;
        private readonly bool _hasAxisFormat;
        private readonly IReadOnlyList<VisualValueReading> _readings;
        internal BulletPlan(Chart chart, VisualRenderContext context, ChartRect plot, VisualThemeColors colors) {
            Context = context; Plot = plot; Colors = colors; XAxis = chart.Options.XAxis.Clone(); Independent = chart.Options.Bullet.IndependentAxes;
            Orientation = chart.Options.Bullet.Orientation; ShowAxes = chart.Options.ShowAxes; LabelTarget = chart.Options.Labels.Target;
            _ordinaryFormat = chart.Options.ValueFormat; _format = _ordinaryFormat.CapturedValuePolicy;
            _formatKey = FormatKey(_format); _hasAxisFormat = XAxis.ValueFormat != null; _axisFormat = XAxis.ValueFormat?.CapturedValuePolicy;
            _axisFormatKey = FormatKey(_axisFormat);
            Rows = chart.Series.Select((series, index) => (series, index)).Where(entry => entry.series.HasSourceData)
                .Select(entry => new BulletRow(chart, entry.series, entry.index, context, colors)).ToArray();
            _readings = Array.AsReadOnly(Rows.Select(row => new VisualValueReading(row.ObservationId, row.Reading.Value,
                row.Reading.Minimum, row.Reading.Maximum, row.SeriesKey)).ToArray());
        }
        string IVisualValuePlan.Kind => "bullet-value";
        string IVisualValuePlan.SeriesKey => Rows[0].SeriesKey;
        VisualSize IVisualValuePlan.Size => Context.Layout.Size;
        FontSpec IVisualValuePlan.Font => Context.Font;
        IReadOnlyList<VisualValueReading> IVisualValuePlan.Readings => _readings;
        bool IVisualValuePlan.AllowsOutOfDomainReadings => true;
        bool IVisualValuePlan.SamePolicy(IVisualValuePlan other) => other is BulletPlan bullet && Rows.Length == bullet.Rows.Length
            && _formatKey.HasValue && _formatKey.Equals(bullet._formatKey) && _hasAxisFormat == bullet._hasAxisFormat
            && (!_hasAxisFormat || _axisFormatKey.HasValue && _axisFormatKey.Equals(bullet._axisFormatKey))
            && Independent == bullet.Independent && Orientation == bullet.Orientation && ShowAxes == bullet.ShowAxes
            && LabelTarget == bullet.LabelTarget && Plot.Equals(bullet.Plot) && SameAxis(XAxis, bullet.XAxis)
            && Rows.Select((row, index) => row.SamePolicy(bullet.Rows[index])).All(same => same)
            && VisualValuePolicy.SameNativeTheme(Context, bullet.Context);
        void IVisualValuePlan.Paint(VisualSceneBuilder builder, double[]? readings, bool captured) {
            var rows = readings == null ? Rows : Rows.Select((row, index) => row.WithValue(readings[index])).ToArray();
            var paint = new BulletPaintContext(this, rows, readings == null ? _ordinaryFormat ?? _format! : _format!, captured);
            try { PaintBullets(paint, Context, builder, Plot, Colors); }
            finally {
                // Ordinary callbacks have completed. A stored capture never retains a caller-owned closure.
                if (readings == null) { _ordinaryFormat = null; XAxis.ValueFormat = _axisFormat; foreach (var row in Rows) row.ReleaseOrdinaryFormatter(); }
            }
        }
    }
    private sealed class BulletRow {
        internal readonly int Index;
        internal readonly string Name, SeriesKey;
        internal readonly string? ObservationId, AuthoredLabel;
        internal readonly bool Local, ShowLabels;
        internal readonly ChartSeriesState StateRole;
        internal readonly ChartColor Color;
        internal readonly SvgPaint Paint;
        internal readonly ChartBulletValue Reading;
        internal readonly ChartAxis Axis;
        internal readonly VisualValueTextPolicy TextPolicy;
        private readonly ChartValueFormat? _axisFormat;
        private readonly (string Notation, string Format, string Culture)? _axisFormatKey;
        private readonly bool _hasAxisFormat;
        internal BulletRow(Chart chart, ChartSeries series, int index, VisualRenderContext context, VisualThemeColors colors) {
            Range(series, out _, out _); Index = index; Name = series.Name; SeriesKey = series.Id.Value; Local = series.Id.IsAutomatic;
            ObservationId = series.ObservationIds.TryGetValue(0, out var id) ? id.Value : null;
            AuthoredLabel = series.PointLabels.Count > 0 ? series.PointLabels[0] : null; Reading = series.BulletValues[0];
            ShowLabels = series.ResolveDataLabels(chart.Options, defaultVisible: true); StateRole = series.StateRole;
            Color = BulletColor(series, index, colors); Paint = BulletPaint(series, Color); TextPolicy = new VisualValueTextPolicy(chart, series, context);
            Axis = series.CaptureBulletAxis(); _hasAxisFormat = Axis.ValueFormat != null; _axisFormat = Axis.ValueFormat?.CapturedValuePolicy; _axisFormatKey = FormatKey(_axisFormat);
        }
        private BulletRow(BulletRow source, double value) {
            Index = source.Index; Name = source.Name; SeriesKey = source.SeriesKey; Local = source.Local; ObservationId = source.ObservationId;
            AuthoredLabel = source.AuthoredLabel; Reading = new ChartBulletValue(value, source.Reading.Target, source.Reading.Minimum, source.Reading.Maximum, source.Reading.RangeEnds);
            ShowLabels = source.ShowLabels; StateRole = source.StateRole; Color = source.Color; Paint = source.Paint; TextPolicy = source.TextPolicy;
            Axis = source.Axis; _hasAxisFormat = source._hasAxisFormat; _axisFormat = source._axisFormat; _axisFormatKey = source._axisFormatKey;
        }
        internal BulletRow WithValue(double value) => new(this, value);
        internal void ReleaseOrdinaryFormatter() => Axis.ValueFormat = _axisFormat;
        internal bool SamePolicy(BulletRow other) => !Local && !other.Local && ObservationId != null && ObservationId == other.ObservationId
            && SeriesKey == other.SeriesKey && Index == other.Index && Name == other.Name && AuthoredLabel == other.AuthoredLabel
            && Reading.Minimum == other.Reading.Minimum && Reading.Maximum == other.Reading.Maximum && Reading.Target == other.Reading.Target
            && Reading.RangeEnds.SequenceEqual(other.Reading.RangeEnds) && StateRole == other.StateRole && ShowLabels == other.ShowLabels
            && Color.Equals(other.Color) && Paint.Value == other.Paint.Value && Paint.IsRaw == other.Paint.IsRaw
            && _hasAxisFormat == other._hasAxisFormat && (!_hasAxisFormat || _axisFormatKey.HasValue && _axisFormatKey.Equals(other._axisFormatKey))
            && SameAxis(Axis, other.Axis) && TextPolicy.SamePolicy(other.TextPolicy);
    }
    /// <summary>Passes only detached row values and captured text to the existing legacy/configured native bullet methods.</summary>
    private sealed class BulletPaintContext {
        private readonly BulletPlan _plan;
        internal BulletPaintContext(BulletPlan plan, BulletRow[] rows, ChartValueFormat format, bool captured) { _plan = plan; Rows = rows; Format = format; Captured = captured; }
        internal BulletRow[] Rows { get; }
        internal ChartValueFormat Format { get; }
        internal bool Captured { get; }
        internal ChartAxis XAxis => _plan.XAxis;
        internal bool Independent => _plan.Independent;
        internal ChartOrientation Orientation => _plan.Orientation;
        internal bool ShowAxes => _plan.ShowAxes;
        internal string LabelTarget => _plan.LabelTarget;
        internal Func<double, string>? ValueFormatter => ReferenceEquals(Format, ChartValueFormat.ExistingValue) ? null : Format.Callback;
        internal TextMetrics Measure(VisualSceneBuilder builder, string text, TextStyle style) {
            if (!Captured) return builder.MeasureText(text, style);
            foreach (var row in Rows) if (row.TextPolicy.TryFace(style, out var face)) return face!.Prepare(text, style.EffectiveFontSize).Metrics;
            throw new NotSupportedException("The native bullet drawing requested an uncaptured face.");
        }
        internal void CaptureIdentity(Dictionary<string, string> metadata, BulletRow row) {
            if (!Captured) return;
            metadata["data-cfx-series-key"] = row.SeriesKey; metadata["data-cfx-series-scope"] = row.Local ? "local" : "shared";
            if (row.ObservationId != null) metadata["data-cfx-observation-id"] = row.ObservationId;
        }
    }
    private static (string Notation, string Format, string Culture)? FormatKey(ChartValueFormat? format) {
        if (format == null) return null;
        try { return format.PortablePolicy(); } catch (NotSupportedException) { return null; } catch (ArgumentException) { return null; }
    }
    private static bool SameAxis(ChartAxis a, ChartAxis b) => a.Minimum == b.Minimum && a.Maximum == b.Maximum && a.Scale == b.Scale
        && a.LogarithmBase == b.LogarithmBase && a.LogarithmicDomain == b.LogarithmicDomain && a.SymmetricLogarithmThreshold == b.SymmetricLogarithmThreshold
        && a.TickCount == b.TickCount && a.IntegerTicks == b.IntegerTicks && a.Visible == b.Visible && a.Reversed == b.Reversed && a.ShowLine == b.ShowLine
        && a.Labels.Select(label => (label.Value, label.Text)).SequenceEqual(b.Labels.Select(label => (label.Value, label.Text)));
}
