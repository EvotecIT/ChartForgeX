using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    /// <summary>Captures only real nullable uncertainty members and reconstructs through native typed guards.</summary>
    internal static IVisualValuePlan? TryCaptureErrorValues(Chart chart, VisualRenderContext context,
        VisualSceneTextCapture text, VisualScene scene, ChartCartesianProjection? axes) {
        if (!context.CaptureNativeValueSource || axes == null || ChartCartesianOrientation.IsHorizontal(chart) || !text.SupportsCapturedReplay
            || chart.Series.Count == 0 || chart.Options.ShowPointLegend || chart.Options.ValueFormatter != null
            || chart.Series.Any(series => series.Kind != ChartSeriesKind.ErrorBar || series.Id.IsAutomatic || series.ErrorBars.Count == 0
                || series.ObservationIds.Count != series.ErrorBars.Count || Enumerable.Range(0, series.ErrorBars.Count).Any(index => !series.ObservationIds.ContainsKey(index)))) return null;
        if (!ComponentValueAxesSupported(chart)) return null;
        try {
            var source = ChartVisibilitySource.CaptureNativeComponentValues(chart, context); var expanded = text;
            if (!text.IsReplay) {
                expanded = text.CaptureNativeRequests(builder => ObserveErrorValueText(chart, context, builder), context.Layout.Size, context.Font);
                expanded = expanded.TryCaptureValueRepertoire(chart.Options.ValueFormat, Array.Empty<(string Owner, string Text)>());
            }
            if (!expanded.SupportsCapturedReplay) return null;
            var points = scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").ToArray();
            if (points.Length != source.SeriesRows.Sum(row => row.ErrorBars.Count)) return null;
            var identities = new HashSet<(string Series, string Observation)>();
            foreach (var point in points) if (!point.Metadata.TryGetValue("data-cfx-series-key", out var series)
                || !point.Metadata.TryGetValue("data-cfx-observation-id", out var id) || !identities.Add((series, id))) return null;
            foreach (var row in source.SeriesRows) foreach (var pair in row.ObservationIds)
                if (!identities.Contains((row.Id.Value, pair.Value.Value))) return null;
            var plan = new ComponentValuePlan(source, context.WithNativeTextCapture(null), expanded.Freeze(), axes, "error-components", null);
            if (!text.IsReplay) text.AdoptNativeRequests(expanded); return plan;
        } catch (NotSupportedException) { return null; }
        catch (ArgumentException) { return null; }
        catch (InvalidOperationException) { return null; }
    }
    private static void ObserveErrorValueText(Chart chart, VisualRenderContext context, VisualSceneBuilder builder) {
        var colors = context.Theme.Resolve(context.ThemeMode);
        foreach (var series in chart.Series) for (var index = 0; index < series.ErrorBars.Count; index++) {
            var error = series.ErrorBars[index]; var label = ResolveObservationLabel(chart, context, series, index, colors, format => format("value", error.Y));
            builder.MeasureText(label.Text, label.Style); builder.MeasureText("Mg", label.Style); builder.TextAscent(label.Style);
        }
    }
}
