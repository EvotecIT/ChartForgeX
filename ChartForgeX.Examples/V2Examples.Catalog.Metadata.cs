public static partial class V2Examples {
    private static readonly (string Id, string Label)[] CatalogGroups = {
        ("compare", "Compare values"), ("trends", "Trends and ranges"), ("distribution", "Distributions"),
        ("proportion", "Parts of a whole"), ("indicators", "Progress and targets"),
        ("matrices-maps", "Matrices and maps"), ("schedule", "Schedules"), ("relationships", "Relationships")
    };

    private static string GroupFor(string family) => family switch {
        "line" or "step-line" or "area" or "step-area" or "stacked-area" or "range-band" or "range-area" or "trend-line" or "cartesian" => "trends",
        "scatter" or "bubble" or "error-bar" or "box-plot" or "candlestick" or "ohlc" or "polar" or "radar" => "distribution",
        "pie" or "donut" or "polar-area" or "treemap" or "sunburst" or "funnel" or "pictorial" => "proportion",
        "gauge" or "circle" or "progress-ring" or "layered-radial" or "bullet" or "progress-bar" => "indicators",
        "heatmap" or "hexbin-heatmap" or "calendar-heatmap" or "dotted-map" or "tile-map" or "region-map" => "matrices-maps",
        "timeline" or "state-timeline" or "gantt" or "gantt-lane" => "schedule",
        "tree" or "sankey" or "topology" or "flow" or "sequence" => "relationships",
        _ => "compare"
    };

    private static int GroupOrder(string family) => Array.FindIndex(CatalogGroups, group => group.Id == GroupFor(family));

    private static string FamilyLabel(string family) => family switch {
        "cartesian" => "Combination chart", "chart-grid" => "Chart grid", "circle" => "Circular progress",
        "ohlc" => "OHLC", "hexbin-heatmap" => "Hexbin heatmap", "state-timeline" => "State timeline",
        _ => string.Join(" ", family.Split('-').Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1)))
    };

    private static string VariantLabel(string variant) => variant switch {
        "wide" => "Standard", "expanded" => "Detailed", "compact" => "Compact", "compact-options" => "Compact options", "feasibility" => "Simple",
        "explicit-status" => "Status colors", "explicit-series" => "Series colors", "surface" => "Filled surface",
        "wrapped-legend" => "Long legend", "long-title" => "Long title", "missing" => "Missing observations",
        "zero" => "Zero values", "empty" => "Empty data", "dense" => "Dense data", "options" => "Configured options",
        "sparse" => "Sparse data", "shapes" => "Node shapes", "above-labels" => "Outer labels", "below-labels" => "Inner labels",
        _ => FamilyLabel(variant)
    };
}
