using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private sealed class ResolvedPlotRegion {
        internal ResolvedPlotRegion(ChartPlotRegion source, ChartRect bounds, int index) {
            Source = source; Bounds = bounds; Id = "plot-region-" + Number(index);
        }
        internal ChartPlotRegion Source { get; }
        internal ChartRect Bounds { get; }
        internal string Id { get; }
    }

    /// <summary>Intersects authored bounds with the final data domain before any scale projection.</summary>
    private static IReadOnlyList<ResolvedPlotRegion> ResolvePlotRegions(Chart chart, ChartRange range,
        ChartRange? secondaryRange, ChartMapper map, ChartMapper? secondaryMap) {
        var regions = new List<ResolvedPlotRegion>();
        for (var i = 0; i < chart.PlotRegions.Count; i++) {
            var source = chart.PlotRegions[i];
            var selectedRange = source.YAxis == ChartAxisSide.Secondary ? secondaryRange! : range;
            var selectedMap = source.YAxis == ChartAxisSide.Secondary ? secondaryMap! : map;
            var x0 = Clamp(source.XMinimum ?? range.MinX, range.MinX, range.MaxX);
            var x1 = Clamp(source.XMaximum ?? range.MaxX, range.MinX, range.MaxX);
            var y0 = Clamp(source.YMinimum ?? selectedRange.MinY, selectedRange.MinY, selectedRange.MaxY);
            var y1 = Clamp(source.YMaximum ?? selectedRange.MaxY, selectedRange.MinY, selectedRange.MaxY);
            var left = selectedMap.X(x0); var right = selectedMap.X(x1);
            var top = selectedMap.Y(y1); var bottom = selectedMap.Y(y0);
            regions.Add(new ResolvedPlotRegion(source, new ChartRect(Math.Min(left, right), Math.Min(top, bottom),
                Math.Abs(right - left), Math.Abs(bottom - top)), i));
        }
        return regions;
    }

    private static double Clamp(double value, double minimum, double maximum) => Math.Max(minimum, Math.Min(maximum, value));

    private static Dictionary<string, string> PlotRegionMetadata(ResolvedPlotRegion region) {
        var source = region.Source;
        return new Dictionary<string, string> {
            ["data-cfx-x-minimum"] = Optional(source.XMinimum), ["data-cfx-x-maximum"] = Optional(source.XMaximum),
            ["data-cfx-y-minimum"] = Optional(source.YMinimum), ["data-cfx-y-maximum"] = Optional(source.YMaximum),
            ["data-cfx-y-axis"] = source.YAxis.ToString().ToLowerInvariant(), ["data-cfx-label"] = source.Label,
            ["data-cfx-show-label"] = source.ShowLabel ? "true" : "false", ["aria-label"] = source.Label,
            ["data-cfx-passive"] = "true"
        };
    }

    private static string Optional(double? value) => value.HasValue ? Number(value.Value) : string.Empty;

    private static ChartColor PlotRegionColor(ChartPlotRegion source, VisualThemeColors colors) =>
        source.PaletteIndex.HasValue ? colors.Palette[source.PaletteIndex.Value % colors.Palette.Count] : source.Color!.Value;

    private static void DrawPlotRegionFills(VisualSceneBuilder builder, IReadOnlyList<ResolvedPlotRegion> regions, VisualThemeColors colors) {
        foreach (var region in regions) {
            using (builder.PushGroup(region.Id, "plot-region", PlotRegionMetadata(region))) {
                var source = region.Source;
                var color = PlotRegionColor(source, colors);
                var paint = source.PaletteIndex.HasValue ? SvgPaint.Of(color, SvgColorRole.Series) : SvgPaint.Literal(color);
                if (region.Bounds.Width > 0 && region.Bounds.Height > 0)
                    builder.Rect(region.Bounds, ChartColorMath.WithOpacity(color, source.Opacity), role: "plot-region-fill",
                        paint: VisualChartPaint.Fill(paint.WithOpacity(ChartColorMath.WithOpacity(color, source.Opacity), source.Opacity)));
                builder.AddRegion(new VisualSemanticRegion(region.Id, "plot-region", region.Bounds, source.Label));
            }
        }
    }

    private static void DrawPlotRegionLabels(VisualRenderContext context, VisualSceneBuilder builder,
        IReadOnlyList<ResolvedPlotRegion> regions, VisualThemeColors colors, List<LabelObstacle> obstacles) {
        foreach (var region in regions) {
            var source = region.Source; var bounds = region.Bounds;
            if (!source.ShowLabel || source.Label.Length == 0 || bounds.Width <= 0 || bounds.Height <= 0) continue;
            // A fitted, opaque caption plate keeps ink readable across overlapping fills.
            // Reuse the annotation caption treatment rather than sampling layered regions.
            var color = PlotRegionColor(source, colors);
            var surface = new ChartColorBlend(ChartColor.FromRgb(colors.Surface.R, colors.Surface.G, colors.Surface.B), SvgColorRole.Surface,
                ChartColor.FromRgb(color.R, color.G, color.B), source.PaletteIndex.HasValue ? SvgColorRole.Series : null, .12);
            var ink = ChartColorBlend.Contrast(surface);
            var style = new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = ink.Color };
            var candidates = new[] { new LabelCandidate(bounds.Width / 2, bounds.Height / 2, .5, .5),
                new LabelCandidate(0, 0), new LabelCandidate(bounds.Width, 0, 1),
                new LabelCandidate(0, bounds.Height, 0, 1), new LabelCandidate(bounds.Width, bounds.Height, 1, 1) };
            var request = new LabelPlacementRequest(source.Label, new ChartPoint(bounds.Left, bounds.Top), style, candidates) { Padding = 4 };
            var label = new LabelPlacementService().Place(new[] { request }, bounds, obstacles, 2, builder.MeasureText)[0];
            if (label.IsDropped || label.IsEllipsized)
                builder.AddDiagnostic(new VisualDiagnostic("cartesian.plot-region-label-overflow", "A plot region caption was shortened or omitted to fit its visible region and avoid marks and other labels."));
            if (label.IsDropped) continue;
            obstacles.Add(new LabelObstacle(region.Id + "-label", label.Bounds));
            using (builder.PushGroup(region.Id + "-caption", "plot-region-caption", PlotRegionMetadata(region))) {
                builder.Rect(label.Bounds, surface.Color, radius: Math.Min(4, context.Theme.BarRadius),
                    role: "plot-region-label-backplate", paint: VisualChartPaint.Fill(surface.Paint));
                builder.Text(label.Text, label.Bounds.Left + request.Padding, label.Bounds.Top + request.Padding + builder.TextAscent(style),
                    style, role: "plot-region-label", paint: ink.Paint);
            }
        }
    }
}
