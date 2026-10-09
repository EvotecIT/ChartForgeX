using ChartForgeX.Core;
using ChartForgeX.Topology;
using System.Xml.Linq;

public static partial class LegacySceneBenchmarkCases {
    private static void ValidatePhase3Marks(XElement root, string fixture) {
        if (fixture == "topology") { ValidateTopology(root, CreateTopology()); return; }
        var chart = (Chart)Create(fixture);
        switch (fixture) {
            case "matrix":
                var cells = Role(root, "heatmap-cell").ToDictionary(element => (Index(element, "data-cfx-row"), Index(element, "data-cfx-column")));
                Require(cells.Count == chart.Series.Sum(series => series.Points.Count), "Matrix source-cell count changed.");
                for (var row = 0; row < chart.Series.Count; row++) for (var column = 0; column < chart.Series[row].Points.Count; column++) {
                    Require(cells.TryGetValue((row, column), out var cell), "A matrix source cell was lost.");
                    var point = chart.Series[row].Points[column];
                    // The frozen renderer records values in accessible text rather than a numeric data attribute.
                    var expected = chart.Series[row].Name + ", Slot " + (column + 1) + ": " + Numeric(point.Y);
                    var label = (string?)cell!.Attribute("aria-label") ?? "";
                    Require(label == expected || label.StartsWith(expected + ", ", StringComparison.Ordinal), "Matrix source value changed.");
                    if (cell.Attribute("data-cfx-value") != null) Require(Near(Parse(cell, "data-cfx-value"), point.Y), "Matrix numeric metadata changed.");
                    RequireDrawing(cell);
                }
                break;
            case "calendar":
                var days = root.Descendants().Where(element => element.Attribute("data-cfx-date") != null && element.Attribute("data-cfx-value") != null
                    && !string.Equals((string?)element.Attribute("data-cfx-empty"), "true", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(element => (string)element.Attribute("data-cfx-date")!);
                Require(days.Count == chart.Series[0].Points.Count, "Calendar source-day count changed.");
                foreach (var point in chart.Series[0].Points) {
                    var date = DateTime.FromOADate(point.X).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                    Require(days.TryGetValue(date, out var day), "A calendar source day was lost.");
                    Require(Near(Parse(day!, "data-cfx-value"), point.Y), "Calendar source value changed."); RequireDrawing(day!);
                }
                break;
            case "progress":
                var rows = Role(root, "progress-fill", "progress-row").Where(element => element.Attribute("data-cfx-value") != null)
                    .ToDictionary(element => Index(element, "data-cfx-point"));
                Require(rows.Count == chart.Series[0].Points.Count, "Progress source-row count changed.");
                for (var index = 0; index < chart.Series[0].Points.Count; index++) {
                    Require(rows.TryGetValue(index, out var row), "A progress source row was lost.");
                    var value = chart.Series[0].Points[index].Y;
                    Require(Near(Parse(row!, "data-cfx-value"), value), "Progress source value changed.");
                    // A zero-width legacy fill is the correct zero-value mark; prepared rows also contain a visible track.
                    RequireDrawing(row!, allowZeroWidth: value == 0);
                }
                break;
            case "polar":
                var points = Role(root, "polar-point", "polar-point-source").Where(element => element.Attribute("data-cfx-angle") != null)
                    .ToDictionary(element => (SeriesIndex(element), Index(element, "data-cfx-point")));
                Require(points.Count == chart.Series.Sum(series => series.Points.Count), "Polar source-point count changed.");
                for (var series = 0; series < chart.Series.Count; series++) for (var index = 0; index < chart.Series[series].Points.Count; index++) {
                    Require(points.TryGetValue((series, index), out var mark), "A polar source point was lost.");
                    var point = chart.Series[series].Points[index];
                    Require(Near(Parse(mark!, "data-cfx-angle"), point.X), "Polar source angle changed.");
                    Require(Near(Parse(mark!, mark!.Attribute("data-cfx-radius") != null ? "data-cfx-radius" : "data-cfx-value"), point.Y), "Polar source radius changed."); RequireDrawing(mark!);
                }
                break;
            case "map":
                var locations = Role(root, "dotted-map-point", "dotted-map-point-source").Where(element => element.Attribute("data-cfx-longitude") != null)
                    .ToDictionary(element => Index(element, "data-cfx-point"));
                var items = MapItems(); Require(locations.Count == items.Length, "Map source-point count changed.");
                for (var index = 0; index < items.Length; index++) {
                    Require(locations.TryGetValue(index, out var mark), "A map source point was lost."); var item = items[index];
                    Require((string?)mark!.Attribute("data-cfx-label") == item.Label && Near(Parse(mark, "data-cfx-longitude"), item.Longitude)
                        && Near(Parse(mark, "data-cfx-latitude"), item.Latitude) && Near(Parse(mark, "data-cfx-value"), item.Value!.Value), "Map source data changed."); RequireDrawing(mark);
                }
                break;
            case "treemap":
#if LEGACY_CHART_API
                var tiles = Role(root, "treemap-tile").ToDictionary(element => Index(element, "data-cfx-point"));
                var treemapFacts = TreemapFacts(chart);
                Require(tiles.Count == treemapFacts.Count, "Treemap source-tile count changed.");
                for (var index = 0; index < treemapFacts.Count; index++) {
                    Require(tiles.TryGetValue(index, out var tile), "A treemap source tile was lost.");
                    Require(Near(Parse(tile!, "data-cfx-value"), treemapFacts[index].Value)
                        && (string?)tile!.Attribute("data-cfx-label") == treemapFacts[index].Label, "Treemap source data changed."); RequireDrawing(tile!);
                }
#else
                var tiles = Role(root, "treemap-tile").ToDictionary(element => (string)element.Attribute("data-cfx-target-id")!);
                Require(tiles.Count == chart.Series[0].TreemapItems.Count, "Treemap source-tile count changed.");
                for (var index = 0; index < chart.Series[0].TreemapItems.Count; index++) {
                    Require(tiles.TryGetValue(chart.Series[0].TreemapItems[index].Id, out var tile), "A treemap source tile was lost.");
                    Require(Near(Parse(tile!, "data-cfx-value"), chart.Series[0].TreemapItems[index].Value!.Value)
                        && (string?)tile!.Attribute("data-cfx-label") == chart.Series[0].TreemapItems[index].Label, "Treemap source data changed."); RequireDrawing(tile!);
                }
#endif
                break;
            case "sankey":
                ValidateSankey(root, chart);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(fixture));
        }
    }

    private static void ValidateSankey(XElement root, Chart chart) {
        var marks = Role(root, "sankey-link").ToArray();
        var series = chart.Series[0];
#if LEGACY_CHART_API
        // Frozen public exports identify endpoints by numeric node index and carry their actual labels.
        foreach (var mark in marks) Require(mark.Attribute("data-cfx-target-id") == null && mark.Attribute("data-cfx-id") == null
            && mark.Attribute("data-cfx-target-kind") == null && mark.Attribute("data-cfx-source-link-index") == null
            && mark.Attribute("data-cfx-source-point") == null && mark.Attribute("data-cfx-target-point") == null,
            "Legacy Sankey output contains mixed relationship metadata.");
        var links = marks.ToDictionary(mark => (SankeyNodeIndex(chart, Parse(mark, "data-cfx-source")), SankeyNodeIndex(chart, Parse(mark, "data-cfx-target"))));
        var facts = SankeyFacts(chart).ToArray(); Require(links.Count == facts.Length, "Sankey source-link count changed.");
        for (var index = 0; index < facts.Length; index++) {
            var endpoints = series.Points[index * 2]; var fact = facts[index];
            Require(links.TryGetValue((SankeyNodeIndex(chart, endpoints.X), SankeyNodeIndex(chart, endpoints.Y)), out var mark), "A Sankey source link was lost.");
            Require((string?)mark!.Attribute("data-cfx-source-label") == fact.Source && (string?)mark.Attribute("data-cfx-target-label") == fact.Target,
                "Sankey source endpoints changed.");
            Require(Near(Parse(mark, "data-cfx-value"), fact.Value), "Sankey source value changed."); RequireDrawing(mark);
        }
#else
        foreach (var mark in marks) Require((string?)mark.Attribute("data-cfx-target-kind") == "link"
            && mark.Attribute("data-cfx-source-point") == null && mark.Attribute("data-cfx-target-point") == null,
            "Current Sankey output contains mixed relationship metadata.");
        var links = marks.ToDictionary(mark => (string)mark.Attribute("data-cfx-target-id")!);
        var nodes = series.Nodes.ToDictionary(node => node.Id);
        Require(links.Count == series.FlowLinks.Count, "Sankey source-link count changed.");
        foreach (var link in series.FlowLinks) {
            Require(links.TryGetValue(link.Id, out var mark), "A Sankey source link was lost.");
            Require((string?)mark!.Attribute("data-cfx-id") == link.Id && (string?)mark.Attribute("data-cfx-source") == link.SourceId
                && (string?)mark.Attribute("data-cfx-target") == link.TargetId && (string?)mark.Attribute("data-cfx-source-label") == nodes[link.SourceId].Label
                && (string?)mark.Attribute("data-cfx-target-label") == nodes[link.TargetId].Label, "Sankey source endpoints changed.");
            Require(Near(Parse(mark, "data-cfx-value"), link.Value), "Sankey source value changed."); RequireDrawing(mark);
        }
#endif
    }

    private static void ValidateTopology(XElement root, TopologyChart chart) {
        var nodes = Role(root, "topology-node").ToDictionary(element => (string)element.Attribute("data-node-id")!);
        var edges = Role(root, "topology-edge").ToDictionary(element => (string)element.Attribute("data-edge-id")!);
        Require(nodes.Count == chart.Nodes.Count && edges.Count == chart.Edges.Count, "Topology source counts changed.");
        foreach (var node in chart.Nodes) {
            Require(nodes.TryGetValue(node.Id, out var mark), "A topology source node was lost.");
            Require((string?)mark!.Attribute("data-node-label") == node.Label && (string?)mark.Attribute("data-cfx-status") == node.Status.ToString(), "Topology node data changed."); RequireDrawing(mark);
        }
        foreach (var edge in chart.Edges) {
            Require(edges.TryGetValue(edge.Id, out var mark), "A topology source edge was lost.");
            Require((string?)mark!.Attribute("data-source-node-id") == edge.SourceNodeId && (string?)mark.Attribute("data-target-node-id") == edge.TargetNodeId
                && (string?)mark.Attribute("data-edge-label") == edge.Label, "Topology edge data changed."); RequireDrawing(mark);
        }
    }

    private static IEnumerable<XElement> Role(XElement root, params string[] roles) => root.Descendants().Where(element => roles.Contains((string?)element.Attribute("data-cfx-role") ?? ""));
    private static int Index(XElement element, string attribute) => (int)element.Attribute(attribute)!;
    private static int SeriesIndex(XElement element) => (int)element.AncestorsAndSelf().First(parent => parent.Attribute("data-cfx-series") != null).Attribute("data-cfx-series")!;
    private static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void RequireDrawing(XElement element, bool allowZeroWidth = false) => Require(element.DescendantsAndSelf().Any(mark => mark.Name.LocalName switch {
        "path" => !string.IsNullOrWhiteSpace((string?)mark.Attribute("d")),
        "circle" => (double?)mark.Attribute("r") > 0,
        "ellipse" => (double?)mark.Attribute("rx") > 0 && (double?)mark.Attribute("ry") > 0,
        "rect" => ((double?)mark.Attribute("width") > 0 || allowZeroWidth && (double?)mark.Attribute("width") == 0) && (double?)mark.Attribute("height") > 0,
        "line" => (double?)mark.Attribute("x1") != (double?)mark.Attribute("x2") || (double?)mark.Attribute("y1") != (double?)mark.Attribute("y2"),
        "polyline" or "polygon" => !string.IsNullOrWhiteSpace((string?)mark.Attribute("points")), _ => false
    }), "A source mark has no drawing command.");
}
