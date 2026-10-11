using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ColorScalePositionedNativeTests {
    private static readonly ChartColor Cold = ChartColor.FromHex("#2166AC"), Cool = ChartColor.FromHex("#67A9CF"),
        Warm = ChartColor.FromHex("#EF8A62"), Hot = ChartColor.FromHex("#B2182B"), Missing = ChartColor.FromHex("#9CA3AF");

    private static ChartColorScale Scale() => ChartColorScale.Diverging(
        new[] { new ChartColorStop(-20, Cold), new ChartColorStop(-3, Cool) }, new ChartColorStop(0, ChartColor.White),
        new[] { new ChartColorStop(5, Warm), new ChartColorStop(40, Hot) }).WithNoDataColor(Missing).WithLabels("Cold", "Zero", "Hot");

    private static Chart Create(string family, ChartColorScale scale, int width = 800, bool dark = false) {
        const string font = "CFX Positioned Native Carlito";
        FontRegistry.Register(font, Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"));
        var chart = Chart.Create().WithSize(width, 480).WithDataLabels(false).WithPngSupersampling(1).WithFontFamily(font)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        chart.Options.TransparentBackground = false; chart.Options.ShowCard = false; chart.Options.ShowPlotBackground = false; chart.Options.ShowGrid = false;
        var values = new double?[] { 0, 2.5, 5, 100, null };
        if (family is "scatter" or "bubble") {
            if (family == "bubble") chart.AddBubble("Observations", values.Select((value, index) => new ChartBubble(index + 1, 10 + index * 4, (index + 1) * (index + 1)).WithColorValue(value)));
            else chart.AddScatter("Observations", values.Select((value, index) => new ChartPoint(index + 1, 10 + index * 4).WithColorValue(value)));
            chart.WithXAxisBounds(0, 6).WithYAxisBounds(0, 40);
        } else if (family is "heatmap" or "hexbin") {
            var rows = new[] { ChartHeatmapRow.CreateMasked("Observations", values) };
            if (family == "heatmap") chart.AddHeatmapRows(rows); else chart.AddHexbinHeatmapRows(rows);
        } else if (family == "calendar") chart.AddCalendarHeatmap("Observations", values.Where(value => value.HasValue)
            .Select((value, index) => new ChartCalendarHeatmapItem(new DateTime(2026, 1, 5).AddDays(index), value!.Value)));
        else if (family is "treemap" or "sunburst") {
            var leaves = values.Select((value, index) => new ChartHierarchyItem("leaf-" + index, "Observation " + index,
                family == "sunburst" ? "root" : null, index + 1, value));
            if (family == "sunburst") chart.AddSunburst("Observations", new[] { new ChartHierarchyItem("root", "All", colorValue: 0) }.Concat(leaves));
            else chart.AddTreemap("Observations", leaves);
        } else {
            var items = new[] { new ChartRegionMapItem("CA", 0), new ChartRegionMapItem("NY", 2.5), new ChartRegionMapItem("TX", 5), new ChartRegionMapItem("WA", 100) };
            chart.WithMapLabels(false);
            if (family == "region-map") chart.AddRegionMap("Observations", ChartMapCatalog.Get("us-states"), items);
            else chart.AddTileMap("Observations", ChartTileMapCatalog.Get("us-states"), items);
        }
        SetScale(chart, family, scale);
        return chart;
    }

    private static void SetScale(Chart chart, string family, ChartColorScale scale) {
        if (family is "scatter" or "bubble") chart.Series[0].WithColorScale(scale);
        else if (family is "heatmap" or "hexbin" or "calendar") chart.WithHeatmapColorScale(scale);
        else if (family == "sunburst") chart.ConfigureSunburst(options => options.ColorScale = scale);
        else if (family == "treemap") chart.ConfigureTreemap(options => options.ColorScale = scale);
        else chart.WithMapColorScale(scale);
    }

    private static string Prefix(string family) => family switch {
        "scatter" or "bubble" => "color-scale-0", "heatmap" or "hexbin" => "heatmap-scale", "calendar" => "calendar-scale",
        "treemap" or "sunburst" => family + "-color-scale", _ => "map-scale"
    };
    private static XElement[] Role(XDocument document, string role) => document.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == role).ToArray();
    private static double Number(XElement element, string name) => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static VisualSceneRectangle[] Swatches(PreparedVisual prepared, string family) => prepared.Scene.Nodes.OfType<VisualSceneRectangle>()
        .Where(node => node.Role == (family is "region-map" or "tile-map" or "treemap" or "sunburst" ? Prefix(family) + "-step" : Prefix(family) + "-swatch")).ToArray();

    private static void AssertPixel(RgbaImage image, double x, double y, ChartColor expected) {
        var backdrop = ChartColor.FromRgba(image.Pixels[0], image.Pixels[1], image.Pixels[2], image.Pixels[3]);
        var composite = expected.A == 255 ? expected : ChartColorMath.Blend(backdrop, ChartColor.FromRgb(expected.R, expected.G, expected.B), expected.A / 255d);
        var offset = ((int)y * image.Width + (int)x) * 4;
        Assert.InRange(Math.Abs(image.Pixels[offset] - composite.R), 0, 1);
        Assert.InRange(Math.Abs(image.Pixels[offset + 1] - composite.G), 0, 1);
        Assert.InRange(Math.Abs(image.Pixels[offset + 2] - composite.B), 0, 1);
        Assert.Equal(255, image.Pixels[offset + 3]);
    }

    private static void AssertMetadata(PreparedVisual prepared, XDocument document, string family, ChartColorScale scale) {
        var prefix = Prefix(family); var source = Role(document, prefix + "-stops").Single();
        Assert.Equal("absolute", (string?)source.Attribute("data-cfx-positioning"));
        Assert.Equal(scale.Stops.Count.ToString(CultureInfo.InvariantCulture), (string?)source.Attribute("data-cfx-stop-count"));
        var stops = Role(document, prefix + "-stop-source");
        Assert.Equal(scale.Stops.Select(stop => stop.Value.ToString("R", CultureInfo.InvariantCulture)), stops.Select(stop => (string?)stop.Attribute("data-cfx-value")));
        Assert.Equal(scale.Stops.Select(stop => stop.Color.ToHexRgba()), stops.Select(stop => (string?)stop.Attribute("data-cfx-color")));
        Assert.Equal(Enumerable.Range(0, stops.Length).Select(index => index.ToString(CultureInfo.InvariantCulture)), stops.Select(stop => (string?)stop.Attribute("data-cfx-stop")));
        var pivots = stops.Where(stop => (string?)stop.Attribute("data-cfx-midpoint") == "true").ToArray();
        Assert.Equal(scale.Mode == ChartColorScaleMode.Diverging ? new[] { scale.MidpointValue!.Value.ToString("R", CultureInfo.InvariantCulture) } : Array.Empty<string>(),
            pivots.Select(stop => (string)stop.Attribute("data-cfx-value")!));
        var full = prepared.Regions.Single(region => region.Role == prefix + "-stops").Label;
        Assert.Equal(full, (string?)source.Attribute("aria-label"));
        foreach (var stop in stops) Assert.Contains((string)stop.Attribute("data-cfx-label")!, full);
    }

    private static void AssertKeyPaint(PreparedVisual prepared, XDocument document, string family, ChartColorScale scale, bool right = false) {
        var prefix = Prefix(family);
        var sourceRole = family is "region-map" or "tile-map" or "treemap" or "sunburst" ? prefix + "-step-source" : prefix + "-step";
        var values = Role(document, sourceRole).Select(node => Number(node, "data-cfx-value")).ToArray();
        var expectedValues = ChartColorScaleLegend.Steps(scale, 0, 100);
        Assert.Equal(right ? Enumerable.Reverse(expectedValues).ToArray() : expectedValues, values);
        var swatches = Swatches(prepared, family); Assert.Equal(values.Length, swatches.Length);
        var image = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        for (var index = 0; index < swatches.Length; index++) {
            var expected = scale.ColorFor(values[index]); Assert.Equal(expected, swatches[index].Fill);
            var box = swatches[index].Bounds; AssertPixel(image, box.Left + box.Width / 2, box.Top + box.Height / 2, expected);
        }
    }
}
