using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Rendering;

internal static partial class VisualRadialCompiler {
    /// <summary>Optional complete capture. Unsupported value animation never disables ordinary native rendering.</summary>
    internal static IVisualValuePlan? TryCaptureValues(Chart chart, VisualRenderContext context, VisualSceneTextCapture text, VisualScene scene) {
        var raw = context.CaptureNativeValueSource;
        if (chart.Series.Count == 0 || chart.Series.Any(series => series.Id.IsAutomatic) || !raw && !text.SupportsCapturedReplay) return null;
        foreach (var series in chart.Series) {
            var count = series.Points.Count;
            if (!ChartSeriesKindTraits.IsPieLikeKind(series.Kind) || raw && series.ObservationIds.Keys.Any(index => index < 0 || index >= count) || !raw && (count == 0 || count > chart.Options.MaximumPieSlices)
                || series.Points.Any(point => !ChartMath.IsFinite(point.Y) || (raw ? point.Y < 0 : point.Y <= 0))
                || Enumerable.Range(0, count).Any(index => !series.ObservationIds.ContainsKey(index) || !raw && series.SliceRadiusRatio(index).GetValueOrDefault(1) <= 0)) return null;
        }
        var marks = scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "radial-point").ToArray();
        if (!raw && (marks.Length != chart.Series.Sum(series => series.Points.Count) || marks.Any(mark => !mark.Metadata.ContainsKey("data-cfx-observation-id")))) return null;
        for (var index = 0; index < chart.Series.Count; index++) {
            var key = chart.Series[index].Id.Value;
            if (!raw && marks.Count(mark => mark.Metadata.TryGetValue("data-cfx-series-key", out var actual) && actual == key) != chart.Series[index].Points.Count) return null;
        }
        try {
            var source = new ChartVisibilitySource(chart, context);
            var order = CaptureSliceOrder(chart, context);
            var observed = raw && !text.IsReplay ? text.CaptureNativeRequests(builder => ObserveRawRadialValueText(chart, context, builder), context.Layout.Size, context.Font) : text;
            var expanded = observed.TryCaptureValueRepertoire(chart.Options.ValueFormat, RadialValueTextRepertoire(chart, context, observed));
            if (!expanded.SupportsCapturedReplay) return null;
            var plan = new RadialValuePlan(source, context.WithNativeTextCapture(null), expanded.Freeze(), order, raw);
            if (!ReferenceEquals(expanded, text)) { if (raw) text.AdoptNativeRequests(expanded); else text.AdoptValueRepertoire(expanded); }
            return plan;
        } catch (NotSupportedException) { return null; }
        catch (ArgumentException) { return null; } // Optional source capture budgets/policies never change ordinary rendering.
        catch (InvalidOperationException) { return null; }
    }

    private static int[][] CaptureSliceOrder(Chart chart, VisualRenderContext context) => Enumerable.Range(0, chart.Series.Count)
        .Select(index => GetSlices(chart, context.Theme.Resolve(context.ThemeMode), index).Select(slice => slice.SourcePointIndices[0]).ToArray()).ToArray();
    private static bool SameSliceOrder(int[][] first, int[][] second) => first.Length == second.Length
        && !first.Where((row, index) => !row.SequenceEqual(second[index])).Any();

    /// <summary>Owns copied source facts and captured face requests. Each observation uses the incumbent native frame/legend/slice/label owners.</summary>
    private sealed class RadialValuePlan : IVisualRawValuePlan {
        private readonly ChartVisibilitySource _source;
        private readonly VisualRenderContext _context;
        private readonly VisualSceneTextCapture _text;
        private readonly int[][] _order;
        private readonly string _policy, _fonts;
        private string? _sourceKey;
        private readonly IReadOnlyList<VisualValueReading> _readings;
        internal RadialValuePlan(ChartVisibilitySource source, VisualRenderContext context, VisualSceneTextCapture text, int[][] order, bool raw = false) {
            _source = source; _context = context; _text = text; _order = order.Select(row => (int[])row.Clone()).ToArray(); CapturesRawSource = raw;
            _policy = VisualArtifactInterchangeJson.SerializeVisibilitySource(source, context, includeValues: false, includeDerivedMembership: !raw); _fonts = text.CanonicalFacts(includeRuns: false);
            _readings = Array.AsReadOnly(source.SeriesRows.SelectMany(row => row.Points.Select((point, index) =>
                new VisualValueReading(row.ObservationIds[index].Value, point.Y, 0, double.MaxValue,
                    source.SeriesRows.Count == 1 ? null : row.Id.Value))).ToArray());
        }
        public string Kind => CapturesRawSource ? "radial-raw-value" : "radial-value";
        public bool CapturesRawSource { get; }
        public string StructureKey => _source.ValueStructureKey(_context, _text);
        public string SourceKey(PreparedVisual complete) {
            // One plan may be used by both the bare source and several detached appearance views.
            // Cache only its original full-source identity; bind each actual ordered wrapper afterward.
            if (complete.TryRemoveRootEffects(out var bare, out _))
                return ChartVisibilitySource.AppearanceSourceKey(SourceKey(bare), complete);
            var key = _sourceKey; if (key != null) return key;
            key = _source.RawValueSourceKey(_context, _text, complete);
            return System.Threading.Interlocked.CompareExchange(ref _sourceKey, key, null) ?? key;
        }
        public IReadOnlyList<VisualRawValueSlice> CaptureSlices(PreparedVisual drawing, ChartVisibilityView? view) => CaptureRawRadialSlices(_source.CreateNativeView(view), _context, drawing);
        public string SeriesKey => _source.SeriesRows[0].Id.Value;
        public VisualSize Size => _context.Layout.Size;
        public FontSpec Font => _context.Font;
        public IReadOnlyList<VisualValueReading> Readings => _readings;
        public bool AllowsOutOfDomainReadings => false;
        public bool SamePolicy(IVisualValuePlan other) {
            if (other is not RadialValuePlan radial || _policy != radial._policy || CapturesRawSource != radial.CapturesRawSource || !CapturesRawSource && !SameSliceOrder(_order, radial._order) || _readings.Count != radial._readings.Count) return false;
            if (_fonts != radial._fonts && (_text.Requests.Any(request => request.Clusters.Decisions.Count > 0) || radial._text.Requests.Any(request => request.Clusters.Decisions.Count > 0)))
                throw new NotSupportedException("Native radial value frames require the same actual captured fallback cluster catalog and owned faces; changing fallback repertoires need a separately captured admission.");
            return _fonts == radial._fonts;
        }
        public void Paint(VisualSceneBuilder builder, double[]? readings = null, bool captured = false) =>
            throw new InvalidOperationException("A complete radial value frame uses its native compilation owner.");
        public PreparedVisual Frame(PreparedVisual basis, double[] values) {
            if (values.Length != _readings.Count || values.Any(value => !ChartMath.IsFinite(value) || (CapturesRawSource ? value < 0 : value <= 0)))
                throw new NotSupportedException("Pie/Donut complete value frames currently require positive unaggregated authored observations; zero/negative/aggregate transitions need their own admission.");
            var native = _source.CreateNativeValueView((double[])values.Clone());
            var order = CaptureSliceOrder(native, _context);
            if (!CapturesRawSource && !SameSliceOrder(_order, order)) throw new NotSupportedException("Pie/Donut native value frames must retain their captured native slice order and authored observation structure.");
            // Replay binds every measured/ascent/paint request to its captured face and finite actual fallback decisions, rejecting any missing request or cluster.
            return native.Prepare(_context.WithNativeTextCapture(_text.ReplayCapturedClusters())).WithCapturedExportPolicy(basis);
        }
    }
}
