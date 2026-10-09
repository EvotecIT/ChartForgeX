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
                var tiles = Role(root, "treemap-tile").ToDictionary(element => (string)element.Attribute("data-cfx-target-id")!);
                Require(tiles.Count == chart.Series[0].TreemapItems.Count, "Treemap source-tile count changed.");
                for (var index = 0; index < chart.Series[0].TreemapItems.Count; index++) {
                    Require(tiles.TryGetValue(chart.Series[0].TreemapItems[index].Id, out var tile), "A treemap source tile was lost.");
                    Require(Near(Parse(tile!, "data-cfx-value"), chart.Series[0].TreemapItems[index].Value!.Value)
                        && (string?)tile!.Attribute("data-cfx-label") == "Pool " + (index + 1), "Treemap source data changed."); RequireDrawing(tile!);
                }
                break;
            case "sankey":
                var links = Role(root, "sankey-link").ToDictionary(element => (string)element.Attribute("data-cfx-target-id")!);
                var expectedLinks = SankeyLinks(); Require(links.Count == expectedLinks.Length, "Sankey source-link count changed.");
                foreach (var link in expectedLinks) {
                    Require(links.TryGetValue(link.Id, out var mark), "A Sankey source link was lost.");
                    Require(Near(Parse(mark!, "data-cfx-value"), link.Value), "Sankey source value changed."); RequireDrawing(mark!);
                }
                break;
            default: throw new ArgumentOutOfRangeException(nameof(fixture));
        }
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
