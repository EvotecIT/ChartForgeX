using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    /// <summary>Captures real typed Y/Size components; optional admission never changes ordinary Bubble rendering.</summary>
    internal static IVisualValuePlan? TryCaptureBubbleValues(Chart chart, VisualRenderContext context,
        VisualSceneTextCapture text, VisualScene scene, ChartCartesianProjection? axes, ChartBubbleSizeScale sizes) {
        if (!context.CaptureNativeValueSource || axes == null || ChartCartesianOrientation.IsHorizontal(chart)
            || !text.SupportsCapturedReplay || chart.Series.Count == 0 || chart.Options.ShowPointLegend
            || !chart.Options.Bubble.MinimumValue.HasValue || !chart.Options.Bubble.MaximumValue.HasValue
            || chart.Options.ValueFormatter != null || chart.Series.Any(series => series.Kind != ChartSeriesKind.Bubble
                || series.Id.IsAutomatic || series.Bubbles.Count == 0 || series.PointStrokeColors.Any(color => color.HasValue)
                || series.ObservationIds.Count != series.Bubbles.Count
                || Enumerable.Range(0, series.Bubbles.Count).Any(index => !series.ObservationIds.ContainsKey(index)))) return null;
        if (!ComponentValueAxesSupported(chart)) return null;
        try {
            var source = ChartVisibilitySource.CaptureNativeBubbleValues(chart, context);
            var expanded = text;
            if (!text.IsReplay) {
                expanded = text.CaptureNativeRequests(builder => ObserveBubbleValueText(chart, context, builder, sizes), context.Layout.Size, context.Font);
                expanded = expanded.TryCaptureValueRepertoire(chart.Options.ValueFormat, Array.Empty<(string Owner, string Text)>());
                if (chart.Options.Bubble.SizeDimension.ValueFormat is { } format) {
                    _ = format.PortablePolicy();
                    expanded = expanded.TryCaptureValueRepertoire(format, Array.Empty<(string Owner, string Text)>());
                }
            }
            if (!expanded.SupportsCapturedReplay) return null;
            var points = scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").ToArray();
            if (points.Length != source.SeriesRows.Sum(row => row.Bubbles.Count)) return null;
            var identities = new HashSet<(string Series, string Observation)>();
            foreach (var point in points) if (!point.Metadata.TryGetValue("data-cfx-series-key", out var series)
                || !point.Metadata.TryGetValue("data-cfx-observation-id", out var id) || !identities.Add((series, id))) return null;
            foreach (var row in source.SeriesRows) foreach (var pair in row.ObservationIds)
                if (!identities.Contains((row.Id.Value, pair.Value.Value))) return null;
            var plan = new BubbleValuePlan(source, context.WithNativeTextCapture(null), expanded.Freeze(), axes, sizes);
            if (!text.IsReplay) text.AdoptNativeRequests(expanded);
            return plan;
        } catch (NotSupportedException) { return null; }
        catch (ArgumentException) { return null; }
        catch (InvalidOperationException) { return null; }
    }

    private static void ObserveBubbleValueText(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartBubbleSizeScale sizes) {
        var colors = context.Theme.Resolve(context.ThemeMode);
        foreach (var series in chart.Series) for (var index = 0; index < series.Bubbles.Count; index++) {
            var size = series.Bubbles[index].Size;
            var label = ResolveObservationLabel(chart, context, series, index, colors, _ => sizes.Presentation.Value(size));
            builder.MeasureText(label.Text, label.Style); builder.MeasureText("Mg", label.Style); builder.TextAscent(label.Style);
        }
    }
}
