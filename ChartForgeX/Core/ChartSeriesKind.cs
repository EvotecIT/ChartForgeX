namespace ChartForgeX.Core;

/// <summary>
/// Defines the visual style used to render a chart series.
/// </summary>
public enum ChartSeriesKind {
    /// <summary>
    /// Renders connected line segments.
    /// </summary>
    Line,

    /// <summary>
    /// Renders horizontal-then-vertical step line segments.
    /// </summary>
    StepLine,

    /// <summary>
    /// Renders a filled area under connected line segments.
    /// </summary>
    Area,

    /// <summary>
    /// Renders a filled area under horizontal-then-vertical step segments.
    /// </summary>
    StepArea,

    /// <summary>
    /// Renders independent point markers.
    /// </summary>
    Scatter,

    /// <summary>
    /// Renders x/y points with marker area scaled by a third value.
    /// </summary>
    Bubble,

    /// <summary>
    /// Renders point estimates with lower and upper uncertainty bounds.
    /// </summary>
    ErrorBar,

    /// <summary>
    /// Renders open, high, low, and close values.
    /// </summary>
    Candlestick,

    /// <summary>
    /// Renders a filled band between lower and upper values across x positions.
    /// </summary>
    RangeBand,

    /// <summary>
    /// Renders a filled interval area with emphasized upper and lower bounds.
    /// </summary>
    RangeArea,

    /// <summary>
    /// Renders vertical bars.
    /// </summary>
    Bar,

    /// <summary>
    /// Renders vertical stems with circular value markers.
    /// </summary>
    Lollipop,

    /// <summary>
    /// Renders paired values connected by a line and endpoint markers.
    /// </summary>
    Dumbbell,

    /// <summary>
    /// Renders vertical interval bars from start to end values.
    /// </summary>
    RangeBar,

    /// <summary>
    /// Renders statistical box and whisker summaries.
    /// </summary>
    BoxPlot,

    /// <summary>
    /// Renders horizontal bars.
    /// </summary>
    HorizontalBar,

    /// <summary>
    /// Renders a matrix of colored value cells.
    /// </summary>
    Heatmap,

    /// <summary>
    /// Renders a matrix of colored hexagonal value cells.
    /// </summary>
    HexbinHeatmap,

    /// <summary>
    /// Renders date-indexed values as a contribution-style calendar grid.
    /// </summary>
    CalendarHeatmap,

    /// <summary>
    /// Renders longitude/latitude points over a dotted world map.
    /// </summary>
    DottedMap,

    /// <summary>
    /// Renders regions from a reusable tile-map definition as a cartogram.
    /// </summary>
    TileMap,

    /// <summary>
    /// Renders regions from a reusable map definition as a geographic choropleth map.
    /// </summary>
    RegionMap,

    /// <summary>
    /// Renders a single-value radial gauge.
    /// </summary>
    Gauge,

    /// <summary>
    /// Renders a single-value circular progress chart.
    /// </summary>
    Circle,

    /// <summary>
    /// Renders one or more circular progress rings.
    /// </summary>
    RadialBar,

    /// <summary>
    /// Renders one or more independently styled radial arc layers.
    /// </summary>
    LayeredRadial,

    /// <summary>
    /// Renders compact value, target, and qualitative range bars.
    /// </summary>
    Bullet,

    /// <summary>
    /// Renders cumulative positive and negative change bars.
    /// </summary>
    Waterfall,

    /// <summary>
    /// Renders values on a radial category axis.
    /// </summary>
    Radar,

    /// <summary>
    /// Renders descending stage values as centered funnel segments.
    /// </summary>
    Funnel,

    /// <summary>
    /// Renders horizontal date or numeric ranges.
    /// </summary>
    Timeline,

    /// <summary>
    /// Renders project tasks, progress, dependencies, and milestones on a schedule axis.
    /// </summary>
    Gantt,

    /// <summary>
    /// Renders weighted flows between named nodes.
    /// </summary>
    Sankey,

    /// <summary>
    /// Renders hierarchical parent-child relationships.
    /// </summary>
    Tree,

    /// <summary>
    /// Renders hierarchical parent-child relationships as radial partition rings.
    /// </summary>
    Sunburst,

    /// <summary>
    /// Renders a proportional pie chart.
    /// </summary>
    Pie,

    /// <summary>
    /// Renders a proportional donut chart.
    /// </summary>
    Donut,

    /// <summary>
    /// Renders filled area series stacked on top of earlier stacked area series.
    /// </summary>
    StackedArea,

    /// <summary>
    /// Renders a two-point comparison line between start and end values.
    /// </summary>
    Slope,

    /// <summary>
    /// Renders proportional flat rectangles for part-to-whole comparison.
    /// </summary>
    Treemap,

    /// <summary>
    /// Renders values with repeated pictorial symbols.
    /// </summary>
    Pictorial,

    /// <summary>
    /// Renders labeled horizontal progress or slider-style bars.
    /// </summary>
    ProgressBar,

    /// <summary>
    /// Renders weighted terms as a deterministic word cloud.
    /// </summary>
    WordCloud,

    /// <summary>
    /// Renders open, high, low, and close values as stems with open and close ticks.
    /// </summary>
    Ohlc,

    /// <summary>
    /// Renders equal-angle radial segments with radius scaled by value.
    /// </summary>
    PolarArea,

    /// <summary>
    /// Renders a computed least-squares trend line.
    /// </summary>
    TrendLine
}
