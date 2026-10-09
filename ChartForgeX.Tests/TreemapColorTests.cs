using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;
using static ChartForgeX.Tests.TreemapHierarchyTests;

namespace ChartForgeX.Tests;

public sealed class TreemapColorTests {
    private static readonly ChartColor Red = ChartColor.FromRgb(255, 0, 0);
    private static readonly ChartColor Blue = ChartColor.FromRgb(0, 0, 255);
    private static readonly ChartColor Missing = ChartColor.FromRgb(13, 47, 81);
    private static Chart Colored(double multiplier = 1, bool reverseColors = false) => Chart.Create().WithDataLabels(false).AddTreemap("Sizes", new[] {
        new ChartTreemapItem("a", "A", value: 9 * multiplier, colorValue: reverseColors ? 20 : 10),
        new ChartTreemapItem("b", "B", value: 1 * multiplier, colorValue: reverseColors ? 10 : 20),
        new ChartTreemapItem("missing", "Missing", value: 2 * multiplier)
    }).ConfigureTreemap(options => { options.Gap = 0; options.ColorScale = ChartColorScale.Sequential(Red, Blue).WithNoDataColor(Missing); });
    private static XElement OwnMark(XElement target) => target.Elements().Single(element => (string?)element.Attribute("data-cfx-role") is "treemap-tile-mark" or "treemap-group-mark");

