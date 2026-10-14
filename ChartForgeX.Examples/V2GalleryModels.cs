using ChartForgeX.Core;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

/// <summary>Executable gallery models shared by focused downloadable recipes and rendering tests.</summary>
public static class V2GalleryModels {
    /// <summary>Creates the selected family from the same compiled source included in its download.</summary>
    public static Chart Create(ChartSeriesKind kind, string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) =>
        (Chart)ExampleRecipes.ForKind(kind, variant).Create(variant, mode);

    public static ChartGrid CreateGrid() => ChartGridRecipe.Create();
    public static TopologyChart CreateTopology(bool atlas = false) => TopologyRecipe.Create(atlas ? "shapes" : "expanded");
    public static FlowArtifact CreateFlow() => FlowRecipe.Create();
    public static SequenceArtifact CreateSequence() => SequenceRecipe.Create();
    public static Chart CreateMixedRadial(double? curveTension = null) => MixedRadialRecipe.Create(curveTension: curveTension);
    public static Chart CreateRadialBarScales() => RadialBarScalesRecipe.Create();
    public static Chart CreateHistogram(string variant) => HistogramRecipe.Create(variant);

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
        ChartSeriesKind.DifferenceArea => "Actual and forecast requests", ChartSeriesKind.RangeArea => "Expected operating range", ChartSeriesKind.RangeBar => "Minimum and maximum delivery times",
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

}
