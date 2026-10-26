using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    /// <summary>Uses native typed interval, paired-boundary and label owners; the public source remains detached.</summary>
    internal static IVisualValuePlan? TryCaptureRangeValues(Chart chart, VisualRenderContext context,
        VisualSceneTextCapture text, VisualScene scene, ChartCartesianProjection? axes) {
        if (!context.CaptureNativeValueSource || axes == null || ChartCartesianOrientation.IsHorizontal(chart) || !text.SupportsCapturedReplay
            || chart.Series.Count == 0 || chart.Options.ShowPointLegend || chart.Options.ValueFormatter != null
            || chart.Series.Any(series => series.Kind is not (ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea)
                || series.Id.IsAutomatic || series.Ranges.Count == 0 || series.ObservationIds.Count != series.Ranges.Count
                || Enumerable.Range(0, series.Ranges.Count).Any(index => !series.ObservationIds.ContainsKey(index)))) return null;
        if (!ComponentValueAxesSupported(chart)) return null;
        try {
            var source = ChartVisibilitySource.CaptureNativeComponentValues(chart, context); var expanded = text;
            if (!text.IsReplay) {
                expanded = text.CaptureNativeRequests(builder => ObserveRangeValueText(chart, context, builder), context.Layout.Size, context.Font);
                expanded = expanded.TryCaptureValueRepertoire(chart.Options.ValueFormat, RangeValueTextRepertoire(chart, expanded));
            }
            if (!expanded.SupportsCapturedReplay) return null;
            var points = scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").ToArray();
            if (points.Length != source.SeriesRows.Sum(row => row.Ranges.Count)) return null;
            var identities = new HashSet<(string Series, string Observation)>();
            foreach (var point in points) if (!point.Metadata.TryGetValue("data-cfx-series-key", out var series)
                || !point.Metadata.TryGetValue("data-cfx-observation-id", out var id) || !identities.Add((series, id))) return null;
            foreach (var row in source.SeriesRows) foreach (var pair in row.ObservationIds)
                if (!identities.Contains((row.Id.Value, pair.Value.Value))) return null;
            var plan = new ComponentValuePlan(source, context.WithNativeTextCapture(null), expanded.Freeze(), axes, "range-components", null);
            if (!text.IsReplay) text.AdoptNativeRequests(expanded); return plan;
        } catch (NotSupportedException) { return null; }
        catch (ArgumentException) { return null; }
        catch (InvalidOperationException) { return null; }
    }
    private static string RangeValueLabel(Func<string, double, string> format, double lower, double upper) => format("lower", lower) + "–" + format("upper", upper);
    private static void ObserveRangeValueText(Chart chart, VisualRenderContext context, VisualSceneBuilder builder) {
        var colors = context.Theme.Resolve(context.ThemeMode);
        foreach (var series in chart.Series) for (var index = 0; index < series.Ranges.Count; index++) {
            var range = series.Ranges[index]; var label = ResolveObservationLabel(chart, context, series, index, colors, format => RangeValueLabel(format, range.Lower, range.Upper));
            builder.MeasureText(label.Text, label.Style); builder.MeasureText("Mg", label.Style); builder.TextAscent(label.Style);
        }
    }
    private static IEnumerable<(string Owner, string Text)> RangeValueTextRepertoire(Chart chart, VisualSceneTextCapture capture) {
        if (!capture.HasContextCaseRequests || !chart.Options.ValueFormat.TryGetNativeTextRepertoire(out _, out var values)) yield break;
        // Actual native composition, bounded lazily by the existing cluster/scalar observation owner.
        foreach (var lower in values) foreach (var upper in values)
            yield return ("native-range-label-context", RangeValueLabel((member, _) => member == "lower" ? lower : upper, 0, 0));
    }
}
