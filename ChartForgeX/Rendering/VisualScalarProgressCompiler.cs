using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Compiles scalar circular progress, bullet comparisons and labeled progress rows.</summary>
internal static partial class VisualScalarProgressCompiler {
    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors) => chart.Series
        .Select((series, index) => new { series, index }).Where(item => item.series.ShowInLegend)
        .Select(item => {
            var circle = item.series.Kind == ChartSeriesKind.Circle;
            var bullet = item.series.Kind == ChartSeriesKind.Bullet;
            var color = circle ? CircleColor(item.series, colors) : bullet ? BulletColor(item.series, item.index, colors)
                : ChartSeriesColours.Resolve(item.series, item.index, colors);
            var paint = circle
                ? SvgPaint.Of(color, item.series.Color.HasValue || item.series.PointColors.Count > 0 && item.series.PointColors[0].HasValue ? SvgColorRole.Series : SvgColorRole.Status)
                : bullet ? BulletPaint(item.series, color) : VisualChartPaint.Series(item.series, color);
            return new VisualLegendEntry(item.series.Name, color, Id(item.index), item.series.Kind,
                item.series.FillPattern, item.series.StateRole, item.series.InteractionIdentityKey, paint: paint);
        }).ToArray();

    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
        var kind = chart.Series[0].Kind;
        if (chart.Series.Any(series => series.Kind != kind) || kind != ChartSeriesKind.Bullet && chart.Series.Count != 1)
            throw new InvalidOperationException("Scalar progress charts require one family; only bullet charts support multiple series.");
        if (chart.Series.SelectMany(series => series.Points).Any(point => !Finite(point.X) || !Finite(point.Y)))
            throw new InvalidOperationException("Scalar progress values must be finite.");
        if (plot.Width <= 0 || plot.Height <= 0) { builder.AddDiagnostic(new VisualDiagnostic("progress.insufficient-space", "No viewport remains for scalar progress marks.")); return; }
        var colors = context.Theme.Resolve(context.ThemeMode);
        using (builder.PushClip(plot)) {
            if (kind == ChartSeriesKind.Circle) Circle(chart, context, builder, plot, colors);
            else if (kind == ChartSeriesKind.Bullet) Bullets(chart, context, builder, plot, colors);
            else Progress(chart, context, builder, plot, colors);
        }
    }

    private static void Circle(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, VisualThemeColors colors) {
        var series = chart.Series[0];
        Range(series, out var min, out var max);
        var raw = series.Points[0].Y; var ratio = VisualRadialPrimitives.Clamp((VisualRadialPrimitives.Clamp(raw, min, max) - min) / (max - min));
        var status = ratio < .6 ? "negative" : ratio < .8 ? "warning" : "positive";
        var state = series.StateRole == ChartSeriesState.None ? RatioState(ratio) : series.StateRole;
        var color = CircleColor(series, colors);
        var value = Value(chart, series, 0, raw);
        var statusHeight = series.ShowDataLabels != false && chart.Options.ShowCircleStatusLabel ? Math.Min(plot.Height * .2, context.Theme.Typography.DataLabelSize * 1.5 + context.Theme.Spacing) : 0;
        var radius = Math.Max(0, Math.Min(plot.Width / 2 - context.Theme.Spacing, (plot.Height - statusHeight) / 2 - context.Theme.Spacing));
        var requested = Math.Min(plot.Width, plot.Height) * .28 * chart.Options.CircleRadiusScale;
        var halfStrokeFactor = .08 * chart.Options.CircleStrokeScale;
        radius = Math.Min(requested, radius / (1 + halfStrokeFactor));
        if (radius <= 0) { builder.AddDiagnostic(new VisualDiagnostic("circle.insufficient-space", "No radius remains for the circle chart.")); return; }
        var stroke = radius * .16 * chart.Options.CircleStrokeScale;
        var cx = plot.Left + plot.Width / 2; var cy = plot.Top + (plot.Height - statusHeight) / 2;
        builder.AddRegion(new VisualSemanticRegion(Id(0), "circle", new ChartRect(cx - radius - stroke / 2, cy - radius - stroke / 2, radius * 2 + stroke, radius * 2 + stroke), series.Name + ": " + value));
        using (builder.PushGroup(Id(0), "circle-chart", new Dictionary<string, string> {
            ["data-cfx-value"] = N(raw), ["data-cfx-min"] = N(min), ["data-cfx-max"] = N(max), ["data-cfx-percent"] = N(ratio),
            ["data-cfx-status"] = status, ["data-cfx-state"] = state.ToString(), ["data-cfx-full-label"] = value,
            ["data-cfx-radius-scale"] = N(chart.Options.CircleRadiusScale), ["data-cfx-stroke-scale"] = N(chart.Options.CircleStrokeScale)
        })) {
            VisualRadialPrimitives.Arc(builder, cx, cy, radius, stroke, -Math.PI / 2, Math.PI * 2, colors.Border, "circle-track", paint: SvgPaint.Of(colors.Border, SvgColorRole.Surface));
            var valuePaint = SvgPaint.Of(color, series.Color.HasValue || series.PointColors.Count > 0 && series.PointColors[0].HasValue ? SvgColorRole.Series : SvgColorRole.Status);
            VisualRadialPrimitives.Arc(builder, cx, cy, radius, stroke, -Math.PI / 2, Math.PI * 2 * ratio, color, "circle-value", round: true, paint: valuePaint);
            var inner = Math.Max(0, radius - stroke * .82);
            builder.Ellipse(cx, cy, inner, inner, colors.Surface, colors.Border, role: "circle-center", paint: new VisualScenePaintBinding(SvgPaint.Of(colors.Surface, SvgColorRole.Surface), SvgPaint.Of(colors.Border, SvgColorRole.Surface)));
            if (series.ShowDataLabels != false) {
                Text(chart, context, builder, series, 0, value, new ChartRect(cx - inner * .8, cy - inner * .7, inner * 1.6, inner * .7), "circle-label", Id(0) + "-value", colors.Foreground, context.Theme.Typography.TitleSize, 700);
                Text(chart, context, builder, series, 0, series.Name, new ChartRect(cx - inner * .8, cy, inner * 1.6, inner * .7), "circle-title", Id(0) + "-title", colors.MutedForeground, context.Theme.Typography.DataLabelSize);
                if (chart.Options.ShowCircleStatusLabel) {
                    var y = plot.Bottom - statusHeight;
                    var marker = Math.Min(context.Theme.MarkerRadius, statusHeight / 4);
                    builder.Ellipse(cx - plot.Width * .15, y + statusHeight / 2, marker, marker, ChartSeriesColours.State(state, colors, color), role: "circle-status-marker", paint: VisualChartPaint.Fill(ChartSeriesColours.State(state, colors, color), SvgColorRole.Status));
                    Text(chart, context, builder, series, 0, status, new ChartRect(cx - plot.Width * .1, y, plot.Width * .3, statusHeight), "circle-status-label", Id(0) + "-status", colors.MutedForeground, context.Theme.Typography.DataLabelSize);
                }
            }
        }
    }

    private static ChartColor CircleColor(ChartSeries series, VisualThemeColors colors) {
        Range(series, out var min, out var max);
        var ratio = (VisualRadialPrimitives.Clamp(series.Points[0].Y, min, max) - min) / (max - min);
        var state = series.StateRole == ChartSeriesState.None ? RatioState(ratio) : series.StateRole;
        return series.PointColors.Count > 0 && series.PointColors[0].HasValue ? series.PointColors[0]!.Value : series.Color ?? ChartSeriesColours.State(state, colors, colors.Accent);
    }

    private static ChartSeriesState RatioState(double ratio) => ratio < .6 ? ChartSeriesState.Danger : ratio < .8 ? ChartSeriesState.Warning : ChartSeriesState.Success;
    private static void Range(ChartSeries series, out double min, out double max) {
        if (series.Points.Count < 2) throw new InvalidOperationException("Circle and bullet charts require minimum and maximum points.");
        min = series.Points[0].X; max = series.Points[1].X;
        if (!Finite(min) || !Finite(max) || max <= min || !Finite(max - min)) throw new InvalidOperationException("Scalar progress charts require a finite positive range.");
    }
    private static string Value(Chart chart, ChartSeries series, int index, double value) => index < series.PointLabels.Count && series.PointLabels[index] != null ? series.PointLabels[index]! : ChartNumericFormatter.FormatValue(chart.Options, value);
    private static string Category(Chart chart, double value) => ChartAxisValueFormatter.FindExplicitLabel(chart.Options.XAxisLabels, value) ?? ChartAxisValueFormatter.Format(chart.Options.XAxis, value);
    private static string Id(int series, int? point = null) => "series-" + series.ToString(CultureInfo.InvariantCulture) + (point.HasValue ? "-point-" + point.Value.ToString(CultureInfo.InvariantCulture) : "");
    private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static TextStyle Style(Chart chart, VisualRenderContext context, ChartSeries series, int point, ChartColor color, double size, int weight, bool ticks = false) {
        var fallback = new TextStyle { Font = context.Font, FontSize = size, Color = color }; fallback.Font.Weight = weight;
        if (ticks) return chart.Options.TickLabelStyle.Resolve(fallback);
        var style = series.DataLabelStyle.Resolve(chart.Options.DataLabelStyle.Resolve(fallback));
        return point >= 0 && point < series.PointDataLabelStyles.Count && series.PointDataLabelStyles[point] != null ? series.PointDataLabelStyles[point]!.Resolve(style) : style;
    }

    private static void Text(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartSeries series, int point,
        string text, ChartRect bounds, string role, string id, ChartColor color, double size, int weight = 400, bool ticks = false,
        TextAlignment alignment = TextAlignment.Center) =>
        VisualRadialPrimitives.Text(builder, text, bounds, Style(chart, context, series, point, color, Math.Min(size, Math.Max(.1, bounds.Height / 1.3)), weight, ticks), role, id,
            alignment: alignment);
}
