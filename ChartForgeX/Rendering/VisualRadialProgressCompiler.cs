using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Compiles concentric progress rings and independently scaled radial layers.</summary>
internal static class VisualRadialProgressCompiler {
    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors) {
        var series = chart.Series[0];
        if (series.Kind == ChartSeriesKind.LayeredRadial && series.RadialLayers.Any(layer => layer == null))
            throw new InvalidOperationException("Radial layers must not contain null.");
        if (!series.ShowInLegend) return Array.Empty<VisualLegendEntry>();
        return series.Kind == ChartSeriesKind.LayeredRadial
            ? series.RadialLayers.Select((layer, index) => new VisualLegendEntry(layer.Name, layer.Color ?? VisualRadialPrimitives.Color(series, index, colors), Id(index), series.Kind,
                stateRole: series.StateRole, seriesKey: series.InteractionIdentityKey,
                paint: VisualChartPaint.Series(series, layer.Color ?? VisualRadialPrimitives.Color(series, index, colors), index, layer.Color.HasValue))).ToArray()
            : series.Points.Select((point, index) => new VisualLegendEntry(VisualRadialPrimitives.Label(chart, point, index), VisualRadialPrimitives.Color(series, index, colors), Id(index), series.Kind,
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
        if (series.Points.Count == 0) { builder.AddDiagnostic(new VisualDiagnostic("radial.no-data", "Radial bars need at least one value.")); return; }
        if (series.Points.Any(point => double.IsNaN(point.Y) || double.IsInfinity(point.Y) || point.Y < 0 || point.Y > 100))
            throw new InvalidOperationException("Radial bar values must be finite and between zero and 100.");
        var outer = Math.Min(maximum, maximum * .85 * chart.Options.RadialBarRadiusScale);
        if (outer < 1) { builder.AddDiagnostic(new VisualDiagnostic("radial.insufficient-space", "The viewport is too small for progress rings.")); return; }
        var average = series.Points.Average(point => point.Y);
        var value = ChartNumericFormatter.FormatValue(chart.Options, average);
        var style = VisualRadialPrimitives.Style(chart, context, colors.Foreground, context.Theme.Typography.TitleSize, 700);
        var captionStyle = VisualRadialPrimitives.Style(chart, context, colors.MutedForeground, context.Theme.Typography.DataLabelSize);
        var requested = series.ShowDataLabels != false && chart.Options.ShowRadialBarCenterLabel
            ? Math.Max(Math.Max(builder.MeasureText(value, style).Width, builder.MeasureText(series.Name, captionStyle).Width) / 2,
                (builder.MeasureText(value, style).Height + builder.MeasureText(series.Name, captionStyle).Height) / 2) + context.Theme.Spacing : 0;
        var layout = RadialBarRingLayout.Create(outer, series.Points.Count, chart.Options.RadialBarStrokeScale, requested);
        for (var index = 0; index < series.Points.Count; index++) {
            var point = series.Points[index]; var label = VisualRadialPrimitives.Label(chart, point, index);
            var formatted = ChartNumericFormatter.FormatValue(chart.Options, point.Y);
            var radius = layout.RadiusAt(index); var color = VisualRadialPrimitives.Color(series, index, colors);
            var bounds = new ChartRect(cx - radius - layout.StrokeWidth / 2, cy - radius - layout.StrokeWidth / 2,
                radius * 2 + layout.StrokeWidth, radius * 2 + layout.StrokeWidth);
            builder.AddRegion(new VisualSemanticRegion(Id(index), "radial-bar-ring", bounds, label + ": " + formatted));
            using (builder.PushGroup(Id(index), "radial-bar-point", Metadata(index, label, point.Y, 0, 100))) {
                VisualRadialPrimitives.Arc(builder, cx, cy, radius, layout.StrokeWidth, -Math.PI / 2, Math.PI * 2, colors.Border, "radial-bar-track", paint: SvgPaint.Of(colors.Border, SvgColorRole.Surface));
                VisualRadialPrimitives.Arc(builder, cx, cy, radius, layout.StrokeWidth, -Math.PI / 2, Math.PI * 2 * point.Y / 100,
                    color, "radial-bar-ring", round: true, paint: VisualChartPaint.Series(series, color, index));
            }
        }
        builder.Ellipse(cx, cy, layout.CenterRadius, layout.CenterRadius, colors.Surface, colors.Border, role: "radial-bar-center",
            paint: new VisualScenePaintBinding(SvgPaint.Of(colors.Surface, SvgColorRole.Surface), SvgPaint.Of(colors.Border, SvgColorRole.Surface)));
        if (series.ShowDataLabels != false && chart.Options.ShowRadialBarCenterLabel)
            Center(builder, cx, cy, layout.CenterRadius, value, series.Name,
                CenterStyle(chart, context, colors.Foreground, context.Theme.Typography.TitleSize, layout.CenterRadius, 700),
                CenterStyle(chart, context, colors.MutedForeground, context.Theme.Typography.DataLabelSize, layout.CenterRadius), "radial-bar");
    }

    private static void Layers(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, double cx, double cy, double maximum, VisualThemeColors colors) {
        var series = chart.Series[0];
        if (series.RadialLayers.Count == 0) { builder.AddDiagnostic(new VisualDiagnostic("radial.no-data", "Layered radial charts need at least one layer.")); return; }
        if (series.RadialLayers.Any(layer => layer == null || layer.Maximum <= layer.Minimum || double.IsInfinity(layer.Maximum - layer.Minimum)))
            throw new InvalidOperationException("Radial layers require non-null layers and finite positive ranges.");
        var extent = series.RadialLayers.Max(layer => layer.RadiusRatio + layer.StrokeRatio * chart.Options.RadialBarStrokeScale / 2);
        var separatorClearance = series.RadialLayers.Max(layer => layer.SeparatorCount > 0 ? layer.SeparatorStrokeWidth / 2 : 0);
        var outer = Math.Min(maximum * .85 * chart.Options.RadialBarRadiusScale, Math.Max(0, maximum - separatorClearance) / extent);
        if (outer <= 0) { builder.AddDiagnostic(new VisualDiagnostic("radial.insufficient-space", "Layer separators exceed the available viewport.")); return; }
        var centerRadius = maximum;
        string? centerValue = null;
        for (var index = 0; index < series.RadialLayers.Count; index++) {
            var layer = series.RadialLayers[index];
            var radius = outer * layer.RadiusRatio; var stroke = outer * layer.StrokeRatio * chart.Options.RadialBarStrokeScale;
            centerRadius = Math.Min(centerRadius, Math.Max(0, radius - stroke / 2));
            var start = (layer.StartAngleDegrees % 360) * Math.PI / 180; var sweep = layer.SweepAngleDegrees * Math.PI / 180 * VisualRadialPrimitives.Clamp((layer.Value - layer.Minimum) / (layer.Maximum - layer.Minimum));
            var color = ChartColorMath.WithOpacity(layer.Color ?? VisualRadialPrimitives.Color(series, index, colors), layer.Opacity);
            var bounds = new ChartRect(cx - radius - stroke / 2, cy - radius - stroke / 2, radius * 2 + stroke, radius * 2 + stroke);
            centerValue = ChartNumericFormatter.FormatValue(chart.Options, layer.Value);
            builder.AddRegion(new VisualSemanticRegion(Id(index), "layered-radial-layer", bounds, layer.Name + ": " + centerValue));
            using (builder.PushGroup(Id(index), "layered-radial-point", Metadata(index, layer.Name, layer.Value, layer.Minimum, layer.Maximum))) {
                VisualRadialPrimitives.Arc(builder, cx, cy, radius, stroke, start, sweep, color, "layered-radial-layer", round: layer.LineCap == ChartRadialLayerCap.Round,
                    paint: VisualChartPaint.Series(series, layer.Color ?? VisualRadialPrimitives.Color(series, index, colors), index, layer.Color.HasValue).WithOpacity(color, layer.Opacity));
                if (sweep <= 0 || layer.SeparatorCount <= 0) continue;
                var inset = Math.Min(Math.Max(0, stroke / 2 - .5), stroke * layer.SeparatorInsetRatio);
                var inner = Math.Max(0, radius - stroke / 2 + inset); var outside = radius + stroke / 2 - inset;
                for (var separator = 1; separator <= layer.SeparatorCount; separator++) {
                    var angle = start + sweep * separator / (layer.SeparatorCount + 1);
                    builder.Line(cx + Math.Cos(angle) * inner, cy + Math.Sin(angle) * inner,
                        cx + Math.Cos(angle) * outside, cy + Math.Sin(angle) * outside,
                        layer.SeparatorColor ?? colors.Surface, layer.SeparatorStrokeWidth, "layered-radial-separator", paint: VisualChartPaint.Stroke(layer.SeparatorColor ?? colors.Surface, SvgColorRole.Surface));
                }
            }
        }
        if (series.ShowDataLabels != false) {
            Center(builder, cx, cy, centerRadius, centerValue!, series.Name,
                CenterStyle(chart, context, colors.Foreground, context.Theme.Typography.TitleSize, centerRadius, 700),
                CenterStyle(chart, context, colors.MutedForeground, context.Theme.Typography.DataLabelSize, centerRadius), "layered-radial");
        }
    }

    private static ChartForgeX.Typography.TextStyle CenterStyle(Chart chart, VisualRenderContext context, ChartColor color, double size, double radius, int weight = 400) =>
        VisualRadialPrimitives.Style(chart, context, color, Math.Min(size, Math.Max(.1, radius * .7 / 1.3)), weight);

    private static void Center(VisualSceneBuilder builder, double cx, double cy, double radius, string value, string caption,
        ChartForgeX.Typography.TextStyle valueStyle, ChartForgeX.Typography.TextStyle captionStyle, string role) {
        var height = Math.Max(0, radius * .7); var width = Math.Max(0, radius * 1.6);
        VisualRadialPrimitives.Text(builder, value, new ChartRect(cx - width / 2, cy - height, width, height), valueStyle, role + "-value", "series-0-center-value");
        VisualRadialPrimitives.Text(builder, caption, new ChartRect(cx - width / 2, cy, width, height), captionStyle, role + "-title", "series-0-center-caption");
    }

    private static string Id(int index) => "series-0-point-" + index.ToString(CultureInfo.InvariantCulture);
    private static IReadOnlyDictionary<string, string> Metadata(int index, string label, double value, double min, double max) => new Dictionary<string, string> {
        ["data-cfx-point"] = index.ToString(CultureInfo.InvariantCulture), ["data-cfx-label"] = label, ["data-cfx-value"] = value.ToString("R", CultureInfo.InvariantCulture),
        ["data-cfx-min"] = min.ToString("R", CultureInfo.InvariantCulture), ["data-cfx-max"] = max.ToString("R", CultureInfo.InvariantCulture)
    };
}
