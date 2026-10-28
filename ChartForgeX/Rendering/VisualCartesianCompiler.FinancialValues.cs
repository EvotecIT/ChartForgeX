using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    /// <summary>Captures real OHLC tuples and both authored direction policies through the incumbent native financial owner.</summary>
    internal static IVisualValuePlan? TryCaptureFinancialValues(Chart chart, VisualRenderContext context,
        VisualSceneTextCapture text, VisualScene scene, ChartCartesianProjection? axes) {
        if (!context.CaptureNativeValueSource || axes == null || ChartCartesianOrientation.IsHorizontal(chart) || !text.SupportsCapturedReplay
            || chart.Series.Count == 0 || chart.Options.ShowPointLegend || chart.Options.ValueFormatter != null
            || chart.Series.Any(series => series.Kind is not (ChartSeriesKind.Candlestick or ChartSeriesKind.Ohlc) || series.Id.IsAutomatic || series.FinancialValues.Count == 0
                || series.ObservationIds.Count != series.FinancialValues.Count || Enumerable.Range(0, series.FinancialValues.Count).Any(index => !series.ObservationIds.ContainsKey(index)))) return null;
        if (!ComponentValueAxesSupported(chart)) return null;
        try {
            var source = ChartVisibilitySource.CaptureNativeComponentValues(chart, context); var expanded = text;
            if (!text.IsReplay) {
                expanded = text.CaptureNativeRequests(builder => ObserveFinancialValueText(chart, context, builder), context.Layout.Size, context.Font);
                expanded = expanded.TryCaptureValueRepertoire(chart.Options.ValueFormat, Array.Empty<(string Owner, string Text)>());
            }
            if (!expanded.SupportsCapturedReplay) return null;
            var points = scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").ToArray();
            if (points.Length != source.SeriesRows.Sum(row => row.FinancialValues.Count)) return null;
            var identities = new HashSet<(string Series, string Observation)>();
            foreach (var point in points) if (!point.Metadata.TryGetValue("data-cfx-series-key", out var series)
                || !point.Metadata.TryGetValue("data-cfx-observation-id", out var id) || !identities.Add((series, id))) return null;
            foreach (var row in source.SeriesRows) foreach (var pair in row.ObservationIds)
                if (!identities.Contains((row.Id.Value, pair.Value.Value))) return null;
            var plan = new ComponentValuePlan(source, context.WithNativeTextCapture(null), expanded.Freeze(), axes, "financial-components", null);
            if (!text.IsReplay) text.AdoptNativeRequests(expanded); return plan;
        } catch (NotSupportedException) { return null; }
        catch (ArgumentException) { return null; }
        catch (InvalidOperationException) { return null; }
    }
    private static void ObserveFinancialValueText(Chart chart, VisualRenderContext context, VisualSceneBuilder builder) {
        var colors = context.Theme.Resolve(context.ThemeMode);
        foreach (var series in chart.Series) for (var index = 0; index < series.FinancialValues.Count; index++) {
            var item = series.FinancialValues[index]; var label = ResolveObservationLabel(chart, context, series, index, colors, format => format("close", item.Close));
            builder.MeasureText(label.Text, label.Style); builder.MeasureText("Mg", label.Style); builder.TextAscent(label.Style);
        }
    }
}
