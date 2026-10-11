using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects native midpoint facts used by paired-boundary interaction on nonlinear axes.</summary>
public sealed class RangeMidlineMetadataTests {
    [Fact]
    public void CapturedMidlineUsesNativeMappedGeometryWithoutBecomingAReadoutFact() {
        var chart = Chart.Create().WithSize(600, 380).WithXAxisBounds(0, 1).WithYAxisBounds(10, 1000)
            .ConfigureYAxis(axis => axis.Scale = ChartScaleKind.Logarithmic);
        chart.AddRangeArea("Pair", new[] { new ChartRangeBand(.5, 1, 1800) });
        chart.Series[0].ConfigureMarkers(markers => markers.Enabled = false);
        var svg = XDocument.Parse(chart.ToSvg());
        var point = svg.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "point");
        var midline = svg.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "range-midline");
        var capturedY = double.Parse((string)point.Attribute("data-cfx-midline-y")!, CultureInfo.InvariantCulture);
        var path = ((string)midline.Attribute("d")!).Split(' ');
        Assert.Equal("M", path[0]);
        var nativeY = double.Parse(path[2], CultureInfo.InvariantCulture);
        Assert.InRange(System.Math.Abs(capturedY - nativeY), 0, .0005);
        Assert.DoesNotContain("midline-y", (string)point.Attribute("aria-label")!);
        var image = PngReader.Decode(chart.ToPng());
        var x = (int)double.Parse(path[1], CultureInfo.InvariantCulture);
        var y = (int)nativeY; var pixel = (y * image.Width + x) * 4;
        Assert.True(image.Pixels[pixel + 2] > image.Pixels[pixel] + 10, "The native dashed midpoint cap must remain visible.");
    }
}
