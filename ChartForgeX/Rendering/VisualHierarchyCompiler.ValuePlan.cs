using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Rendering;

internal static partial class VisualHierarchyCompiler {
    /// <summary>Captures fixed authored hierarchy structure; ordinary rendering remains available outside this finite admission.</summary>
    internal static IVisualValuePlan? TryCaptureValues(Chart chart, VisualRenderContext context, VisualSceneTextCapture text, VisualScene scene) {
        if (chart.Series.Count != 1 || chart.Series[0].Id.IsAutomatic) return null;
        var series = chart.Series[0];
        if (series.Kind is not (ChartSeriesKind.Treemap or ChartSeriesKind.Sunburst) || !series.HierarchyItems.Any(item => item.Value.HasValue)
            || chart.NativeHierarchyVisibility?.HiddenTargets.Count > 0) return null;
        var nodes = scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Metadata.ContainsKey("data-cfx-node")).ToArray();
        if (nodes.Length != series.HierarchyItems.Count || nodes.Select(node => node.Metadata["data-cfx-node"]).Distinct(StringComparer.Ordinal).Count() != nodes.Length) return null;
        try {
            var source = new ChartVisibilitySource(chart, context);
            // A zero/unpainted node can become positive. Observe its actual native caption policy now, before caller/registry state can escape.
            var observed = text.CaptureNativeRequests(builder => ObserveHierarchyValueText(chart, context, builder), context.Layout.Size, context.Font);
            var expanded = observed.TryCaptureValueRepertoire(chart.Options.ValueFormat, HierarchyValueTextRepertoire(chart, observed));
            var plan = new HierarchyValuePlan(source, context.WithNativeTextCapture(null), expanded.Freeze());
            text.AdoptNativeRequests(expanded); return plan;
        } catch (NotSupportedException) { return null; }
        catch (ArgumentException) { return null; }
        catch (InvalidOperationException) { return null; }
    }
    private static void ObserveHierarchyValueText(Chart chart, VisualRenderContext context, VisualSceneBuilder builder) {
        var series = chart.Series[0]; var colors = context.Theme.Resolve(context.ThemeMode);
        var surface = new ChartColorPresentationSet(chart).HierarchySurface(colors);
        for (var index = 0; index < series.HierarchyItems.Count; index++) {
            var item = series.HierarchyItems[index]; var style = LabelStyle(chart, context, surface.Blend(index).Color, index);
            var name = HierarchyCaption(series, index, item.Label);
            builder.MeasureText(name, style); builder.MeasureText("Mg", style); builder.TextAscent(style);
            if (series.Kind == ChartSeriesKind.Treemap && chart.Options.Treemap.LabelTrimming == TextTrimming.Ellipsis)
                _ = builder.LayoutText(name, 1, style, TextWrapMode.NoWrap, 1, TextTrimming.Ellipsis);
            if (series.Kind == ChartSeriesKind.Treemap && chart.Options.Treemap.ShowValues) {
                var value = ChartNumericFormatter.FormatValue(chart.Options, chart.HierarchyPreparation!.Values[index]);
                builder.MeasureText(value, style); builder.MeasureText(TreemapHeaderCaption(name, value), style);
            }
        }
    }
    private static IEnumerable<(string Owner, string Text)> HierarchyValueTextRepertoire(Chart chart, VisualSceneTextCapture capture) {
        if (!capture.HasContextCaseRequests || chart.Series[0].Kind != ChartSeriesKind.Treemap || !chart.Options.Treemap.ShowValues
            || !chart.Options.ValueFormat.TryGetNativeTextRepertoire(out _, out var values)) yield break;
        var series = chart.Series[0];
        for (var index = 0; index < series.HierarchyItems.Count; index++) foreach (var value in values)
            yield return ("native-treemap-header-context", TreemapHeaderCaption(HierarchyCaption(series, index, series.HierarchyItems[index].Label), value));
    }

    private sealed class HierarchyValuePlan : IVisualCompleteValuePlan {
        private readonly ChartVisibilitySource _source;
        private readonly VisualRenderContext _context;
        private readonly VisualSceneTextCapture _text;
        private readonly string _policy, _fonts;
        private readonly IReadOnlyList<VisualValueReading> _readings;
        internal HierarchyValuePlan(ChartVisibilitySource source, VisualRenderContext context, VisualSceneTextCapture text) {
            _source = source; _context = context; _text = text;
            _policy = VisualArtifactInterchangeJson.SerializeVisibilitySource(source, context, includeValues: false);
            _fonts = text.CanonicalFacts(includeRuns: false);
            _readings = Array.AsReadOnly(source.SeriesRows[0].HierarchyItems.Where(item => item.Value.HasValue)
                .Select(item => new VisualValueReading(item.Id, item.Value!.Value, 0, double.MaxValue)).ToArray());
        }
        public string Kind => "hierarchy-value";
        public string SeriesKey => _source.SeriesRows[0].Id.Value;
        public VisualSize Size => _context.Layout.Size;
        public FontSpec Font => _context.Font;
        public IReadOnlyList<VisualValueReading> Readings => _readings;
        public bool AllowsOutOfDomainReadings => false;
        public bool SamePolicy(IVisualValuePlan other) {
            if (other is not HierarchyValuePlan hierarchy || _policy != hierarchy._policy || _readings.Count != hierarchy._readings.Count) return false;
            if (_fonts != hierarchy._fonts) throw new NotSupportedException("Hierarchy frames require the same captured native face requests and bounded fallback decisions, including omitted captions.");
            return true;
        }
        public void Paint(VisualSceneBuilder builder, double[]? readings = null, bool captured = false) =>
            throw new InvalidOperationException("Complete hierarchy frames use the native hierarchy compiler.");
        public PreparedVisual Frame(PreparedVisual basis, double[] values) {
            if (values.Length != _readings.Count || values.Any(value => !ChartMath.IsFinite(value) || value < 0))
                throw new NotSupportedException("Hierarchy readings retain ordered finite nonnegative authored sizes.");
            // Full-source parent totals and visible geometry are validated by the same native relationship/layout owners on every frame.
            return _source.CreateNativeValueView((double[])values.Clone()).Prepare(_context.WithNativeTextCapture(_text.ReplayCapturedClusters()))
                .WithCapturedExportPolicy(basis);
        }
    }
}
