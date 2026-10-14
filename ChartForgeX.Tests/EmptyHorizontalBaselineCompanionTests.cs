using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Raster;
using System.Xml.Linq;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class EmptyHorizontalBaselineCompanionTests {
    [Fact]
    public void EmptyVerticalBarDoesNotChangeSharedHorizontalLineCategoryBounds() {
        var chart = Chart.Create().AddLine("Readings", new[] { new ChartPoint(1, 20), new ChartPoint(2, 40) });
        chart.Options.LineOrientation = ChartOrientation.Horizontal;
        var expected = ChartRange.FromChart(chart);
        chart.AddBar("Empty", Array.Empty<ChartPoint>());
        ChartGrid.Create().Add(chart).WithSharedYAxis();
        Assert.Equal(expected.MinY, chart.Options.YAxis.Minimum);
        Assert.Equal(expected.MaxY, chart.Options.YAxis.Maximum);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void EmptyBaselineCompanionDoesNotLowerALogarithmicLineDomain(int physicalAxis) {
        var chart = Chart.Create().AddLine("Readings", new[] { new ChartPoint(1, 20), new ChartPoint(2, 40) });
        if (physicalAxis == 0) {
            chart.Options.LineOrientation = ChartOrientation.Horizontal;
            chart.Options.XAxis.Scale = ChartScaleKind.Logarithmic;
        } else if (physicalAxis == 1) chart.Options.YAxis.Scale = ChartScaleKind.Logarithmic;
        else {
            chart.Series[0].UseSecondaryYAxis();
            chart.Options.SecondaryYAxis.Scale = ChartScaleKind.Logarithmic;
        }
        ChartRange Domain() {
            var primary = ChartRange.FromChart(chart);
            return physicalAxis == 2 ? ChartRange.FromSecondaryYAxis(chart, primary) : primary;
        }
        var expected = Domain();
        if (physicalAxis == 0) chart.AddHorizontalBar("Empty", Array.Empty<ChartPoint>());
        else {
            chart.AddBar("Empty", Array.Empty<ChartPoint>());
            if (physicalAxis == 2) chart.Series[1].UseSecondaryYAxis();
        }
        var actual = Domain();
        Assert.Equal(expected.MinX, actual.MinX); Assert.Equal(expected.MaxX, actual.MaxX);
        Assert.Equal(expected.MinY, actual.MinY); Assert.Equal(expected.MaxY, actual.MaxY);
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EmptyBaselineCompanionDoesNotChangePopulatedHorizontalLineDomain(bool negative) {
        var sign = negative ? -1 : 1;
        var chart = Chart.Create().WithHeader(false).WithLegend(false).WithDataLabels(false).WithLineMarkers(ChartLineMarkerMode.None)
            .AddLine("Readings", new[] { new ChartPoint(1, sign * 20), new ChartPoint(2, sign * 40) });
        chart.Series[0].Color = ChartColor.FromHex("#E11D48");
        chart.Options.LineOrientation = ChartOrientation.Horizontal;
        var expected = ChartRange.FromChart(chart);
        var before = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        chart.AddHorizontalBar("Empty", Array.Empty<ChartPoint>());
        var actual = ChartRange.FromChart(chart);
        Assert.Equal(expected.MinX, actual.MinX); Assert.Equal(expected.MaxX, actual.MaxX);
        Assert.Equal(expected.MinY, actual.MinY); Assert.Equal(expected.MaxY, actual.MaxY);
        var after = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var beforePlot = Assert.Single(before.Regions, region => region.Role == "cartesian-plot").Bounds;
        var afterPlot = Assert.Single(after.Regions, region => region.Role == "cartesian-plot").Bounds;
        var beforePoints = before.Regions.Where(region => region.Role == "point").OrderBy(region => region.Id).ToArray();
        var afterPoints = after.Regions.Where(region => region.Role == "point").OrderBy(region => region.Id).ToArray();
        Assert.Equal(2, afterPoints.Length);
        for (var index = 0; index < afterPoints.Length; index++) {
            var first = beforePoints[index].Bounds; var second = afterPoints[index].Bounds;
            var expectedPosition = (first.Left + first.Width / 2 - beforePlot.Left) / beforePlot.Width;
            var actualPosition = (second.Left + second.Width / 2 - afterPlot.Left) / afterPlot.Width;
            Assert.True(Math.Abs(expectedPosition - actualPosition) * afterPlot.Width <= 1,
                "An empty companion must preserve the line's physical value placement within one raster pixel.");
        }
        var svg = after.ToSvg();
        var line = Assert.Single(XDocument.Parse(svg).Descendants(), element => (string?)element.Attribute("data-cfx-role") == "line");
        var vertices = Assert.Single(ChartMapPathParser.ParseSubpaths(line.Attribute("d")!.Value, 1)).Points;
        Assert.Equal(2, vertices.Count);
        Assert.Equal("#E11D48", (string?)line.Attribute("stroke"));
        for (var index = 0; index < vertices.Count; index++) {
            var bounds = afterPoints[index].Bounds;
            Assert.InRange(Math.Abs(vertices[index].X - (bounds.Left + bounds.Width / 2)), 0, .002);
            Assert.InRange(Math.Abs(vertices[index].Y - (bounds.Top + bounds.Height / 2)), 0, .002);
        }
        var png = after.ToPng();
        var image = RasterImageDecoder.Decode(png);
        var middle = new ChartPoint((vertices[0].X + vertices[1].X) / 2, (vertices[0].Y + vertices[1].Y) / 2);
        var found = false;
        for (var y = (int)middle.Y - 2; y <= (int)middle.Y + 2; y++) for (var x = (int)middle.X - 2; x <= (int)middle.X + 2; x++) {
            var offset = (y * image.Width + x) * 4;
            if (Math.Abs(image.Pixels[offset] - 225) <= 5 && Math.Abs(image.Pixels[offset + 1] - 29) <= 5
                && Math.Abs(image.Pixels[offset + 2] - 72) <= 5) found = true;
        }
        Assert.True(found, "The native PNG must paint the authored line within two pixels of its SVG midpoint.");
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", 800, 600);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            var stem = Path.Combine(directory, $"empty-baseline-{negative}");
            File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + "-native.png", png);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + "-browser.png", FullPage = true });
        }
    }
}
