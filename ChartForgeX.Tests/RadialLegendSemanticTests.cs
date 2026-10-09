using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RadialLegendSemanticTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void UnpaintedRadialSlicesRetainTheirSourceCollectionInNativeLegends(bool donut, bool allZero) {
        var chart = Chart.Create().WithSize(640, 360).WithXLabels("A", "B");
        var points = new[] { new ChartPoint(1, 0), new ChartPoint(2, allZero ? 0 : 4) };
        if (donut) chart.AddDonut("Values", points); else chart.AddPie("Values", points);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var legend = Assert.Single(XDocument.Parse(svg).Descendants(), node =>
            (string?)node.Attribute("data-cfx-role") == "legend-entry" && (string?)node.Attribute("data-cfx-point") == "0");
        Assert.Equal("radial-slice", (string?)legend.Attribute("data-cfx-derived"));
        Assert.Equal("0", (string?)legend.Attribute("data-cfx-source-points"));
        Assert.Equal("0", (string?)legend.Attribute("data-cfx-value"));
        Assert.DoesNotContain(XDocument.Parse(svg).Descendants(), node =>
            (string?)node.Attribute("data-cfx-role") == "radial-point" && (string?)node.Attribute("data-cfx-point") == "0");
        chart.Series[0].Points[0] = new ChartPoint(1, 20);
        chart.Options.MaximumPieSlices = 2;
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Equal(png, prepared.ToPng());
    }
}
