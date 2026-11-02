using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void DrawDifferenceArea(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartMapper map, int index, VisualThemeColors colors, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles,
        ChartAxisWindow? xViewport = null, ChartAxisWindow? yViewport = null) {
        var series = chart.Series[index];
        var axis = series.ValueAxis == ChartAxisSide.Secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
        var geometry = new ChartPairedBoundaryGeometry(series, map, axis.Reversed,
            axis.Scale is ChartScaleKind.Logarithmic or ChartScaleKind.SymmetricLogarithmic);
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
        DrawLayeredPath(chart, builder, ChartPairedBoundaryGeometry.BoundaryPath(series, geometry.First, true), color, paint, stroke, "difference-first");
        var (secondColor, secondPaint) = DifferenceSecondPaint(series, index, colors);
        DrawLayeredPath(chart, builder, ChartPairedBoundaryGeometry.BoundaryPath(series, geometry.Second, false), secondColor, secondPaint, stroke, "difference-second");
        for (var item = 0; item < geometry.First.Count; item++) {
            var first = series.RenderPoints[item * 2]; var second = series.RenderPoints[item * 2 + 1];
            var showMarkers = PairedObservationMarkersEnabled(series, item);
            var radius = showMarkers ? series.MarkerRadius ?? context.Theme.MarkerRadius : 0;
            var a = geometry.First[item]; var b = geometry.Second[item];
            var bounds = series.Markers.CustomPath == null ? Extents(a.X, a.Y, b.X, b.Y, VisualMarkerScene.Extent(series, radius))
                : VisualMarkerScene.PairBounds(series, a, b, radius);
            var firstText = Value(chart, first.Y); var secondText = Value(chart, second.Y);
            var options = series.DifferenceArea;
            var label = ResolveObservationLabel(chart, context, series, item, colors,
                _ => options.FirstName + ": " + firstText + "; " + options.SecondName + ": " + secondText);
            var facts = new Dictionary<string, string> {
                ["data-cfx-label-first"] = options.FirstName, ["data-cfx-label-second"] = options.SecondName,
                ["data-cfx-formatted-first"] = firstText, ["data-cfx-formatted-second"] = secondText,
                ["data-cfx-formatted-y"] = firstText
            };
            series.AddDifferenceSourceFacts(item, facts);
            var visible = ViewportObservationVisible(xViewport, yViewport, first.X, Math.Min(first.Y, second.Y), Math.Max(first.Y, second.Y), map);
            using (ObservationGroup(builder, series, index, item, item * 2, 2, bounds, label, visible, facts,
                ("x", first.X), ("first", first.Y), ("second", second.Y))) {
                if (visible && showMarkers) {
                    var pointColor = PointColor(series, index, item, colors); var pointPaint = VisualChartPaint.Series(series, pointColor, item);
                    // An authored point colour marks the observation; otherwise each marker keeps its boundary's identity.
                    var pointOverride = item < series.PointColors.Count && series.PointColors[item].HasValue;
                    foreach (var (point, ownColor, ownPaint) in new[] { (a, pointColor, pointPaint),
                        (b, pointOverride ? pointColor : secondColor, pointOverride ? pointPaint : secondPaint) })
                        VisualMarkerScene.Draw(builder, series, item, point.X, point.Y, radius,
                            ownColor, ownPaint, "difference-marker", pattern: ObservationPattern(series, item),
                            backdrop: ChartStateMark.Backdrop(chart.Options, colors, context.Frame), patternRole: "difference-marker-pattern");
                }
            }
            if (!visible) continue;
            var placement = series.DataLabelPlacement ?? chart.Options.DataLabelPlacement;
            var y = placement == ChartDataLabelPlacement.Below ? Math.Max(a.Y, b.Y)
                : placement is ChartDataLabelPlacement.Left or ChartDataLabelPlacement.Right ? a.Y / 2 + b.Y / 2 : Math.Min(a.Y, b.Y);
            AddLabel(chart, context, series, index, item, new ChartPoint(a.X, y), bounds, label, labels, first.Y);
        }
    }

    /// <summary>
    /// The Second boundary never shares the First boundary's colour: it takes the negative fill or the next palette slot,
    /// so the two observations stay distinguishable without reading the shading.
    /// </summary>
    internal static (ChartColor Color, SvgPaint Paint) DifferenceSecondPaint(ChartSeries series, int index, VisualThemeColors colors) {
        var configured = series.DifferenceArea.NegativeFill;
        var first = Color(series, index, colors);
        var color = configured.HasValue ? configured.Value.WithAlpha(255) : ChartSeriesColours.Palette(series, index, colors, 1);
        if (SameRgb(color, first)) color = ChartSeriesColours.Palette(series, index, colors, SameRgb(ChartSeriesColours.Palette(series, index, colors, 1), first) ? 2 : 1);
        color = color.WithAlpha(first.A);
        return (color, VisualChartPaint.Series(series, color, explicitOverride: configured.HasValue));
    }

    private static bool SameRgb(ChartColor a, ChartColor b) => a.R == b.R && a.G == b.G && a.B == b.B;

    internal static (ChartColor Color, SvgPaint Paint) DifferenceFill(ChartSeries series, int index, bool positive,
        VisualThemeColors colors, VisualRenderContext context) {
        var options = series.DifferenceArea;
        var configured = positive ? options.PositiveFill : options.NegativeFill;
        var color = configured ?? (positive || series.Color.HasValue || series.StateRole != ChartSeriesState.None
            ? Color(series, index, colors) : ChartSeriesColours.Palette(series, index, colors, 1));
        var opacity = (positive ? options.PositiveFillOpacity : options.NegativeFillOpacity) ?? context.Theme.AreaWash(context.ThemeMode);
        var fill = ChartColorMath.WithOpacity(color, opacity);
        return (fill, VisualChartPaint.Series(series, color, explicitOverride: configured.HasValue).WithOpacity(fill, opacity));
    }

    /// <summary>A difference area compares two observations, so its legend names both boundaries in their own colours.</summary>
    private static IEnumerable<VisualLegendEntry> DifferenceLegendEntries(Chart chart, ChartSeries series, int index, VisualThemeColors colors) {
        var first = Color(series, index, colors); var firstPaint = VisualChartPaint.Series(series, first);
        var (second, secondPaint) = DifferenceSecondPaint(series, index, colors);
        yield return new VisualLegendEntry(series.DifferenceArea.FirstName, first, SeriesId(index) + "-first", series.Kind, series.FillPattern, series.StateRole,
            series.InteractionIdentityKey, marker: VisualMarkerScene.DifferenceLegend(chart, series, first, firstPaint, positive: true), paint: firstPaint);
        yield return new VisualLegendEntry(series.DifferenceArea.SecondName, second, SeriesId(index) + "-second", series.Kind, series.FillPattern, series.StateRole,
            series.InteractionIdentityKey, marker: VisualMarkerScene.DifferenceLegend(chart, series, second, secondPaint, positive: false), paint: secondPaint);
    }
}