    [Fact]
    public void ColorObservationsDoNotAlterSizeGeometryAndSizeDoesNotAlterColorDomain() {
        var original = Prepare(Colored(), true); var resized = Prepare(Colored(10), true); var recolored = Prepare(Colored(reverseColors: true), true);
        Assert.Equal(original.Regions.Select(region => region.Bounds), resized.Regions.Select(region => region.Bounds));
        Assert.Equal(original.Regions.Select(region => region.Bounds), recolored.Regions.Select(region => region.Bounds));
        var first = Targets(original).ToDictionary(node => (string)node.Attribute("data-cfx-target-id")!);
        var larger = Targets(resized).ToDictionary(node => (string)node.Attribute("data-cfx-target-id")!);
        var swapped = Targets(recolored).ToDictionary(node => (string)node.Attribute("data-cfx-target-id")!);
        foreach (var id in first.Keys) Assert.Equal((string?)OwnMark(first[id]).Attribute("fill"), (string?)OwnMark(larger[id]).Attribute("fill"));
        Assert.Equal(Red.ToCss(), (string?)OwnMark(first["a"]).Attribute("fill")); Assert.Equal(Blue.ToCss(), (string?)OwnMark(first["b"]).Attribute("fill"));
        Assert.Equal(Blue.ToCss(), (string?)OwnMark(swapped["a"]).Attribute("fill")); Assert.Equal(Red.ToCss(), (string?)OwnMark(swapped["b"]).Attribute("fill"));
        var scale = XDocument.Parse(original.ToSvg()).Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "treemap-color-scale");
        Assert.Equal("10", (string?)scale.Attribute("data-cfx-min-value")); Assert.Equal("20", (string?)scale.Attribute("data-cfx-max-value"));
        Assert.Equal(Missing.ToCss(), (string?)OwnMark(first["missing"]).Attribute("fill"));
        Assert.Null(first["missing"].Attribute("data-cfx-color-value")); Assert.Equal("true", (string?)first["missing"].Attribute("data-cfx-color-missing"));
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void FixedScaleAndDiscreteBandsUseTheSamePaintInMarksLegendAndNativePixels(VisualThemeMode mode) {
        foreach (var scale in new[] {
            ChartColorScale.Sequential(Red, Blue).WithValueRange(0, 100).WithNoDataColor(Missing),
            ChartColorScale.Discrete(new[] { new ChartColorBand(50, Red, "Lower"), new ChartColorBand(null, Blue, "Upper") }).WithNoDataColor(Missing)
        }) {
            var chart = Chart.Create().WithDataLabels(false).AddTreemap("Colors", new[] {
                new ChartTreemapItem("low", "Low", value: 9, colorValue: 0), new ChartTreemapItem("high", "High", value: 1, colorValue: 100), new ChartTreemapItem("missing", "Missing", value: 2)
            }).ConfigureTreemap(options => { options.Gap = 0; options.ColorScale = scale; });
            var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(720, 460)), themeMode: mode, frame: new VisualFrame(showLegend: true)));
            var xml = XDocument.Parse(prepared.ToSvg()); var nodes = Targets(prepared).ToDictionary(node => (string)node.Attribute("data-cfx-target-id")!);
            Assert.Equal(Red.ToCss(), (string?)OwnMark(nodes["low"]).Attribute("fill")); Assert.Equal(Blue.ToCss(), (string?)OwnMark(nodes["high"]).Attribute("fill"));
            var steps = xml.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "treemap-color-scale-step").ToArray();
            Assert.Equal(Red.ToCss(), (string?)steps[0].Attribute("fill")); Assert.Equal(Blue.ToCss(), (string?)steps[^1].Attribute("fill"));
            var native = PngReader.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
            Pixel(native, prepared.Regions.Single(region => region.Id == "series-0-node-low").Bounds, Red);
            Pixel(native, prepared.Regions.Single(region => region.Id == "series-0-node-high").Bounds, Blue);
            Pixel(native, prepared.Regions.Single(region => region.Id == "series-0-node-missing").Bounds, Missing);
            Pixel(native, Bounds(steps[0]), Red); Pixel(native, Bounds(steps[^1]), Blue);
            var missingSwatch = xml.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "treemap-color-scale-missing"); Pixel(native, Bounds(missingSwatch), Missing);
            if (scale.Mode == ChartColorScaleMode.Discrete) {
                Assert.Contains(prepared.Regions, region => region.Label == "Lower · < 50"); Assert.Contains(prepared.Regions, region => region.Label == "Upper · ≥ 50");
            } else {
                var legend = xml.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "treemap-color-scale");
                Assert.Equal("0", (string?)legend.Attribute("data-cfx-min-value")); Assert.Equal("100", (string?)legend.Attribute("data-cfx-max-value"));
            }
        }
    }

    [Fact]
    public void BoundaryEqualitySelectsNextBandAndPointPaintOverridesRemainExplicit() {
        var chart = Chart.Create().WithDataLabels(false).AddTreemap("Bands", new[] {
            new ChartTreemapItem("before", "Before", value: 4, colorValue: 49), new ChartTreemapItem("boundary", "Boundary", value: 2, colorValue: 50), new ChartTreemapItem("override", "Override", value: 3, colorValue: 100)
        }).ConfigureTreemap(options => options.ColorScale = ChartColorScale.Discrete(new[] { new ChartColorBand(50, Red, "Low"), new ChartColorBand(null, Blue, "High") }));
        chart.Series[0].WithPointColor(2, Missing); chart.Series[0].WithPointFillPattern(2, ChartFillPattern.Crosshatch);
        var prepared = Prepare(chart); var nodes = Targets(prepared).ToDictionary(node => (string)node.Attribute("data-cfx-target-id")!);
        Assert.Equal(Red.ToCss(), (string?)OwnMark(nodes["before"]).Attribute("fill")); Assert.Equal(Blue.ToCss(), (string?)OwnMark(nodes["boundary"]).Attribute("fill"));
        Assert.Equal(Missing.ToCss(), (string?)OwnMark(nodes["override"]).Attribute("fill"));
        Assert.Contains(nodes["override"].Descendants(), element => (string?)element.Attribute("data-cfx-role") == "treemap-pattern");
    }

    [Fact]
    public void AllMissingColorsDoNotInventAnObservedZeroDomain() {
        var chart = Chart.Create().WithDataLabels(false).AddTreemap("Unknown", new[] { new ChartTreemapItem("a", "A", value: 2) })
            .ConfigureTreemap(options => options.ColorScale = ChartColorScale.Sequential(Red, Blue).WithNoDataColor(Missing));
        var prepared = Prepare(chart, true); var xml = XDocument.Parse(prepared.ToSvg()); var node = Assert.Single(Targets(prepared));
        Assert.Equal(Missing.ToCss(), (string?)OwnMark(node).Attribute("fill")); Assert.Null(node.Attribute("data-cfx-color-value"));
        var scale = xml.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "treemap-color-scale");
        Assert.Null(scale.Attribute("data-cfx-min-value")); Assert.Null(scale.Attribute("data-cfx-max-value"));
        Assert.DoesNotContain(xml.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "treemap-color-scale-step");
        Assert.Contains(prepared.Regions, region => region.Label == "No data");
    }

    [Fact]
    public void DefaultColorDomainIncludesAuthoredGroupColorWithoutAggregatingChildColors() {
        var chart = Chart.Create().AddTreemap("Color", new[] {
            new ChartTreemapItem("group", "Group", colorValue: -10), new ChartTreemapItem("one", "One", "group", 100, 5), new ChartTreemapItem("two", "Two", "group", 300, 15)
        });
        var prepared = Prepare(chart, true); var xml = XDocument.Parse(prepared.ToSvg());
        var group = Targets(prepared).Single(node => (string?)node.Attribute("data-cfx-target-id") == "group");
        Assert.Equal(400, Number(group, "data-cfx-value")); Assert.Equal(-10, Number(group, "data-cfx-color-value"));
        var scale = xml.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "treemap-color-scale");
        Assert.Equal("-10", (string?)scale.Attribute("data-cfx-min-value")); Assert.Equal("15", (string?)scale.Attribute("data-cfx-max-value"));
    }

    [Fact]
    public void LeafLegendAndSpacingOptionsUseCurrentItemFacts() {
        var chart = Chart.Create().WithPointLegend().AddTreemap("Leaves", new[] {
            new ChartTreemapItem("group", "Group"), new ChartTreemapItem("first", "Support", "group", 1), new ChartTreemapItem("second", "Support", "group", 2)
        });
        chart.Series[0].WithPointColor(1, Red); chart.Series[0].WithPointColor(2, Blue);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(2, prepared.Scene.Nodes.Count(node => node.Role == "legend-entry"));
        var legends = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "legend-entry").ToArray();
        Assert.Equal(new[] { "first", "second" }, legends.Select(node => node.Metadata["data-cfx-legend-target-id"]));
        Assert.All(legends, node => {
            Assert.Equal("node", node.Metadata["data-cfx-legend-target-kind"]);
            Assert.Equal("Support", node.Metadata["data-cfx-label"]);
            Assert.False(node.Metadata.ContainsKey("data-cfx-point"));
        });
        Assert.DoesNotContain(prepared.Regions, region => region.Role == "legend" && region.Label == "Group");
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.Treemap.Gap = double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.Treemap.GroupPadding = -1);
        var before = Prepare(chart).Regions.Single(region => region.Id == "series-0-node-first").Bounds;
        chart.ConfigureTreemap(options => { options.GroupPadding = 0; options.Gap = 0; options.ShowGroupLabels = false; });
        var after = Prepare(chart).Regions.Single(region => region.Id == "series-0-node-first").Bounds;
        Assert.True(after.Width * after.Height > before.Width * before.Height);
        Assert.DoesNotContain(XDocument.Parse(Prepare(chart).ToSvg()).Descendants(), element => (string?)element.Attribute("data-cfx-role") == "treemap-group-label");
    }

    private static ChartRect Bounds(XElement rectangle) => new(Number(rectangle, "x"), Number(rectangle, "y"), Number(rectangle, "width"), Number(rectangle, "height"));
    private static void Pixel(RgbaImage image, ChartRect bounds, ChartColor expected) {
        var x = (int)(bounds.X + bounds.Width / 2); var y = (int)(bounds.Y + bounds.Height / 2); var offset = (y * image.Width + x) * 4;
        Assert.Equal(new[] { expected.R, expected.G, expected.B, expected.A }, image.Pixels.AsSpan(offset, 4).ToArray());
    }
}
