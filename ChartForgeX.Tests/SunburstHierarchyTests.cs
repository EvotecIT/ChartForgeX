using System.Collections;
using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SunburstHierarchyTests {
    internal static PreparedVisual Prepare(Chart chart, bool legend = false) => chart.Prepare(new VisualRenderContext(
        new VisualLayoutOptions(new VisualSize(720, 460)), frame: new VisualFrame(showLegend: legend)));
    internal static Dictionary<string, XElement> Nodes(PreparedVisual prepared) => XDocument.Parse(prepared.ToSvg()).Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-target-kind") == "node")
        .ToDictionary(element => (string)element.Attribute("data-cfx-target-id")!, StringComparer.Ordinal);
    internal static double Number(XElement element, string key) => double.Parse(element.Attribute("data-cfx-" + key)!.Value, CultureInfo.InvariantCulture);

    [Fact]
    public void LeafAggregatePreservesProvidedGroupSizesWithoutUsingThemForAngles() {
        var items = new[] {
            new ChartHierarchyItem("root", "All", value: 2),
            new ChartHierarchyItem("group", "Support", "root", 100),
            new ChartHierarchyItem("leaf", "Support", "group", 3),
            new ChartHierarchyItem("other", "Support", "root", 2)
        };
        var chart = Chart.Create().AddSunburst("Work", items);
        Assert.Equal(ChartHierarchyValuePolicy.LeafAggregate, chart.Options.Sunburst.ParentValuePolicy);
        Assert.Equal(items, chart.Series[0].HierarchyItems);
        Assert.Empty(chart.Series[0].TreeLinks);
        var nodes = Nodes(Prepare(chart));
        Assert.Equal(5, Number(nodes["root"], "value")); Assert.Equal(2, Number(nodes["root"], "authored-value"));
        Assert.Equal(3, Number(nodes["group"], "value")); Assert.Equal(100, Number(nodes["group"], "authored-value"));
        Assert.Equal(0, Number(nodes["group"], "remainder-value"));
        Assert.Equal(1.5, Number(nodes["group"], "sweep") / Number(nodes["other"], "sweep"), 12);
    }

    [Fact]
    public void InclusiveTotalsLeaveActualRemainderAndNullGroupsDeriveResolvedChildren() {
        var chart = Chart.Create().ConfigureSunburst(options => options.ParentValuePolicy = ChartHierarchyValuePolicy.AuthoredTotal)
            .AddSunburst("Budget", new[] {
                new ChartHierarchyItem("root", "All", value: 20),
                new ChartHierarchyItem("provided", "Provided", "root", 10),
                new ChartHierarchyItem("derived", "Derived", "root"),
                new ChartHierarchyItem("inside", "Inside", "provided", 6),
                new ChartHierarchyItem("second", "Second", "derived", 4),
                new ChartHierarchyItem("zero", "Zero", "provided", 0)
            });
        var prepared = Prepare(chart); var nodes = Nodes(prepared);
        Assert.Equal(20, Number(nodes["root"], "value")); Assert.Equal(6, Number(nodes["root"], "remainder-value"));
        Assert.Equal(10, Number(nodes["provided"], "value")); Assert.Equal(4, Number(nodes["provided"], "remainder-value"));
        Assert.Equal(4, Number(nodes["derived"], "value")); Assert.Null(nodes["derived"].Attribute("data-cfx-authored-value"));
        Assert.Equal(Math.PI, Number(nodes["provided"], "sweep"), 12);
        Assert.Equal(.6, Number(nodes["inside"], "sweep") / Number(nodes["provided"], "sweep"), 12);
        var end = Number(nodes["derived"], "start-angle") + Number(nodes["derived"], "sweep");
        Assert.Equal(.3 * Math.PI * 2, Number(nodes["root"], "start-angle") + Number(nodes["root"], "sweep") - end, 12);
        Assert.Empty(nodes["zero"].Elements()); Assert.Equal("zero", (string?)nodes["zero"].Attribute("data-cfx-geometry-status"));
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        var rootBounds = prepared.Regions.Single(region => region.Id == "series-0-node-root").Bounds;
        var centerX = rootBounds.X + rootBounds.Width / 2; var centerY = rootBounds.Y + rootBounds.Height / 2;
        var gapAngle = (end + Number(nodes["root"], "start-angle") + Number(nodes["root"], "sweep")) / 2;
        var radius = (Number(nodes["root"], "outer-radius") + Number(nodes["provided"], "outer-radius")) / 2;
        var gapPixel = ((int)(centerY + Math.Sin(gapAngle) * radius) * image.Width + (int)(centerX + Math.Cos(gapAngle) * radius)) * 4;
        var backgroundPixel = ((int)centerY * image.Width + (int)(centerX - Number(nodes["inside"], "outer-radius") * 1.1)) * 4;
        Assert.Equal(image.Pixels.AsSpan(backgroundPixel, 4).ToArray(), image.Pixels.AsSpan(gapPixel, 4).ToArray());
    }

    [Theory]
    [InlineData(.3, .1, .2)]
    [InlineData(3e-300, 1e-300, 2e-300)]
    [InlineData(3e300, 1e300, 2e300)]
    public void RepresentationalOverrunClosesOnlyTheFinalPositiveChild(double parent, double first, double last) {
        var chart = Chart.Create().ConfigureSunburst(options => options.ParentValuePolicy = ChartHierarchyValuePolicy.AuthoredTotal)
            .AddSunburst("Fractions", new[] {
                new ChartHierarchyItem("root", "All", value: parent),
                new ChartHierarchyItem("first", "First", "root", first),
                new ChartHierarchyItem("last", "Last", "root", last),
                new ChartHierarchyItem("zero", "Zero", "root", 0)
            });
        var nodes = Nodes(Prepare(chart));
        Assert.Equal(parent, Number(nodes["root"], "value"));
        Assert.Equal(Math.PI * 2, Number(nodes["first"], "sweep") + Number(nodes["last"], "sweep"), 12);
        Assert.Equal(Number(nodes["last"], "start-angle") + Number(nodes["last"], "sweep"), Number(nodes["zero"], "start-angle"));
        Assert.InRange(Number(nodes["root"], "remainder-value"), 0, parent * 1e-14);
    }

    [Fact]
    public void APositiveRepresentableRemainderIsRetainedInsteadOfNormalizedAway() {
        var parent = Math.BitIncrement(1d);
        var chart = Chart.Create().ConfigureSunburst(options => options.ParentValuePolicy = ChartHierarchyValuePolicy.AuthoredTotal)
            .AddSunburst("Remainder", new[] {
                new ChartHierarchyItem("root", "All", value: parent), new ChartHierarchyItem("one", "One", "root", .5), new ChartHierarchyItem("two", "Two", "root", .5)
            });
        var nodes = Nodes(Prepare(chart));
        Assert.Equal(parent - 1, Number(nodes["root"], "remainder-value"));
        Assert.True(Number(nodes["one"], "sweep") + Number(nodes["two"], "sweep") < Number(nodes["root"], "sweep"));
    }

    [Fact]
    public void SingletonZeroAndCollapsedPositiveSourcesDoNotInventPaintedSectors() {
        var single = Prepare(Chart.Create().AddSunburst("Single", new[] { new ChartHierarchyItem("only", "Only", value: 7) }));
        var node = Assert.Single(Nodes(single)).Value;
        Assert.Equal(Math.PI * 2, Number(node, "sweep")); Assert.Single(node.Elements());
        var zero = Prepare(Chart.Create().AddSunburst("Zero", new[] {
            new ChartHierarchyItem("root", "All"), new ChartHierarchyItem("child", "Child", "root", 0)
        }));
        Assert.All(Nodes(zero).Values, item => { Assert.Empty(item.Elements()); Assert.Equal(0, Number(item, "sweep")); });
        Assert.All(zero.Regions, region => Assert.Equal(0, region.Bounds.Width));
        Assert.Contains(zero.Diagnostics, diagnostic => diagnostic.Code == "hierarchy.no-data");
        var collapsed = Prepare(Chart.Create().AddSunburst("Precision", new[] {
            new ChartHierarchyItem("root", "All"), new ChartHierarchyItem("large", "Large", "root", 1), new ChartHierarchyItem("tiny", "Tiny", "root", double.Epsilon)
        }));
        var tiny = Nodes(collapsed)["tiny"];
        Assert.Equal(double.Epsilon, Number(tiny, "value")); Assert.Empty(tiny.Elements());
        Assert.Equal("precision-collapse", (string?)tiny.Attribute("data-cfx-geometry-status"));
        Assert.NotEmpty(collapsed.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Fact]
    public void DeepCompactRingsRemainAdjacentWithoutInventedMinimumThickness() {
        var items = Enumerable.Range(0, 64).Select(index => new ChartHierarchyItem("level-" + index, "Level " + index,
            index == 0 ? null : "level-" + (index - 1), index == 63 ? 1 : null));
        var chart = Chart.Create().WithDataLabels(false).AddSunburst("Nested", items);
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(160, 128)), frame: new VisualFrame(showLegend: false)));
        var nodes = Nodes(prepared);
        for (var index = 1; index < 64; index++)
            Assert.Equal(Number(nodes["level-" + (index - 1)], "outer-radius"), Number(nodes["level-" + index], "inner-radius"));
        Assert.True(Number(nodes["level-0"], "outer-radius") < 1);
    }

    [Fact]
    public void SnapshotsDetachPolicyColorLabelsAndAuthoredCollectionsAndFailedPreparationIsAtomic() {
        var items = new List<ChartHierarchyItem> { new("root", "All", value: 1), new("first", "First", "root", 2), new("last", "Last", "root", 3) };
        var chart = Chart.Create().AddSunburst("Work", items);
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        items.Clear();
        Assert.Throws<NotSupportedException>(() => ((IList)chart.Series[0].HierarchyItems).Clear());
        chart.Options.Sunburst.ParentValuePolicy = ChartHierarchyValuePolicy.AuthoredTotal;
        Assert.Throws<ArgumentException>(() => Prepare(chart));
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        chart.Options.Sunburst.ParentValuePolicy = ChartHierarchyValuePolicy.LeafAggregate;
        Assert.Equal(svg, Prepare(chart).ToSvg()); Assert.Equal(1, chart.Series[0].HierarchyItems[0].Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.Sunburst.ParentValuePolicy = (ChartHierarchyValuePolicy)100);
    }

    [Fact]
    public void ItemOrderControlsSiblingsAndExplicitIdsSurviveRepeatedLabels() {
        var chart = Chart.Create().AddSunburst("Order", new[] {
            new ChartHierarchyItem("last", "Same", "root", 3), new ChartHierarchyItem("root", "Same"), new ChartHierarchyItem("first", "Same", "root", 2)
        });
        var nodes = Nodes(Prepare(chart));
        Assert.Equal(Number(nodes["root"], "start-angle"), Number(nodes["last"], "start-angle"));
        Assert.Equal(Number(nodes["last"], "start-angle") + Number(nodes["last"], "sweep"), Number(nodes["first"], "start-angle"), 12);
        Assert.Equal(new[] { "last", "root", "first" }, chart.Series[0].HierarchyItems.Select(item => item.Id));
    }

    [Fact]
    public void InvalidFactsAndImpossibleAuthoredTotalsCannotAppendPartialSeries() {
        var invalid = new[] {
            Array.Empty<ChartHierarchyItem>(), new[] { default(ChartHierarchyItem) },
            new[] { new ChartHierarchyItem("a", "A") },
            new[] { new ChartHierarchyItem("a", "A", value: 1), new ChartHierarchyItem("a", "A", value: 2) },
            new[] { new ChartHierarchyItem("a", "A", "missing", 1) },
            new[] { new ChartHierarchyItem("a", "A", "a", 1) },
            new[] { new ChartHierarchyItem("a", "A", "b"), new ChartHierarchyItem("b", "B", "a") },
            new[] { new ChartHierarchyItem("a", "A", value: 1), new ChartHierarchyItem("b", "B", value: 1) },
            new[] { new ChartHierarchyItem("root", "All", value: .3), new ChartHierarchyItem("leaf", "Leaf", "root", .30000001) },
            new[] { new ChartHierarchyItem("root", "All", value: 0), new ChartHierarchyItem("leaf", "Leaf", "root", double.Epsilon) },
            new[] { new ChartHierarchyItem("root", "All", value: double.Epsilon), new ChartHierarchyItem("leaf", "Leaf", "root", double.Epsilon * 2) },
            new[] { new ChartHierarchyItem("root", "All"), new ChartHierarchyItem("one", "One", "root", double.MaxValue), new ChartHierarchyItem("two", "Two", "root", double.MaxValue) }
        };
        foreach (var items in invalid) {
            var chart = Chart.Create().ConfigureSunburst(options => options.ParentValuePolicy = ChartHierarchyValuePolicy.AuthoredTotal);
            Assert.Throws<ArgumentException>(() => chart.AddSunburst("Invalid", items)); Assert.Empty(chart.Series);
        }
        var treemap = new[] { new ChartHierarchyItem("group", "Group", value: 10), new ChartHierarchyItem("leaf", "Leaf", "group", 2) };
        Assert.Throws<ArgumentException>(() => Chart.Create().AddTreemap("Still aggregated", treemap));
        Assert.Null(Chart.Create().Options.Sunburst.ColorScale); Assert.True(Chart.Create().Options.Sunburst.ShowColorScaleLegend);
    }
}
