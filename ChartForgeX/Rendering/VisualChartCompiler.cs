using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Routes model families to native scene producers; export backends do not choose layout.</summary>
internal static class VisualChartCompiler {
    internal static VisualChartFamily Family(Chart chart) {
        if (chart.Series.All(series => VisualCartesianCompiler.Supports(series.Kind))) return VisualChartFamily.Cartesian;
        if (ChartSeriesKindTraits.UsesNumericRadialCoordinates(chart)) return VisualChartFamily.NumericRadial;
        if (chart.Series.All(series => ChartMapComposition.IsMap(series.Kind))) return VisualChartFamily.Map;
        if (chart.Series.All(series => ChartSeriesKindTraits.IsPieLikeKind(series.Kind))) return VisualChartFamily.Radial;
        if (ChartSeriesKindTraits.IsMixedSchedule(chart)) return VisualChartFamily.Schedule;
        var kind = chart.Series[0].Kind;
        if (chart.Series.Any(series => series.Kind != kind)) throw new InvalidOperationException("These chart families cannot share one coordinate system.");
        return kind switch {
            ChartSeriesKind.Pie or ChartSeriesKind.Donut => VisualChartFamily.Radial,
            ChartSeriesKind.Gauge => VisualChartFamily.Gauge,
            ChartSeriesKind.ProgressRing or ChartSeriesKind.LayeredRadial => VisualChartFamily.RadialProgress,
            ChartSeriesKind.RadialBar or ChartSeriesKind.RadialColumn => VisualChartFamily.NumericRadial,
            ChartSeriesKind.Polar or ChartSeriesKind.Radar or ChartSeriesKind.PolarArea => VisualChartFamily.Polar,
            ChartSeriesKind.Circle or ChartSeriesKind.Bullet or ChartSeriesKind.ProgressBar => VisualChartFamily.Scalar,
            ChartSeriesKind.Heatmap or ChartSeriesKind.HexbinHeatmap or ChartSeriesKind.CalendarHeatmap => VisualChartFamily.Matrix,
            ChartSeriesKind.Timeline or ChartSeriesKind.Gantt or ChartSeriesKind.StateTimeline or ChartSeriesKind.GanttLane => VisualChartFamily.Schedule,
            ChartSeriesKind.DottedMap or ChartSeriesKind.RegionMap or ChartSeriesKind.TileMap => VisualChartFamily.Map,
            ChartSeriesKind.Tree or ChartSeriesKind.Sunburst or ChartSeriesKind.Treemap => VisualChartFamily.Hierarchy,
            ChartSeriesKind.Sankey => VisualChartFamily.Sankey,
            ChartSeriesKind.Chord => VisualChartFamily.Chord,
            ChartSeriesKind.Funnel or ChartSeriesKind.Pyramid or ChartSeriesKind.Pictorial or ChartSeriesKind.WordCloud => VisualChartFamily.Specialty,
            _ => throw new NotSupportedException("The chart has no native scene producer.")
        };
    }

    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(VisualChartFamily family, Chart chart, VisualThemeColors colors,
        ChartAxisValueFormatter.Cache? axisLabels = null, ChartBubbleSizeScale? bubbleScale = null, ChartColorPresentationSet? colorScales = null) => family switch {
        VisualChartFamily.Cartesian => VisualCartesianCompiler.LegendEntries(chart, colors, axisLabels, bubbleScale, colorScales),
        VisualChartFamily.Radial => VisualRadialCompiler.LegendEntries(chart, colors),
        VisualChartFamily.Gauge => VisualGaugeCompiler.LegendEntries(chart, colors),
        VisualChartFamily.RadialProgress => VisualRadialProgressCompiler.LegendEntries(chart, colors),
        VisualChartFamily.NumericRadial => VisualCartesianCompiler.LegendEntries(chart, colors, axisLabels),
        VisualChartFamily.Polar => VisualPolarCompiler.LegendEntries(chart, colors),
        VisualChartFamily.Scalar => VisualScalarProgressCompiler.LegendEntries(chart, colors),
        VisualChartFamily.Matrix => VisualMatrixCompiler.LegendEntries(chart, colors),
        VisualChartFamily.Schedule => VisualScheduleCompiler.LegendEntries(chart, colors),
        VisualChartFamily.Map => VisualMapCompiler.LegendEntries(chart, colors, colorScales),
        VisualChartFamily.Hierarchy => VisualHierarchyCompiler.LegendEntries(chart, colors, colorScales),
        VisualChartFamily.Sankey => VisualSankeyCompiler.LegendEntries(chart, colors),
        VisualChartFamily.Chord => VisualChordCompiler.LegendEntries(chart, colors),
        _ => VisualSpecialtyCompiler.LegendEntries(chart, colors)
    };

