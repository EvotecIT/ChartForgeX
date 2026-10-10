using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;
using static ChartForgeX.Tests.SunburstHierarchyTests;

namespace ChartForgeX.Tests;

public sealed class SunburstColorTests {
    private static readonly ChartColor Red = ChartColor.FromRgb(240, 30, 50);
    private static readonly ChartColor Blue = ChartColor.FromRgb(30, 70, 240);
    private static readonly ChartColor Missing = ChartColor.FromRgb(120, 130, 140);
    private static XElement Mark(XElement node) => node.Elements().Single(element => (string?)element.Attribute("data-cfx-role") == "sunburst-segment-mark");

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void IndependentMeasurementsShareFixedAndDiscretePaintAcrossSectorsLegendAndNativePixels(VisualThemeMode mode) {
        foreach (var scale in new[] {
            ChartColorScale.Sequential(Red, Blue).WithValueRange(0, 100).WithNoDataColor(Missing),
            ChartColorScale.Discrete(new[] { new ChartColorBand(50, Red, "Lower"), new ChartColorBand(null, Blue, "Upper") }).WithNoDataColor(Missing)
        }) {
            var chart = Chart.Create().WithDataLabels(false).AddSunburst("Work", new[] {
                new ChartHierarchyItem("root", "All", colorValue: 100),
                new ChartHierarchyItem("low", "Same", "root", 9, 0),
                new ChartHierarchyItem("high", "Same", "root", 1, 100),
                new ChartHierarchyItem("missing", "Missing", "root", 2)
            }).ConfigureSunburst(options => options.ColorScale = scale);
            var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(720, 460)),
                themeMode: mode, frame: new VisualFrame(showLegend: true)));
            var nodes = Nodes(prepared); var xml = XDocument.Parse(prepared.ToSvg());
            Assert.Equal(9, Number(nodes["low"], "sweep") / Number(nodes["high"], "sweep"), 12);
            Assert.Equal(Red.ToCss(), (string?)Mark(nodes["low"]).Attribute("fill"));
            Assert.Equal(Blue.ToCss(), (string?)Mark(nodes["high"]).Attribute("fill"));
            Assert.Equal(Blue.ToCss(), (string?)Mark(nodes["root"]).Attribute("fill"));
            Assert.Equal(Missing.ToCss(), (string?)Mark(nodes["missing"]).Attribute("fill"));
            Assert.Equal("0", (string?)nodes["low"].Attribute("data-cfx-color-value"));
            Assert.Equal("true", (string?)nodes["missing"].Attribute("data-cfx-color-missing"));
            var steps = xml.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "sunburst-color-scale-step").ToArray();
            Assert.Equal(Red.ToCss(), (string?)steps[0].Attribute("fill")); Assert.Equal(Blue.ToCss(), (string?)steps[^1].Attribute("fill"));
            var rgba = PngReader.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
            SectorPixel(rgba, prepared, nodes["low"], Red); SectorPixel(rgba, prepared, nodes["high"], Blue);
            SectorPixel(rgba, prepared, nodes["root"], Blue); SectorPixel(rgba, prepared, nodes["missing"], Missing);
            foreach (var step in new[] { steps[0], steps[^1] }) RectanglePixel(rgba, step, step == steps[0] ? Red : Blue);
            RectanglePixel(rgba, xml.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "sunburst-color-scale-missing"), Missing);
        }
    }

    [Fact]
    public void InferredDomainUsesGroupAndLeafFactsWithoutInheritanceOrAggregation() {
        var chart = Chart.Create().WithDataLabels(false).AddSunburst("Colors", new[] {
            new ChartHierarchyItem("root", "All", value: 100, colorValue: -10),
            new ChartHierarchyItem("group", "Group", "root"),
            new ChartHierarchyItem("leaf", "Leaf", "group", 7, 15),
            new ChartHierarchyItem("missing", "Missing", "root", 3)
        });
        var prepared = Prepare(chart, true); var nodes = Nodes(prepared); var xml = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(10, Number(nodes["root"], "value")); Assert.Equal(-10, Number(nodes["root"], "color-value"));
        Assert.Null(nodes["group"].Attribute("data-cfx-color-value")); Assert.Equal("true", (string?)nodes["group"].Attribute("data-cfx-color-missing"));
        var scale = xml.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "sunburst-color-scale");
        Assert.Equal("-10", (string?)scale.Attribute("data-cfx-min-value")); Assert.Equal("15", (string?)scale.Attribute("data-cfx-max-value"));
        Assert.Equal((string?)Mark(nodes["group"]).Attribute("fill"), (string?)Mark(nodes["missing"]).Attribute("fill"));
    }

    [Fact]
    public void MissingOnlyScaleHasNoInventedDomainAndLegendVisibilityUsesSharedPolicy() {
        var chart = Chart.Create().AddSunburst("Unknown", new[] { new ChartHierarchyItem("one", "One", value: 2) })
            .ConfigureSunburst(options => options.ColorScale = ChartColorScale.Sequential(Red, Blue).WithNoDataColor(Missing));
        var prepared = Prepare(chart, true); var xml = XDocument.Parse(prepared.ToSvg());
        var scale = xml.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "sunburst-color-scale");
        Assert.Null(scale.Attribute("data-cfx-min-value")); Assert.Null(scale.Attribute("data-cfx-max-value"));
        Assert.DoesNotContain(xml.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "sunburst-color-scale-step");
        Assert.Contains(prepared.Regions, region => region.Label == "No data");
        chart.Options.Sunburst.ShowColorScaleLegend = false;
        Assert.DoesNotContain("data-cfx-role=\"sunburst-color-scale\"", Prepare(chart, true).ToSvg());
        chart.Options.Sunburst.ShowColorScaleLegend = true; chart.Series[0].ShowInLegend = false;
        Assert.DoesNotContain("data-cfx-role=\"sunburst-color-scale\"", Prepare(chart, true).ToSvg());
    }

    [Fact]
    public void OrdinalPaintPatternsLabelsAndStateKeepTheirMeaningAndScaleProvenance() {
        var chart = Chart.Create().WithDataLabels().AddSunburst("Styles", new[] {
            new ChartHierarchyItem("root", "All", colorValue: 0), new ChartHierarchyItem("band", "Band", "root", 5, 50), new ChartHierarchyItem("custom", "Custom", "root", 5, 100)
        }).ConfigureSunburst(options => options.ColorScale = ChartColorScale.Discrete(new[] { new ChartColorBand(50, Red), new ChartColorBand(null, Blue) }));
        chart.Series[0].WithNodeState("band", ChartSeriesState.Warning);
        chart.Series[0].WithPointColor(2, Missing).WithPointFillPattern(2, ChartFillPattern.Crosshatch).WithPointLabel(2, "Override label");
        var prepared = Prepare(chart, true); var nodes = Nodes(prepared);
        Assert.Equal(Blue.ToCss(), (string?)Mark(nodes["band"]).Attribute("fill"));
        Assert.Equal("Warning", (string?)nodes["band"].Attribute("data-cfx-state"));
        Assert.Equal("2", (string?)Mark(nodes["band"]).Attribute("stroke-width"));
        Assert.Equal(Missing.ToCss(), (string?)Mark(nodes["custom"]).Attribute("fill"));
        Assert.Contains(nodes["custom"].Descendants(), element => (string?)element.Attribute("data-cfx-role") == "sunburst-pattern");
        Assert.Equal("Override label", (string?)nodes["custom"].Attribute("data-cfx-full-label"));
        var variables = new SvgColorVariables().Add("--ramp-low", Red, SvgColorRole.Ramp).Add("--ramp-high", Blue, SvgColorRole.Ramp)
            .Add("--explicit", Missing, SvgColorRole.Series);
        var themed = NodesFromSvg(prepared.ToSvg(new VisualSvgOptions("colors", variables)));
        Assert.Contains("var(--ramp-high,", (string?)Mark(themed["band"]).Attribute("fill"));
        Assert.Contains("var(--explicit,", (string?)Mark(themed["custom"]).Attribute("fill"));
        var svg = prepared.ToSvg(); chart.Options.Sunburst.ColorScale = null; chart.Options.Sunburst.ColorLegendTitle = "Changed";
        chart.Series[0].WithNodeState("band", ChartSeriesState.None);
        Assert.Equal(svg, prepared.ToSvg());
    }

    private static Dictionary<string, XElement> NodesFromSvg(string svg) => XDocument.Parse(svg).Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-target-kind") == "node").ToDictionary(element => (string)element.Attribute("data-cfx-target-id")!);
    private static void SectorPixel(RgbaImage image, PreparedVisual prepared, XElement node, ChartColor expected) {
        var bounds = prepared.Regions.Single(region => region.Id == "series-0-node-" + (string)node.Attribute("data-cfx-target-id")!).Bounds;
        var angle = Number(node, "start-angle") + Number(node, "sweep") / 2;
        var radius = (Number(node, "inner-radius") + Number(node, "outer-radius")) / 2;
        Pixel(image, bounds.X + bounds.Width / 2 + Math.Cos(angle) * radius, bounds.Y + bounds.Height / 2 + Math.Sin(angle) * radius, expected);
    }
    private static void RectanglePixel(RgbaImage image, XElement element, ChartColor expected) {
        double Value(string name) => double.Parse(element.Attribute(name)!.Value, System.Globalization.CultureInfo.InvariantCulture);
        Pixel(image, Value("x") + Value("width") / 2, Value("y") + Value("height") / 2, expected);
    }
    private static void Pixel(RgbaImage image, double x, double y, ChartColor expected) {
        var offset = ((int)y * image.Width + (int)x) * 4;
        Assert.Equal(expected.R, image.Pixels[offset]); Assert.Equal(expected.G, image.Pixels[offset + 1]); Assert.Equal(expected.B, image.Pixels[offset + 2]);
    }
}
