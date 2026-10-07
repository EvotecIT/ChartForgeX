using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualPolarCompiler {
    private static void Area(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, VisualThemeColors colors) {
        var series = chart.Series[0];
        var radius = Math.Max(0, Math.Min(plot.Width, plot.Height) / 2 - context.Theme.Spacing - context.Theme.SeriesStrokeWidth / 2);
        if (radius <= 0) { builder.AddDiagnostic(new VisualDiagnostic("polar.insufficient-space", "No radius remains for the polar area chart.")); return; }
        var cx = plot.Left + plot.Width / 2; var cy = plot.Top + plot.Height / 2;
        var max = series.Points.Max(point => point.Y); var total = series.Points.Sum(point => point.Y);
        if (double.IsInfinity(total)) throw new InvalidOperationException("The sum of polar area values must be finite.");
        var sweep = Math.PI * 2 / series.Points.Count;
        var labels = new List<PolarLabel>();
        using (builder.PushClip(plot)) {
            if (chart.Options.ShowGrid) for (var ring = 1; ring <= ChartVisualPrimitives.PolarAreaGridRings; ring++)
                builder.Ellipse(cx, cy, radius * ring / ChartVisualPrimitives.PolarAreaGridRings, radius * ring / ChartVisualPrimitives.PolarAreaGridRings,
                    null, colors.Border, context.Theme.GridStrokeWidth, "polar-area-ring");
            for (var index = 0; index < series.Points.Count; index++) {
                var point = series.Points[index]; var color = ChartSeriesColours.Point(series, index, index, colors);
                var start = -Math.PI / 2 + index * sweep; var zero = point.Y == 0;
                var r = zero ? radius : radius * Math.Sqrt(point.Y / max); var inner = zero ? radius * ChartVisualPrimitives.PolarAreaZeroSlotInnerRadiusFactor : 0;
                var value = index < series.PointLabels.Count && series.PointLabels[index] != null ? series.PointLabels[index]! : ChartNumericFormatter.FormatValue(chart.Options, point.Y);
                var category = Category(chart, point.X); var role = zero ? "polar-area-zero-slot" : "polar-area-segment";
                var bounds = new ChartRect(cx - r, cy - r, r * 2, r * 2);
                builder.AddRegion(new VisualSemanticRegion(Id(0, index), role, bounds, category + ": " + value));
                using (builder.PushGroup(Id(0, index), "polar-area-point-source", new Dictionary<string, string> {
                    ["data-cfx-point"] = N(index), ["data-cfx-label"] = category, ["data-cfx-full-label"] = value,
                    ["data-cfx-value"] = N(point.Y), ["data-cfx-percent"] = N(total > 0 ? point.Y / total : 0)
                })) {
                    builder.Slice(cx, cy, r, inner, start, sweep, zero ? ChartColorMath.WithOpacity(color, ChartVisualPrimitives.PolarAreaZeroSlotFillOpacity) : color,
                        zero ? color : colors.Surface, context.Theme.GridStrokeWidth, role);
                    var pattern = Pattern(series, index);
                    if (!zero && pattern != ChartFillPattern.None) builder.PatternSlice(cx, cy, r, 0, start, sweep, pattern,
                        ChartColorMath.AccessibleTextOnBackground(color).WithAlpha(100), role: "polar-area-pattern");
                }
                if (!zero && (series.ShowDataLabels ?? chart.Options.ShowDataLabels)) {
                    var angle = start + sweep / 2; var position = new ChartPoint(cx + Math.Cos(angle) * r * .65, cy + Math.Sin(angle) * r * .65);
                    AddDataLabel(chart, context, builder, plot, labels, series, index, Id(0, index), value, position, angle, false, 0, 1);
                }
            }
            DrawLabels(builder, plot, labels, context.Theme.Spacing);
        }
        if (max == 0) builder.AddDiagnostic(new VisualDiagnostic("polar-area.all-zero", "All polar area values are zero; their category slots remain visible."));
    }
}
