using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CartesianCoordinateCaptionTests {
    [Theory]
    [InlineData(.1, 5.15, "(0.1, 5.15)")]
    [InlineData(1e-10, -1e-12, "(1E-10, -1E-12)")]
    [InlineData(.10000000000000002, .10000000000000003, "(0.10000000000000002, 0.10000000000000003)")]
    public void HumanCoordinateCaptionsKeepReadableRoundtripValuesAndCompleteSourceFacts(double x, double y, string suffix) {
        var chart = Chart.Create().AddScatter("Reading", new[] { new ChartPoint(x, y) });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var group = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Single(node => node.Role == "point");
        var description = group.Metadata["aria-label"];
        Assert.EndsWith(suffix, description, StringComparison.Ordinal);
        Assert.Equal(x, double.Parse(group.Metadata["data-cfx-x"], CultureInfo.InvariantCulture));
        Assert.Equal(y, double.Parse(group.Metadata["data-cfx-y"], CultureInfo.InvariantCulture));
        Assert.Equal(description, prepared.Regions.Single(region => region.Id == group.Id).Label);
        var point = XDocument.Parse(prepared.ToSvg()).Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "point");
        Assert.Equal(description, (string?)point.Attribute("aria-label"));
    }
}