    internal static ChartCartesianProjection? Build(VisualChartFamily family, Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect content,
        ChartAxisValueFormatter.Cache? axisLabels = null, ChartBubbleSizeScale? bubbleScale = null, ChartColorPresentationSet? colorScales = null, double legendHeight = 0,
        ChartCartesianViewport? viewport = null, TileMapLegendInset? mapLegendInset = null, ChartCartesianFullDomain? fullDomain = null) {
        using var coordinateScope = family is VisualChartFamily.Radial or VisualChartFamily.RadialProgress or VisualChartFamily.Polar or VisualChartFamily.NumericRadial
            || family == VisualChartFamily.Gauge && chart.Options.Gauge.Form != ChartGaugeForm.Linear
            || family == VisualChartFamily.Hierarchy && chart.Series[0].Kind == ChartSeriesKind.Sunburst
            ? builder.PushGroup(null, "coordinate-system", new Dictionary<string, string> { ["data-cfx-coordinate-system"] = "polar" }) : null;
        if (family is VisualChartFamily.Cartesian or VisualChartFamily.NumericRadial or VisualChartFamily.Polar)
            ReportPaletteReuse(chart, context.Theme.Resolve(context.ThemeMode).Palette.Count, builder);
        switch (family) {
            case VisualChartFamily.Cartesian: return VisualCartesianCompiler.BuildInViewport(chart, context, builder, content, bubbleScale, axisLabels, colorScales, legendHeight, viewport, fullDomain);
            case VisualChartFamily.Radial: VisualRadialCompiler.Build(chart, context, builder, content); break;
            case VisualChartFamily.Gauge: VisualGaugeCompiler.Build(chart, context, builder, content); break;
            case VisualChartFamily.RadialProgress: VisualRadialProgressCompiler.Build(chart, context, builder, content); break;
            case VisualChartFamily.NumericRadial: VisualNumericRadialCompiler.Build(chart, context, builder, content, axisLabels); break;
            case VisualChartFamily.Polar: VisualPolarCompiler.Build(chart, context, builder, content); break;
            case VisualChartFamily.Scalar: VisualScalarProgressCompiler.Build(chart, context, builder, content); break;
            case VisualChartFamily.Matrix: VisualMatrixCompiler.Build(chart, context, builder, content, colorScales?.Heatmap); break;
            case VisualChartFamily.Schedule: VisualScheduleCompiler.Build(chart, context, builder, content); break;
            case VisualChartFamily.Map: VisualMapCompiler.Build(chart, context, builder, content, colorScales?.Map, mapLegendInset, colorScales); break;
            case VisualChartFamily.Hierarchy: VisualHierarchyCompiler.Build(chart, context, builder, content, colorScales); break;
            case VisualChartFamily.Sankey: VisualSankeyCompiler.Build(chart, context, builder, content, colorScales); break;
            case VisualChartFamily.Chord: VisualChordCompiler.Build(chart, context, builder, content, colorScales); break;
            default: VisualSpecialtyCompiler.Build(chart, context, builder, content); break;
        }
        return null;
    }

    /// <summary>
    /// A seventh series is never a generated hue: when more series take automatic colours than the palette has slots,
    /// slots repeat from the first, and the preparation says so instead of reusing them silently.
    /// </summary>
    internal static void ReportPaletteReuse(Chart chart, int slots, VisualSceneBuilder builder) {
        if (slots <= 0 || chart.Options.SeriesColorAssignment == ChartSeriesColorAssignment.Identity) return;
        var automatic = chart.Series.Count(series => !series.Color.HasValue && series.StateRole == ChartSeriesState.None
            && series.Kind != ChartSeriesKind.PolarArea);
        if (automatic <= slots) return;
        builder.AddDiagnostic(PaletteReused(automatic, slots, "series"));
    }

    internal static VisualDiagnostic PaletteReused(int count, int slots, string items) => new("palette.reused",
        count.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + items + " use automatic colours but the palette has "
        + slots.ToString(System.Globalization.CultureInfo.InvariantCulture) + " slots, so colours repeat from the first. Group the rest into Other, give them explicit colours, or use small multiples.",
        ChartForgeX.Diagnostics.VisualDiagnosticSeverity.Information);
}

internal enum VisualChartFamily { Cartesian, Radial, Gauge, RadialProgress, Polar, Scalar, Matrix, Schedule, Map, Hierarchy, Sankey, Chord, Specialty, NumericRadial }
