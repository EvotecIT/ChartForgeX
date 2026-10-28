using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    /// <summary>Each frame reconstructs the copied typed tuples and invokes the incumbent native size/path/label compiler.</summary>
    private class ComponentValuePlan : IVisualComponentValuePlan {
        private readonly ChartVisibilitySource _source;
        private readonly VisualRenderContext _context;
        private readonly VisualSceneTextCapture _text;
        private readonly ChartCartesianProjection _axes;
        private readonly string _policy, _fonts;
        private readonly IReadOnlyList<VisualValueReading> _readings;
        private readonly IReadOnlyList<ChartAxis?> _memberAxes;
        private string? _sourceKey;
        internal ComponentValuePlan(ChartVisibilitySource source, VisualRenderContext context, VisualSceneTextCapture text,
            ChartCartesianProjection axes, string sourceFamily, VisualComponentSizePolicy? sizes) {
            _source = source; _context = context; _text = text; _axes = axes; SourceFamily = sourceFamily; SizePolicy = sizes;
            _policy = VisualArtifactInterchangeJson.SerializeVisibilitySource(source, context, includeValues: false);
            _fonts = text.CanonicalFacts(includeRuns: false);
            _readings = Array.AsReadOnly(source.SeriesRows.SelectMany(row => ReadingsFor(row, axes, sizes)).ToArray());
            var policyAxes = source.CopyPresentation();
            var bySeries = source.SeriesRows.ToDictionary(row => row.Id.Value, row => row.ValueAxis == ChartAxisSide.Secondary ? policyAxes.SecondaryYAxis : policyAxes.YAxis);
            _memberAxes = Array.AsReadOnly(_readings.Select(row => row.Component == "size" ? null : row.Component?.StartsWith("x-", StringComparison.Ordinal) == true ? policyAxes.XAxis : bySeries[row.SeriesKey!]).ToArray());
            if (_readings.Where((row, index) => !ChartMath.IsFinite(row.Value) || !ChartMath.IsFinite(row.Minimum) || !ChartMath.IsFinite(row.Maximum)
                || row.Maximum <= row.Minimum || !IntrinsicValue(index, row.Value)).Any())
                throw new NotSupportedException("Native component source endpoints require finite captured domains and finite raw values, with positive raw Size when present; native tuple and projection guards remain authoritative.");
        }
        public string Kind => "cartesian-component-value";
        public string SourceFamily { get; }
        public bool CapturesRawSource => true;
        public VisualComponentSizePolicy? SizePolicy { get; }
        public string SeriesKey => _source.SeriesRows[0].Id.Value;
        public VisualSize Size => _context.Layout.Size;
        public FontSpec Font => _context.Font;
        public IReadOnlyList<VisualValueReading> Readings => _readings;
        // Display windows do not invalidate raw authored values. The native tuple, projection, clip and exact fixed-axis owners validate every complete frame.
        public bool AllowsOutOfDomainReadings => true;
        public string StructureKey => _source.ValueStructureKey(_context, _text);
        public string SourceKey(PreparedVisual complete) {
            if (complete.TryRemoveRootEffects(out var bare, out _)) return ChartVisibilitySource.AppearanceSourceKey(SourceKey(bare), complete);
            var key = _sourceKey; if (key != null) return key;
            key = _source.RawValueSourceKey(_context, _text, complete);
            return System.Threading.Interlocked.CompareExchange(ref _sourceKey, key, null) ?? key;
        }
        public bool SamePolicy(IVisualValuePlan other) => other is ComponentValuePlan plan && SourceFamily == plan.SourceFamily && _policy == plan._policy && _fonts == plan._fonts
            && VisualSeriesVisibilityCapture.SameAxis(_axes.X, plan._axes.X) && VisualSeriesVisibilityCapture.SameAxis(_axes.Y, plan._axes.Y)
            && VisualSeriesVisibilityCapture.SameAxis(_axes.SecondaryX, plan._axes.SecondaryX) && VisualSeriesVisibilityCapture.SameAxis(_axes.SecondaryY, plan._axes.SecondaryY);
        public void Paint(VisualSceneBuilder builder, double[]? readings = null, bool captured = false) =>
            throw new InvalidOperationException("Complete typed components use their native chart compilation owner.");
        public PreparedVisual Frame(PreparedVisual basis, double[] values) {
            if (values.Length != _readings.Count || values.Where((value, index) => !ChartMath.IsFinite(value)
                || !IntrinsicValue(index, value)).Any())
                throw new NotSupportedException("Typed components require finite raw values and positive raw Size when present; native tuple, projection, clipping and size-clamping constraints remain authoritative.");
            var result = _source.CreateNativeValueView((double[])values.Clone()).Prepare(_context.WithNativeTextCapture(_text.ReplayCapturedClusters()));
            if (!VisualSeriesVisibilityCapture.SameAxis(_axes.X, result.XProjection) || !VisualSeriesVisibilityCapture.SameAxis(_axes.Y, result.YProjection)
                || !VisualSeriesVisibilityCapture.SameAxis(_axes.SecondaryX, result.SecondaryXProjection) || !VisualSeriesVisibilityCapture.SameAxis(_axes.SecondaryY, result.SecondaryYProjection))
                throw new NotSupportedException("Typed component frames retain their exact captured native axes and plot basis; automatic layout changes require a distinct admission.");
            if (result.Scene.ValueCapture?.Plan is not ComponentValuePlan next || next.SourceFamily != SourceFamily)
                throw new NotSupportedException("Every typed component frame must retain its complete captured source/font/identity facts.");
            return result.WithCapturedExportPolicy(basis);
        }
        private bool IntrinsicValue(int index, double value) {
            if (_readings[index].Component == "size") return value > 0;
            var axis = _memberAxes[index]!;
            return axis.Scale != ChartScaleKind.Logarithmic || axis.IsInLogarithmicDomain(value);
        }
        private static IEnumerable<VisualValueReading> ReadingsFor(ChartVisibilitySource.Series row, ChartCartesianProjection axes, VisualComponentSizePolicy? sizes) {
            var axis = row.ValueAxis == ChartAxisSide.Secondary ? axes.SecondaryY! : axes.Y;
            VisualValueReading Reading(int index, string member, double value) => new(row.ObservationIds[index].Value, value, axis.Minimum, axis.Maximum, row.Id.Value, member);
            if (row.Kind == ChartSeriesKind.Bubble) {
                if (sizes == null) throw new InvalidOperationException("Bubble components need the captured native size policy.");
                for (var index = 0; index < row.Bubbles.Count; index++) {
                    var bubble = row.Bubbles[index]; yield return Reading(index, "y", bubble.Y);
                    yield return new VisualValueReading(row.ObservationIds[index].Value, bubble.Size, sizes.Minimum, sizes.Maximum, row.Id.Value, "size");
                }
            } else if (row.Kind is ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea) {
                for (var index = 0; index < row.Ranges.Count; index++) {
                    yield return Reading(index, "lower", row.Ranges[index].Lower); yield return Reading(index, "upper", row.Ranges[index].Upper);
                }
            } else if (row.Kind == ChartSeriesKind.ErrorBar) {
                for (var index = 0; index < row.ErrorBars.Count; index++) {
                    var error = row.ErrorBars[index]; yield return Reading(index, "value", error.Y);
                    if (error.XBounds is { } xBounds) {
                        yield return new VisualValueReading(row.ObservationIds[index].Value, xBounds.Lower, axes.X.Minimum, axes.X.Maximum, row.Id.Value, "x-lower");
                        yield return new VisualValueReading(row.ObservationIds[index].Value, xBounds.Upper, axes.X.Minimum, axes.X.Maximum, row.Id.Value, "x-upper");
                    }
                    if (error.YBounds is { } yBounds) { yield return Reading(index, "y-lower", yBounds.Lower); yield return Reading(index, "y-upper", yBounds.Upper); }
                }
            } else if (row.Kind is ChartSeriesKind.Candlestick or ChartSeriesKind.Ohlc) {
                for (var index = 0; index < row.FinancialValues.Count; index++) {
                    var item = row.FinancialValues[index]; yield return Reading(index, "open", item.Open); yield return Reading(index, "high", item.High);
                    yield return Reading(index, "low", item.Low); yield return Reading(index, "close", item.Close);
                }
            } else throw new NotSupportedException("This native component family has no captured typed reading map.");
        }
    }
}
