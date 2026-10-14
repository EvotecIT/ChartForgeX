using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void DrawDifferenceArea(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartMapper map, int index, VisualThemeColors colors, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles) {
        var series = chart.Series[index]; var geometry = new ChartPairedBoundaryGeometry(series, map);
        var color = Color(series, index, colors); var paint = VisualChartPaint.Series(series, color);
        var stroke = series.HasExplicitStrokeWidth ? series.StrokeWidth : context.Theme.SeriesStrokeWidth;
        foreach (var region in ChartDifferenceAreaGeometry.Regions(series, geometry)) {
            var role = region.Positive ? "difference-positive" : "difference-negative";
            var (fill, fillPaint) = DifferenceFill(series, index, region.Positive, colors, context);
            using (FillSegment(builder, region.Start, region.Count)) {
                builder.Path(region.Path, fill, role: role, close: true, paint: VisualChartPaint.Fill(fillPaint));
                if (fill.A > 0) DrawPattern(builder, region.Path, series.FillPattern, fill,
                    ChartStateMark.Backdrop(chart.Options, colors, context.Frame), role + "-pattern");
            }
            obstacles.Add(new LabelObstacle(SeriesId(index) + "-" + role + "-" + Number(obstacles.Count),
                new LabelMarkShape(new[] { region.Path.Flatten() }, true, stroke, plot)));
        }
        DrawLayeredPath(chart, builder, ChartPathBuilder.FromPoints(geometry.First, series.Interpolation, series.StepPosition), color, paint, stroke, "difference-first");
        DrawLayeredPath(chart, builder, ChartPathBuilder.FromPoints(geometry.Second, series.Interpolation, series.StepPosition), color, paint, stroke, "difference-second");
        for (var item = 0; item < geometry.First.Count; item++) {
            var first = series.RenderPoints[item * 2]; var second = series.RenderPoints[item * 2 + 1];
            var hasOverride = item < series.PointColors.Count && series.PointColors[item].HasValue
                || item < series.PointFillPatterns.Count && series.PointFillPatterns[item].HasValue;
            var showMarkers = VisualMarkerScene.Enabled(series, hasOverride || series.MarkerRadius.HasValue);
            var radius = showMarkers ? series.MarkerRadius ?? context.Theme.MarkerRadius : 0;
            var a = geometry.First[item]; var b = geometry.Second[item];
            var bounds = Extents(a.X, a.Y, b.X, b.Y, VisualMarkerScene.Extent(series, radius));
            var firstText = Value(chart, first.Y); var secondText = Value(chart, second.Y);
            var options = series.DifferenceArea;
            var label = ResolveObservationLabel(chart, context, series, item, colors,
                () => options.FirstName + ": " + firstText + "; " + options.SecondName + ": " + secondText);
            var facts = new Dictionary<string, string> {
                ["data-cfx-label-first"] = options.FirstName, ["data-cfx-label-second"] = options.SecondName,
                ["data-cfx-formatted-first"] = firstText, ["data-cfx-formatted-second"] = secondText
            };
            using (ObservationGroup(builder, series, index, item, item * 2, 2, bounds, label, facts,
                ("x", first.X), ("first", first.Y), ("second", second.Y))) {
                if (showMarkers) {
                    var pointColor = PointColor(series, index, item, colors); var pointPaint = VisualChartPaint.Series(series, pointColor, item);
                    foreach (var point in new[] { a, b }) VisualMarkerScene.Draw(builder, series, item, point.X, point.Y, radius,
                        pointColor, pointPaint, "difference-marker", pattern: ObservationPattern(series, item),
                        backdrop: ChartStateMark.Backdrop(chart.Options, colors, context.Frame), patternRole: "difference-marker-pattern");
                }
            }
            var placement = series.DataLabelPlacement ?? chart.Options.DataLabelPlacement;
            var y = placement == ChartDataLabelPlacement.Below ? Math.Max(a.Y, b.Y)
                : placement is ChartDataLabelPlacement.Left or ChartDataLabelPlacement.Right ? a.Y / 2 + b.Y / 2 : Math.Min(a.Y, b.Y);
            AddLabel(chart, context, series, index, item, new ChartPoint(a.X, y), bounds, label, labels, first.Y);
        }
    }

    internal static (ChartColor Color, SvgPaint Paint) DifferenceFill(ChartSeries series, int index, bool positive,
        VisualThemeColors colors, VisualRenderContext context) {
        var options = series.DifferenceArea;
        var configured = positive ? options.PositiveFill : options.NegativeFill;
        var color = configured ?? (positive || series.Color.HasValue || series.StateRole != ChartSeriesState.None
            ? Color(series, index, colors) : colors.Palette[(index + 1) % colors.Palette.Count]);
        var opacity = (positive ? options.PositiveFillOpacity : options.NegativeFillOpacity) ?? context.Theme.AreaOpacity;
        var fill = ChartColorMath.WithOpacity(color, opacity);
        return (fill, VisualChartPaint.Series(series, color, explicitOverride: configured.HasValue).WithOpacity(fill, opacity));
    }
}
