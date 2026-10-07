using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

/// <summary>Executable, deterministic gallery models shared by the catalog and its downloadable C# examples.</summary>
public static partial class V2GalleryModels {
    /// <summary>Creates an actual source model for every chart kind, without selecting a renderer or export backend.</summary>
    public static Chart Create(ChartSeriesKind kind, string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options")) throw new ArgumentOutOfRangeException(nameof(variant));
        var chart = Basic(kind, variant) ?? Ranges(kind, variant) ?? Radial(kind, variant) ?? MatrixMap(kind, variant, mode) ?? Specialty(kind, variant, mode)
            ?? throw new ArgumentOutOfRangeException(nameof(kind));
        chart.WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
        if (variant == "options" && kind is ChartSeriesKind.Bar or ChartSeriesKind.Area or ChartSeriesKind.HorizontalBar) {
            chart.Series[0].WithFillPattern(ChartFillPattern.Crosshatch);
            chart.Series[0].StateRole = ChartSeriesState.Warning;
            chart.WithDataLabels();
        }
        return chart;
    }

    /// <summary>Gets a short title describing the actual example data.</summary>
    public static string Title(ChartSeriesKind kind) => kind switch {
        ChartSeriesKind.Candlestick or ChartSeriesKind.Ohlc => "Daily market prices",
        ChartSeriesKind.ErrorBar or ChartSeriesKind.BoxPlot => "Delivery time distribution",
        ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea or ChartSeriesKind.RangeBar => "Expected operating ranges",
        ChartSeriesKind.Dumbbell or ChartSeriesKind.Slope => "Before and after comparison",
        ChartSeriesKind.Heatmap or ChartSeriesKind.HexbinHeatmap => "Service coverage matrix",
        ChartSeriesKind.CalendarHeatmap => "Daily activity",
        ChartSeriesKind.DottedMap => "Regional service routes",
        ChartSeriesKind.RegionMap or ChartSeriesKind.TileMap => "Regional observations",
        ChartSeriesKind.Gauge or ChartSeriesKind.Circle or ChartSeriesKind.RadialBar or ChartSeriesKind.LayeredRadial or ChartSeriesKind.Bullet or ChartSeriesKind.ProgressBar => "Capacity and completion",
        ChartSeriesKind.Pie or ChartSeriesKind.Donut or ChartSeriesKind.PolarArea => "Observations by category",
        ChartSeriesKind.Radar or ChartSeriesKind.Polar => "Directional observations",
        ChartSeriesKind.Timeline or ChartSeriesKind.Gantt or ChartSeriesKind.StateTimeline or ChartSeriesKind.GanttLane => "Service delivery schedule",
        ChartSeriesKind.Sankey => "Requests across processing stages",
        ChartSeriesKind.Tree or ChartSeriesKind.Sunburst or ChartSeriesKind.Treemap => "Allocation by team",
        ChartSeriesKind.Pictorial => "Completed assessments",
        ChartSeriesKind.WordCloud => "Topics in observations",
        ChartSeriesKind.Funnel => "Request conversion stages",
        ChartSeriesKind.Waterfall => "Changes in available capacity",
        _ => "Observations over the reporting period"
    };

    private static ChartPoint[] Observations(string variant, double offset = 0) => (variant == "sparse" ? new[] { 1, 3, 6 } : Enumerable.Range(1, 6))
        .Select(index => new ChartPoint(index, 20 + offset + index * 7 % 29, variant == "sparse" && index == 6)).ToArray();
    private static Chart Categories() => Chart.Create().WithXLabels("Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta");
    private static Chart? Basic(ChartSeriesKind kind, string variant) {
        var chart = Categories(); var values = Observations(variant);
        return kind switch {
            ChartSeriesKind.Line => chart.AddLine("Observed", values),
            ChartSeriesKind.StepLine => chart.AddStepLine("Observed", values),
            ChartSeriesKind.Area => chart.AddArea("Observed", values),
            ChartSeriesKind.StepArea => chart.AddStepArea("Observed", values),
            ChartSeriesKind.Scatter => chart.AddScatter("Observed", values),
            ChartSeriesKind.Bar => chart.AddBar("Observed", values),
            ChartSeriesKind.Lollipop => chart.AddLollipop("Observed", values),
            ChartSeriesKind.HorizontalBar => chart.AddHorizontalBar("Observed", values),
            ChartSeriesKind.StackedArea => chart.AddStackedArea("Requests", values).AddStackedArea("Follow-ups", Observations(variant, -10)),
            ChartSeriesKind.Waterfall => chart.AddWaterfall("Changes", new[] { new ChartPoint(1, 60), new ChartPoint(2, -15), new ChartPoint(3, 25), new ChartPoint(4, -10), new ChartPoint(5, 12) }),
            ChartSeriesKind.Slope => Chart.Create().AddSlope("Team A", 28, 64, "Before", "After").AddSlope("Team B", 52, 43, "Before", "After").AddSlope("Team C", 38, 57, "Before", "After"),
            ChartSeriesKind.TrendLine => chart.AddScatter("Observations", values).AddTrendLine("Least-squares trend", values),
            _ => null
        };
    }
}
