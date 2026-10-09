using System.Collections;
using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TreemapHierarchyTests {
    internal static ChartTreemapItem[] ForestItems() => new[] {
        new ChartTreemapItem("north", "North"), new ChartTreemapItem("north-team", "Team", "north"),
        new ChartTreemapItem("north-support", "Support", "north-team", 5, -2), new ChartTreemapItem("north-dev", "Development", "north-team", 10, 3),
        new ChartTreemapItem("south", "South"), new ChartTreemapItem("south-team", "Team", "south"),
        new ChartTreemapItem("south-support", "Support", "south-team", 8, 8), new ChartTreemapItem("reserve", "Reserve", "south-team", 0),
        new ChartTreemapItem("research", "Research", value: 3)
    };
    internal static Chart Forest() => Chart.Create().AddTreemap("Allocation", ForestItems());
    internal static PreparedVisual Prepare(Chart chart, bool legend = false) => chart.Prepare(new VisualRenderContext(
        new VisualLayoutOptions(new VisualSize(720, 460)), frame: new VisualFrame(showLegend: legend)));
    internal static XElement[] Targets(PreparedVisual prepared) => XDocument.Parse(prepared.ToSvg()).Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-target-kind") == "node").ToArray();
    internal static double Number(XElement element, string name) => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);

    [Fact]
    public void ThreeLevelsAndRepeatedLabelsRetainTruthfulForestFactsAndContainment() {
        var chart = Forest(); var series = chart.Series[0]; var prepared = Prepare(chart);
        var nodes = Targets(prepared).ToDictionary(element => (string)element.Attribute("data-cfx-target-id")!);
        Assert.Equal(9, series.TreemapItems.Count); Assert.Equal(9, series.Nodes.Count);
        Assert.Empty(series.Points); Assert.Empty(series.TreeLinks); Assert.Equal(0, series.SourcePointCount);
        Assert.Equal(15, Number(nodes["north"], "data-cfx-value")); Assert.Equal(8, Number(nodes["south"], "data-cfx-value"));
        Assert.Equal(2, Number(nodes["south-support"], "data-cfx-depth"));
        Assert.Equal("Support", (string?)nodes["north-support"].Attribute("data-cfx-label"));
        Assert.Equal("Support", (string?)nodes["south-support"].Attribute("data-cfx-label"));
        Assert.Equal(5, Number(nodes["north-support"], "data-cfx-authored-value"));
        Assert.Null(nodes["north"].Attribute("data-cfx-authored-value"));
        Assert.Equal(-2, Number(nodes["north-support"], "data-cfx-color-value"));
        Assert.Null(nodes["reserve"].Attribute("data-cfx-color-value"));
        Assert.All(nodes.Values, node => {
            Assert.Equal("0", (string?)node.Attribute("data-cfx-series")); Assert.Equal("Allocation", (string?)node.Attribute("data-cfx-series-key"));
            Assert.Null(node.Attribute("data-cfx-point")); Assert.Null(node.Attribute("data-cfx-source-point"));
        });
        // This checks rendered rectangles independently of the hierarchy layout helper.
        var bounds = prepared.Regions.Where(region => region.Role is "treemap-tile" or "treemap-group").ToDictionary(region => region.Id);
        foreach (var item in series.TreemapItems.Where(item => item.ParentId != null && (item.Value ?? 1) > 0)) {
            var child = bounds["series-0-node-" + item.Id].Bounds; var parent = bounds["series-0-node-" + item.ParentId].Bounds;
            Assert.True(child.Left > parent.Left && child.Right < parent.Right && child.Top > parent.Top && child.Bottom < parent.Bottom);
            Assert.Contains(nodes[item.Id].Ancestors(), ancestor => (string?)ancestor.Attribute("data-cfx-target-id") == item.ParentId);
        }
        foreach (var siblings in series.TreemapItems.Where(item => !item.Value.HasValue || item.Value > 0).GroupBy(item => item.ParentId)) {
            var rectangles = siblings.Select(item => bounds["series-0-node-" + item.Id].Bounds).ToArray();
            for (var i = 0; i < rectangles.Length; i++) for (var j = i + 1; j < rectangles.Length; j++) Assert.False(Overlap(rectangles[i], rectangles[j]));
        }
        Assert.Equal(4, XDocument.Parse(prepared.ToSvg()).Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "treemap-group-label"));
        ChartDescriptionFacts? facts = null; chart.Options.Labels.AccessibleTextFormatter = value => { facts = value; return null; };
        _ = Prepare(chart); Assert.Equal(new[] { "Allocation" }, facts!.SeriesNames);
    }

    [Theory]
    [InlineData(5e-8, 5e-7)]
    [InlineData(1e-310, 2e-310)]
    [InlineData(1e308, 3e307)]
    public void LeafSizesPreserveRatiosAcrossTinyAndLargeFiniteRanges(double first, double second) {
        var chart = Chart.Create().AddTreemap("Sizes", new[] {
            new ChartTreemapItem("group", "Group"), new ChartTreemapItem("first", "First", "group", first), new ChartTreemapItem("second", "Second", "group", second)
        }).WithDataLabels(false).ConfigureTreemap(options => { options.Gap = 0; options.GroupPadding = 0; options.ShowGroupLabels = false; });
        var prepared = Prepare(chart); var nodes = Targets(prepared).ToDictionary(element => (string)element.Attribute("data-cfx-target-id")!);
        Assert.Equal(first, Number(nodes["first"], "data-cfx-value")); Assert.Equal(second, Number(nodes["second"], "data-cfx-authored-value"));
        Assert.Equal(first + second, Number(nodes["group"], "data-cfx-value"));
        var a = prepared.Regions.Single(region => region.Id == "series-0-node-first").Bounds;
        var b = prepared.Regions.Single(region => region.Id == "series-0-node-second").Bounds;
        Assert.Equal(first / second, a.Width * a.Height / (b.Width * b.Height), 10);
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Fact]
    public void SingletonAndZeroForestsKeepSourcesWithoutFabricatedPositiveArea() {
        var single = Prepare(Chart.Create().AddTreemap("One", new[] { new ChartTreemapItem("one", "One", value: 7) }));
        Assert.Single(Targets(single)); Assert.Single(single.Regions, region => region.Role == "treemap-tile");
        var zero = Prepare(Chart.Create().AddTreemap("Zero", new[] {
            new ChartTreemapItem("group", "Group"), new ChartTreemapItem("leaf", "Leaf", "group", 0), new ChartTreemapItem("standalone", "Standalone", value: 0)
        }));
        Assert.Equal(3, Targets(zero).Length);
        Assert.All(Targets(zero), node => Assert.Equal(0, Number(node, "data-cfx-value")));
        Assert.DoesNotContain(zero.Scene.Nodes.OfType<VisualSceneRectangle>(), rectangle => rectangle.Role is "treemap-tile-mark" or "treemap-group-mark");
        Assert.All(zero.Regions, region => { Assert.Equal(0, region.Bounds.Width); Assert.Equal(0, region.Bounds.Height); });
        Assert.Contains(zero.Diagnostics, diagnostic => diagnostic.Code == "hierarchy.no-data");
    }

    [Fact]
    public void InvalidItemsReferencesCyclesAndAggregatesAreAtomic() {
        var chart = Chart.Create().AddLine("Existing", new[] { new ChartPoint(1, 2) }); var existing = chart.Series[0];
        var invalid = new[] {
            Array.Empty<ChartTreemapItem>(), new[] { default(ChartTreemapItem) },
            new[] { new ChartTreemapItem("same", "First", value: 1), new ChartTreemapItem("same", "Second", value: 2) },
            new[] { new ChartTreemapItem("leaf", "Leaf") },
            new[] { new ChartTreemapItem("leaf", "Leaf", "missing", 1) },
            new[] { new ChartTreemapItem("self", "Self", "self", 1) },
            new[] { new ChartTreemapItem("a", "A", "b"), new ChartTreemapItem("b", "B", "a") },
            new[] { new ChartTreemapItem("group", "Group", value: 1), new ChartTreemapItem("leaf", "Leaf", "group", 2) },
            new[] { new ChartTreemapItem("group", "Group"), new ChartTreemapItem("a", "A", "group", double.MaxValue), new ChartTreemapItem("b", "B", "group", double.MaxValue) },
            new[] { new ChartTreemapItem("a", "A", value: double.MaxValue), new ChartTreemapItem("b", "B", value: double.MaxValue) }
        };
        foreach (var items in invalid) {
            Assert.Throws<ArgumentException>(() => chart.AddTreemap("Invalid", items));
            Assert.Same(existing, Assert.Single(chart.Series)); Assert.Equal(2, existing.Points[0].Y);
        }
        Assert.Throws<ArgumentException>(() => new ChartTreemapItem(" ", "Label", value: 1));
        Assert.Throws<ArgumentException>(() => new ChartTreemapItem("id", " ", value: 1));
        Assert.Throws<ArgumentException>(() => new ChartTreemapItem("id", "Label", " ", 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartTreemapItem("id", "Label", value: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartTreemapItem("id", "Label", value: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartTreemapItem("id", "Label", value: 1, colorValue: double.PositiveInfinity));
    }

    [Fact]
    public void SourceSnapshotsAndPreparedExportsSurviveInputAndOptionChanges() {
        var items = ForestItems().ToList(); var chart = Chart.Create().AddTreemap("Allocation", items);
        var prepared = Prepare(chart, true); var svg = prepared.ToSvg(); var png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        items.Clear(); Assert.Equal(9, chart.Series[0].TreemapItems.Count);
        Assert.Throws<NotSupportedException>(() => ((IList)chart.Series[0].TreemapItems).Clear());
        Assert.Equal(svg, Prepare(chart, true).ToSvg());
        chart.Options.Treemap.Gap = 40; chart.Options.Treemap.ShowGroupLabels = false; chart.Options.Treemap.ColorScale = ChartColorScale.Sequential(ChartColor.Black, ChartColor.White);
        chart.Series[0].WithPointColor(2, ChartColor.Black); chart.Series[0].DataLabelStyle.FontSize = 40;
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Fact]
    public void ReorderingAndRenamingKeepNodeIdentityWhileSourceOrdinalsFollowInput() {
        var original = Targets(Prepare(Forest())).ToDictionary(node => (string)node.Attribute("data-cfx-target-id")!);
        var reordered = ForestItems().AsEnumerable().Reverse().Select(item => new ChartTreemapItem(item.Id, item.Id == "south-support" ? "Customer care" : item.Label, item.ParentId, item.Value, item.ColorValue));
        var nodes = Targets(Prepare(Chart.Create().AddTreemap("Allocation", reordered)));
        foreach (var node in nodes) {
            var id = (string)node.Attribute("data-cfx-target-id")!;
            Assert.Equal((string?)original[id].Attribute("data-cfx-source-id"), (string?)node.Attribute("data-cfx-source-id"));
            Assert.Equal((string?)original[id].Attribute("data-cfx-value"), (string?)node.Attribute("data-cfx-value"));
        }
        var support = nodes.Single(node => (string?)node.Attribute("data-cfx-target-id") == "south-support");
        Assert.Equal("Customer care", (string?)support.Attribute("data-cfx-label")); Assert.Equal("2", (string?)support.Attribute("data-cfx-source-node-index"));
    }

    [Fact]
    public void ForestDepthSharesTheExistingBoundedHierarchyPolicy() {
        var items = Enumerable.Range(0, 513).Select(i => new ChartTreemapItem("node-" + i, "Node", i == 0 ? null : "node-" + (i - 1), i == 512 ? 1 : null)).ToArray();
        Assert.Equal(513, Chart.Create().AddTreemap("Bounded", items).Series[0].TreemapItems.Count);
        var deeper = items.Take(512).Append(new ChartTreemapItem("node-512", "Node", "node-511")).Append(new ChartTreemapItem("node-513", "Leaf", "node-512", 1));
        Assert.Throws<ArgumentException>(() => Chart.Create().AddTreemap("Too deep", deeper));
    }

    private static bool Overlap(ChartRect a, ChartRect b) => a.Left < b.Right && b.Left < a.Right && a.Top < b.Bottom && b.Top < a.Bottom;
}
