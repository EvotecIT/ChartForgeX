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

    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(VisualChartFamily family, Chart chart, VisualThemeColors colors) => family switch {
        VisualChartFamily.Cartesian => VisualCartesianCompiler.LegendEntries(chart, colors),
        VisualChartFamily.Radial => VisualRadialCompiler.LegendEntries(chart, colors),
        VisualChartFamily.Gauge => VisualGaugeCompiler.LegendEntries(chart, colors),
        VisualChartFamily.RadialProgress => VisualRadialProgressCompiler.LegendEntries(chart, colors),
        VisualChartFamily.NumericRadial => VisualCartesianCompiler.LegendEntries(chart, colors),
        VisualChartFamily.Polar => VisualPolarCompiler.LegendEntries(chart, colors),
        VisualChartFamily.Scalar => VisualScalarProgressCompiler.LegendEntries(chart, colors),
        VisualChartFamily.Matrix => VisualMatrixCompiler.LegendEntries(chart, colors),
        VisualChartFamily.Schedule => VisualScheduleCompiler.LegendEntries(chart, colors),
        VisualChartFamily.Map => VisualMapCompiler.LegendEntries(chart, colors),
        VisualChartFamily.Hierarchy => VisualHierarchyCompiler.LegendEntries(chart, colors),
        VisualChartFamily.Sankey => VisualSankeyCompiler.LegendEntries(chart, colors),
        VisualChartFamily.Chord => VisualChordCompiler.LegendEntries(chart, colors),
        _ => VisualSpecialtyCompiler.LegendEntries(chart, colors)
    };

    internal static void Build(VisualChartFamily family, Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect content) {
        using var coordinateScope = family is VisualChartFamily.Radial or VisualChartFamily.RadialProgress or VisualChartFamily.Polar or VisualChartFamily.NumericRadial
            || family == VisualChartFamily.Gauge && chart.Options.Gauge.Form != ChartGaugeForm.Linear
            || family == VisualChartFamily.Hierarchy && chart.Series[0].Kind == ChartSeriesKind.Sunburst
            ? builder.PushGroup(null, "coordinate-system", new Dictionary<string, string> { ["data-cfx-coordinate-system"] = "polar" }) : null;
        switch (family) {
            case VisualChartFamily.Cartesian: VisualCartesianCompiler.BuildInViewport(chart, context, builder, content); break;
            case VisualChartFamily.Radial: VisualRadialCompiler.Build(chart, context, builder, content); break;
            case VisualChartFamily.Gauge: VisualGaugeCompiler.Build(chart, context, builder, content); break;
            case VisualChartFamily.RadialProgress: VisualRadialProgressCompiler.Build(chart, context, builder, content); break;
            case VisualChartFamily.NumericRadial: VisualNumericRadialCompiler.Build(chart, context, builder, content); break;
            case VisualChartFamily.Polar: VisualPolarCompiler.Build(chart, context, builder, content); break;
            case VisualChartFamily.Scalar: VisualScalarProgressCompiler.Build(chart, context, builder, content); break;
            case VisualChartFamily.Matrix: VisualMatrixCompiler.Build(chart, context, builder, content); break;
            case VisualChartFamily.Schedule: VisualScheduleCompiler.Build(chart, context, builder, content); break;
            case VisualChartFamily.Map: VisualMapCompiler.Build(chart, context, builder, content); break;
            case VisualChartFamily.Hierarchy: VisualHierarchyCompiler.Build(chart, context, builder, content); break;
            case VisualChartFamily.Sankey: VisualSankeyCompiler.Build(chart, context, builder, content); break;
            case VisualChartFamily.Chord: VisualChordCompiler.Build(chart, context, builder, content); break;
            default: VisualSpecialtyCompiler.Build(chart, context, builder, content); break;
        }
    }
}

internal enum VisualChartFamily { Cartesian, Radial, Gauge, RadialProgress, Polar, Scalar, Matrix, Schedule, Map, Hierarchy, Sankey, Chord, Specialty, NumericRadial }
