using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Compiles concentric progress rings and independently scaled radial layers.</summary>
internal static partial class VisualRadialProgressCompiler {
    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors) {
        var series = chart.Series[0];
        if (series.Kind == ChartSeriesKind.LayeredRadial && series.RadialLayers.Any(layer => layer == null))
            throw new InvalidOperationException("Radial layers must not contain null.");
        if (!series.ShowInLegend) return Array.Empty<VisualLegendEntry>();
        return series.Kind == ChartSeriesKind.LayeredRadial
            ? series.RadialLayers.Select((layer, index) => new VisualLegendEntry(layer.Name, layer.Color ?? VisualRadialPrimitives.Color(series, index, colors), Id(index), series.Kind,
                stateRole: series.StateRole, seriesKey: series.InteractionIdentityKey,
                paint: VisualChartPaint.Series(series, layer.Color ?? VisualRadialPrimitives.Color(series, index, colors), index, layer.Color.HasValue))).ToArray()
            : series.RenderPoints.Select((point, index) => new VisualLegendEntry(VisualRadialPrimitives.Label(chart, point, index), VisualRadialPrimitives.Color(series, index, colors), Id(index), series.Kind,
                stateRole: series.StateRole, seriesKey: series.InteractionIdentityKey,
                paint: VisualChartPaint.Series(series, VisualRadialPrimitives.Color(series, index, colors), index))).ToArray();
    }

    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
        var series = chart.Series[0]; var colors = context.Theme.Resolve(context.ThemeMode);
        var cx = plot.Left + plot.Width / 2; var cy = plot.Top + plot.Height / 2;
        var maximum = Math.Min(plot.Width, plot.Height) / 2 - context.Theme.Spacing;
        if (maximum <= 0) { builder.AddDiagnostic(new VisualDiagnostic("radial.insufficient-space", "The viewport has no space for radial progress marks.")); return; }
        using (builder.PushClip(plot)) {
            if (series.Kind == ChartSeriesKind.LayeredRadial) Layers(chart, context, builder, plot, cx, cy, maximum, colors);
            else Rings(chart, context, builder, plot, cx, cy, maximum, colors);
        }
    }

    private static void Rings(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, double cx, double cy, double maximum, VisualThemeColors colors) {
        var series = chart.Series[0];
        if (series.RenderPoints.Count == 0) { builder.AddDiagnostic(new VisualDiagnostic("radial.no-data", "Progress rings need at least one value.")); return; }
        if (series.RenderPoints.Any(point => double.IsNaN(point.Y) || double.IsInfinity(point.Y) || point.Y < 0 || point.Y > 100))
            throw new InvalidOperationException("Progress-ring values must be finite and between zero and 100.");
        var outer = Math.Min(maximum, maximum * .85 * chart.Options.RadialProgressRadiusScale);
        if (outer < 1) { builder.AddDiagnostic(new VisualDiagnostic("radial.insufficient-space", "The viewport is too small for progress rings.")); return; }
        builder.CaptureValue(new RingPlan(chart, context, cx, cy, outer, colors));
    }

    private static void Layers(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, double cx, double cy, double maximum, VisualThemeColors colors) {
        var series = chart.Series[0];
        if (series.RadialLayers.Count == 0) { builder.AddDiagnostic(new VisualDiagnostic("radial.no-data", "Layered radial charts need at least one layer.")); return; }
        if (series.RadialLayers.Any(layer => layer == null || layer.Maximum <= layer.Minimum || double.IsInfinity(layer.Maximum - layer.Minimum)))
            throw new InvalidOperationException("Radial layers require non-null layers and finite positive ranges.");
        var extent = series.RadialLayers.Max(layer => layer.RadiusRatio + layer.StrokeRatio * chart.Options.RadialProgressStrokeScale / 2);
        var separatorClearance = series.RadialLayers.Max(layer => layer.SeparatorCount > 0 ? layer.SeparatorStrokeWidth / 2 : 0);
        var outer = Math.Min(maximum * .85 * chart.Options.RadialProgressRadiusScale, Math.Max(0, maximum - separatorClearance) / extent);
        if (outer <= 0) { builder.AddDiagnostic(new VisualDiagnostic("radial.insufficient-space", "Layer separators exceed the available viewport.")); return; }
        builder.CaptureValue(new LayerPlan(chart, context, cx, cy, maximum, outer, colors));
    }

    private static string Id(int index) => "series-0-point-" + index.ToString(CultureInfo.InvariantCulture);
}
