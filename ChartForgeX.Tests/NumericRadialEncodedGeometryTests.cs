using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NumericRadialEncodedGeometryTests {
    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 0)]
    [InlineData(true, 6)]
    [InlineData(false, 6)]
    public void EncodedNativeSlicesRetainTheirNonzeroFactWithoutInventingPaint(bool bars, double cornerRadius) {
        var chart = Create(bars);
        chart.WithRadialGeometry(new(cornerRadius: cornerRadius));
        if (cornerRadius > 0) chart.Series[0].WithPointFillPattern(0, ChartFillPattern.Crosshatch);
        Capture(chart, (bars ? "bar" : "column") + "-precision-" + cornerRadius);
        var svg = XDocument.Parse(chart.ToSvg());
        var small = Point(svg, 0);
        Assert.Equal("1", (string?)small.Attribute("data-cfx-y"));
        Assert.Equal("false", (string?)small.Attribute("data-cfx-clipped"));
        Assert.Equal("precision-collapse", (string?)small.Attribute("data-cfx-geometry-status"));
        Assert.DoesNotContain(small.Descendants(), node => node.Name.LocalName == "path");
        Assert.Single(Point(svg, 1).Descendants(), node => node.Name.LocalName == "path");
        var png = chart.ToPng();
        chart.Series[0].Points[0] = new ChartPoint(1, 0);
        Assert.Equal(chart.ToPng(), png);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RotatedReversedSlicesAndFullThinColumnsUseTheSameEncodedPaintContract(bool bars) {
        var chart = Create(bars).WithRadialGeometry(new(45, 315));
        chart.Options.YAxis.Reversed = true;
        Assert.Equal("precision-collapse", (string?)Point(XDocument.Parse(chart.ToSvg()), 0).Attribute("data-cfx-geometry-status"));
        if (bars) return;
        chart.Series[0].Points.RemoveAt(1);
        chart.WithYAxisBounds(0, 100000000).WithRadialGeometry(new(-90, 270, .2, 0, 0));
        var small = Point(XDocument.Parse(chart.ToSvg()), 0);
        Assert.Equal("precision-collapse", (string?)small.Attribute("data-cfx-geometry-status"));
        Assert.DoesNotContain(small.Descendants(), node => node.Name.LocalName == "path");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SharedStrokedSlicesDescribeTheirCollapsedFillWithoutRemovingTheirSeparator(bool donut) {
        var chart = SharedStrokedSlice(donut);
        Capture(chart, donut ? "donut-rounded" : "pie-rounded");
        var path = XDocument.Parse(chart.ToSvg()).Descendants().Single(node => node.Name.LocalName == "path"
            && node.Parent?.Attribute("data-cfx-point")?.Value == "1");
        Assert.Equal("false", (string?)path.Attribute("data-cfx-fill-area"));
        Assert.NotEqual("none", (string?)path.Attribute("stroke"));
        Assert.Equal("2", (string?)path.Attribute("stroke-width"));
    }

    internal static Chart Create(bool bars) {
        var chart = Chart.Create().WithSize(600, 440).WithHeader(false).WithAxes(false).WithGrid(false)
            .WithLegend(false).WithDataLabels(false).WithFontFamily("Carlito");
        chart.Options.PngFontPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        return NumericRadialSeriesTests.Add(chart, bars, "Observed", new ChartPoint(1, 1), new ChartPoint(2, 100000000));
    }

    internal static Chart SharedStrokedSlice(bool donut) {
        var chart = Create(true); chart.Series.Clear();
        var points = new[] { new ChartPoint(1, 100000000), new ChartPoint(2, 1) };
        if (donut) chart.AddDonut("Observed", points); else chart.AddPie("Observed", points);
        return chart.WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Right);
    }

    private static XElement Point(XDocument svg, int point) => svg.Descendants().Single(node =>
        (string?)node.Attribute("data-cfx-role") == "point" && (string?)node.Attribute("data-cfx-point") == point.ToString());

    internal static void Capture(Chart chart, string stem) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        var svg = chart.ToSvg();
        File.WriteAllText(Path.Combine(directory, stem + ".svg"), svg);
        File.WriteAllBytes(Path.Combine(directory, stem + "-native.png"), chart.ToPng());
        var points = XDocument.Parse(svg).Descendants().Where(node => node.Attribute("data-cfx-point") != null)
            .Select(node => new { metadata = node.Attributes().ToDictionary(attribute => attribute.Name.LocalName, attribute => attribute.Value),
                paths = node.Descendants().Where(child => child.Name.LocalName == "path").Select(child => (string?)child.Attribute("d")) });
        File.WriteAllText(Path.Combine(directory, stem + "-native.json"), JsonSerializer.Serialize(points, new JsonSerializerOptions { WriteIndented = true }));
    }
}
