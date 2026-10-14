using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class DottedMapPointLegendTests {
    [Theory]
    [InlineData(false, 380)]
    [InlineData(false, 800)]
    [InlineData(true, 380)]
    [InlineData(true, 800)]
    public void TypedLocationsProduceNativePointKeysWithMatchingPaintAndSourceFacts(bool dark, int width) {
        var chart = Create(dark, width: width);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var xml = XDocument.Parse(prepared.ToSvg());
        var entries = Entries(xml);
        Assert.Equal(new[] { "North", "South", "Offscreen" }, entries.Select(entry =>
            entry.Descendants().Single(node => Role(node) == "legend-label").Value));
        for (var index = 0; index < entries.Length; index++) {
            var entry = entries[index];
            var observation = chart.Series[0].MapPoints[index];
            Assert.Equal("legend-series-0-point-" + index, (string?)entry.Attribute("data-cfx-source-id"));
            Assert.Equal("locations", (string?)entry.Attribute("data-cfx-series-key"));
            Assert.Equal(chart.Series[0].ObservationIds[index].Value, (string?)entry.Attribute("data-cfx-observation-id"));
            Assert.Equal(index.ToString(), (string?)entry.Attribute("data-cfx-source-point"));
            Assert.Equal(observation.Longitude.ToString("R", System.Globalization.CultureInfo.InvariantCulture), (string?)entry.Attribute("data-cfx-longitude"));
            Assert.Equal(observation.Value!.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture), (string?)entry.Attribute("data-cfx-value"));
            var markGroup = xml.Descendants().Single(node => Role(node) == "dotted-map-point-source"
                && (string?)node.Attribute("data-cfx-point") == index.ToString());
            if (index < 2) {
                var dot = markGroup.Descendants().Single(node => Role(node) == "dotted-map-point");
                var swatch = entry.Descendants().Single(node => Role(node) == "legend-swatch");
                Assert.Equal((string?)dot.Attribute("fill"), (string?)swatch.Attribute("fill"));
            } else Assert.Equal("false", (string?)markGroup.Attribute("data-cfx-visible"));
            var region = Assert.Single(prepared.Regions, region => region.Id == "legend-series-0-point-" + index);
            Assert.InRange(region.Bounds.Left, 0, width);
            Assert.InRange(region.Bounds.Right, 0, width);
        }
        var image = ChartForgeX.Raster.RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        var glyphs = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "legend-swatch").ToArray();
        Assert.Equal(3, glyphs.Length);
        var expected = new[] { chart.Series[0].PointColors[0]!.Value, chart.Options.Theme.Resolve(chart.Options.ThemeMode).Status.Pass.Fill };
        for (var index = 0; index < expected.Length; index++) {
            var pixel = ((int)glyphs[index].Cy * image.Width + (int)glyphs[index].Cx) * 4;
            Assert.Equal(expected[index], ChartColor.FromRgba(image.Pixels[pixel], image.Pixels[pixel + 1], image.Pixels[pixel + 2], image.Pixels[pixel + 3]));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PointKeysKeepExplicitAndStatusSvgPaintRoles(bool dark) {
        var chart = Create(dark);
        var context = VisualExportRequest.ForChart(chart).Context;
        var variables = new SvgColorVariables()
            .Add("--map-explicit", chart.Series[0].PointColors[0]!.Value, SvgColorRole.Series)
            .Add("--map-status", context.Theme.Resolve(context.ThemeMode).Status.Pass.Fill, SvgColorRole.Status);
        var xml = XDocument.Parse(chart.Prepare(context).ToSvg(new VisualSvgOptions("map-points", variables)));
        Assert.Contains("var(--map-explicit,", (string?)Entries(xml)[0].Descendants().Single(node => Role(node) == "legend-swatch").Attribute("fill"));
        Assert.Contains("var(--map-status,", (string?)Entries(xml)[1].Descendants().Single(node => Role(node) == "legend-swatch").Attribute("fill"));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void SeriesKeyUsesItsDefaultDotPaintWithoutBorrowingAnObservationOverride(bool dark, bool explicitColor) {
        var chart = Create(dark).WithPointLegend(false).WithLegend();
        if (explicitColor) chart.Series[0].Color = ChartColor.FromRgb(17, 53, 97);
        var context = VisualExportRequest.ForChart(chart).Context;
        var expected = chart.Series[0].Color ?? context.Theme.Resolve(context.ThemeMode).Status.Pass.Fill;
        var role = explicitColor ? SvgColorRole.Series : SvgColorRole.Status;
        var prepared = chart.Prepare(context);
        var xml = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions("map-series",
            new SvgColorVariables().Add("--map-default", expected, role))));
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            var name = Path.Combine(captures, "series-key-" + (dark ? "dark" : "light") + (explicitColor ? "-explicit" : "-default"));
            File.WriteAllText(name + ".svg", prepared.ToSvg());
            File.WriteAllBytes(name + ".png", prepared.ToPng());
        }
        var swatch = Assert.Single(Entries(xml)).Descendants().Single(node => Role(node) == "legend-swatch");
        var ordinary = xml.Descendants().Single(node => Role(node) == "dotted-map-point-source"
            && (string?)node.Attribute("data-cfx-observation-id") == "south")
            .Descendants().Single(node => Role(node) == "dotted-map-point");
        Assert.Contains("var(--map-default,", (string?)swatch.Attribute("fill"));
        Assert.Equal((string?)ordinary.Attribute("fill"), (string?)swatch.Attribute("fill"));
    }

    [Fact]
    public void ExplicitSingletonAndEmptyRetainedMapsRespectLegendVisibility() {
        var series = ChartSeries.CreateDottedMap(new ChartSeriesId("locations"), "Locations", new[] { new ChartMapPoint("North", 21, 52) });
        var chart = Chart.Create().WithPointLegend().WithLegend().AddSeries(series);
        Assert.Equal("North", Assert.Single(Entries(XDocument.Parse(chart.ToSvg()))).Descendants().Single(node => Role(node) == "legend-label").Value);
        chart.WithLegend(false);
        Assert.Empty(Entries(XDocument.Parse(chart.ToSvg())));
        chart.WithLegend();
        series.ReplaceMapPoints(Array.Empty<ChartMapPoint>());
        Assert.Empty(Entries(XDocument.Parse(chart.ToSvg())));
    }

    internal static Chart Create(bool dark, bool reversed = false, bool removeNorth = false, int width = 760) {
        var input = new[] {
            new ChartMapPoint("North", 21, 52, 7, ChartColor.FromRgb(125, 85, 210)),
            new ChartMapPoint("South", -3.7, 40.4, 3),
            new ChartMapPoint("Offscreen", 151.2, -33.9, 12, ChartColor.FromRgba(25, 105, 190, 180))
        };
        var ordered = (reversed ? Enumerable.Reverse(input) : input.AsEnumerable()).Where(item => !removeNorth || item.Label != "North").ToArray();
        var series = ChartSeries.CreateDottedMap(new ChartSeriesId("locations"), "Locations", ordered);
        for (var index = 0; index < ordered.Length; index++) {
            series.WithObservationId(index, new ChartObservationId(ordered[index].Label.ToLowerInvariant()));
            if (ordered[index].Label == "North") series.WithPointColor(index, ChartColor.FromRgb(220, 70, 80));
        }
        return Chart.Create().WithSize(width, 420).WithTitle("Observed locations")
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithMapViewport(ChartMapViewport.Europe()).WithPointLegend().WithDataLabels(false).AddSeries(series);
    }

    private static XElement[] Entries(XDocument xml) => xml.Descendants().Where(node => Role(node) == "legend-entry").ToArray();
    private static string? Role(XElement node) => (string?)node.Attribute("data-cfx-role");
}
