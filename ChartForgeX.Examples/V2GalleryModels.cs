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
        var funnelVariant = variant is "cone-vertical" or "stage-bars-horizontal";
        var precisionVariant = (variant is "precision" or "compact-precision") && kind is ChartSeriesKind.TrendLine or ChartSeriesKind.Gauge;
        var authoredHierarchyVariant = (variant is "authored-total" or "compact-authored-total") && kind == ChartSeriesKind.Sunburst;
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options") && !(funnelVariant && kind == ChartSeriesKind.Funnel) && !precisionVariant && !authoredHierarchyVariant)
            throw new ArgumentOutOfRangeException(nameof(variant));
        var chart = (precisionVariant ? kind == ChartSeriesKind.Gauge ? GaugePrecision() : AxisPrecision() : null) ?? (variant is "options" or "compact-options" || funnelVariant ? GeometryOptions(kind, variant) : null)
            ?? Basic(kind, variant) ?? Ranges(kind, variant) ?? Radial(kind, variant) ?? MatrixMap(kind, variant, mode) ?? Specialty(kind, variant, mode)
            ?? throw new ArgumentOutOfRangeException(nameof(kind));
        if (!precisionVariant) chart.WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
        if (variant == "options" && kind == ChartSeriesKind.Area) {
            chart.Series[0].WithFillPattern(ChartFillPattern.Crosshatch);
            chart.Series[0].StateRole = ChartSeriesState.Warning;
            chart.WithDataLabels();
        }
        return chart;
    }

    /// <summary>Gets a short title describing the actual example data.</summary>
    public static string Title(ChartSeriesKind kind) => kind switch {
        ChartSeriesKind.Line => "Weekly request volume", ChartSeriesKind.StepLine => "Requests waiting in the queue",
        ChartSeriesKind.Area => "Capacity used during the week", ChartSeriesKind.StepArea => "Reserved processing capacity",
        ChartSeriesKind.Bar => "Completed orders by region", ChartSeriesKind.HorizontalBar => "Regional order fulfilment",
        ChartSeriesKind.StackedArea => "Requests and follow-ups over time", ChartSeriesKind.TrendLine => "Request volume and its trend",
        ChartSeriesKind.Scatter => "Processing time by workload", ChartSeriesKind.Bubble => "Workload, time and request volume",
        ChartSeriesKind.Lollipop => "Orders at each delivery stage", ChartSeriesKind.Candlestick => "Daily opening and closing prices",
        ChartSeriesKind.Ohlc => "Daily market price ranges", ChartSeriesKind.ErrorBar => "Delivery time and uncertainty",
        ChartSeriesKind.BoxPlot => "Delivery time by service level", ChartSeriesKind.RangeBand => "Expected temperature band",
        ChartSeriesKind.RangeArea => "Expected operating range", ChartSeriesKind.RangeBar => "Minimum and maximum delivery times",
        ChartSeriesKind.Dumbbell => "Service capacity before and after", ChartSeriesKind.Slope => "Capacity change across teams",
        ChartSeriesKind.Heatmap => "Coverage across service areas", ChartSeriesKind.HexbinHeatmap => "Service coverage in hexagonal cells",
        ChartSeriesKind.CalendarHeatmap => "Daily activity across four months", ChartSeriesKind.DottedMap => "Service routes across Europe",
        ChartSeriesKind.RegionMap => "Regional service coverage", ChartSeriesKind.TileMap => "Service coverage by state",
        ChartSeriesKind.Gauge => "Available capacity against a target", ChartSeriesKind.Circle => "Assessments completed",
        ChartSeriesKind.ProgressRing => "Completion by delivery stage", ChartSeriesKind.LayeredRadial => "Reviewed, verified and completed",
        ChartSeriesKind.RadialBar => "Requests by region and service", ChartSeriesKind.RadialColumn => "Regional service workload",
        ChartSeriesKind.Bullet => "Reviewed work against its targets", ChartSeriesKind.ProgressBar => "Progress through delivery stages",
        ChartSeriesKind.Pie => "Revenue by service", ChartSeriesKind.Donut => "Services contributing to revenue",
        ChartSeriesKind.PolarArea => "Service revenue in radial segments", ChartSeriesKind.Radar => "Observed and expected service levels",
        ChartSeriesKind.Polar => "Directional sensor readings", ChartSeriesKind.Timeline => "Project activities in March 2026",
        ChartSeriesKind.Gantt => "Project delivery in March 2026", ChartSeriesKind.StateTimeline => "Availability throughout the day",
        ChartSeriesKind.GanttLane => "Incidents and recovery in parallel",
        ChartSeriesKind.Sankey => "Requests across processing stages", ChartSeriesKind.Tree => "Teams and their responsibilities",
        ChartSeriesKind.Chord => "Transfers between teams",
        ChartSeriesKind.Sunburst => "Allocation through the team hierarchy", ChartSeriesKind.Treemap => "How work is allocated across teams",
        ChartSeriesKind.Pictorial => "Completed assessments by team", ChartSeriesKind.WordCloud => "Topics in service observations",
        ChartSeriesKind.Funnel => "Requests from receipt to completion", ChartSeriesKind.Pyramid => "How work is allocated across services",
        ChartSeriesKind.Waterfall => "Changes in available capacity",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static ChartPoint[] Observations(string variant, double offset = 0) => (variant == "sparse" ? new[] { 1, 3, 6 } : Enumerable.Range(1, 6))
        .Select(index => new ChartPoint(index, 20 + offset + index * 7 % 29, variant == "sparse" && index == 6)).ToArray();
    private static Chart Categories(ChartSeriesKind kind) => Chart.Create().WithXLabels(kind switch {
        ChartSeriesKind.Line or ChartSeriesKind.StepLine or ChartSeriesKind.Area or ChartSeriesKind.StepArea or ChartSeriesKind.StackedArea or ChartSeriesKind.TrendLine or ChartSeriesKind.Candlestick or ChartSeriesKind.Ohlc => new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" },
        ChartSeriesKind.ErrorBar or ChartSeriesKind.BoxPlot or ChartSeriesKind.RangeBar or ChartSeriesKind.Dumbbell => new[] { "Standard", "Express", "Premium", "Bulk", "Return" },
        ChartSeriesKind.Heatmap or ChartSeriesKind.HexbinHeatmap => new[] { "Identity", "Network", "Storage", "Backup", "Access", "Recovery" },
        ChartSeriesKind.Pie or ChartSeriesKind.Donut or ChartSeriesKind.PolarArea => new[] { "Subscriptions", "Consulting", "Support", "Licenses", "Training", "Other" },
        ChartSeriesKind.ProgressRing => new[] { "Received", "Qualified", "Reviewed", "Verified", "Approved", "Complete" },
        ChartSeriesKind.Radar => new[] { "Speed", "Coverage", "Capacity", "Quality", "Recovery", "Support" },
        ChartSeriesKind.Scatter or ChartSeriesKind.Bubble => new[] { "1", "2", "3", "4", "5", "6" },
        ChartSeriesKind.Lollipop => new[] { "Received", "Packed", "Shipped", "In transit", "Delivered", "Reviewed" },
        ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea => new[] { "08:00", "10:00", "12:00", "14:00", "16:00" },
        _ => new[] { "North", "South", "East", "West", "Central", "Online" }
    });
    private static Chart? Basic(ChartSeriesKind kind, string variant) {
        var chart = Categories(kind); var values = Observations(variant);
        return kind switch {
            ChartSeriesKind.Line => chart.AddLine("Requests", values),
            ChartSeriesKind.StepLine => chart.AddStepLine("Queued", values),
            ChartSeriesKind.Area => chart.AddArea("Used capacity", values),
            ChartSeriesKind.StepArea => chart.AddStepArea("Reserved capacity", values),
            ChartSeriesKind.Scatter => chart.WithXAxis("Workload batches").WithYAxis("Time (minutes)").AddScatter("Processing time", values),
            ChartSeriesKind.Bar => chart.AddBar("Orders", values),
            ChartSeriesKind.Lollipop => chart.AddLollipop("Orders", values),
            ChartSeriesKind.HorizontalBar => chart.AddHorizontalBar("Orders", values),
            ChartSeriesKind.StackedArea => chart.AddStackedArea("Requests", values).AddStackedArea("Follow-ups", Observations(variant, -10)),
            ChartSeriesKind.Waterfall => chart.AddWaterfall("Changes", new[] { new ChartPoint(1, 60), new ChartPoint(2, -15), new ChartPoint(3, 25), new ChartPoint(4, -10), new ChartPoint(5, 12) }),
            ChartSeriesKind.Slope => Chart.Create().AddSlope("Team A", 28, 64, "Before", "After").AddSlope("Team B", 52, 43, "Before", "After").AddSlope("Team C", 38, 57, "Before", "After"),
            ChartSeriesKind.TrendLine => chart.AddScatter("Observations", values).AddTrendLine("Least-squares trend", values),
            _ => null
        };
    }
}
