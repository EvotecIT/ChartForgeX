using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class PreparedCartesianCalloutTests {
    [Theory]
    [InlineData(18)]
    // At 21px the full-height fallback crosses the second series; the measured
    // intermediate lane must preserve the caption between the two strokes.
    [InlineData(21)]
    public void BroadEndpointCalloutKeepsFullCaptionInMeasuredInwardLane(double fontSize) {
        const string family = "CFX Broad Endpoint Callout";
        try {
            FontRegistry.Register(family, OpenTypeTestFonts.NameKeyed(extraGlyphs: new Dictionary<int, int> {
                ['A'] = OpenTypeTestFonts.Flex, ['p'] = OpenTypeTestFonts.Flex, ['r'] = OpenTypeTestFonts.Flex
            }));
            var chart = Chart.Create().WithSize(520, 300).WithTheme(ChartTheme.DashboardLight()).WithFontFamily(family)
                .WithDashboardTrendPanelStyle(showLegend: true).WithXLabels("Jan", "Feb", "Mar", "Apr")
                .AddSmoothLine("On-time", Points(21, 23, 25, 37), ChartColor.FromHex("#7057E6"))
                .AddSmoothLine("Absent", Points(18, 22, 17, 14), ChartColor.FromHex("#5FD3D9"))
                .WithDashboardTrendFocus(4, 37, "Apr", ChartColor.FromHex("#7057E6"), ChartDataLabelPlacement.Right);
            chart.Series[2].DataLabelStyle.FontSize = fontSize;
            var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
            var label = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "data-label");
            Assert.Equal("Apr", Assert.Single(label.Text.Lines).Text);
            Assert.DoesNotContain(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "annotation-label");
            Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "annotation-line");
            var annotation = Assert.Single(chart.Annotations);
            Assert.Equal("Apr", annotation.Label); Assert.False(annotation.ShowLabel);
            Assert.Contains("Apr", Assert.Single(prepared.Regions, region => region.Role == "annotation").Label);
            Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.data-label-overflow");
            var point = Assert.Single(prepared.Regions, region => region.Id == "series-2-point-0").Bounds;
            Assert.True(label.X + label.Text.Metrics.Width < point.Left);
            Assert.True(label.Baseline - label.Text.Metrics.Height > point.Bottom);
            var svg = prepared.ToSvg(); var png = prepared.ToPng();
            var metadata = System.Xml.Linq.XDocument.Parse(svg).Descendants()
                .Single(element => (string?)element.Attribute("data-cfx-role") == "annotation");
            Assert.Equal("VerticalLine", (string?)metadata.Attribute("data-cfx-kind"));
            Assert.Equal("4", (string?)metadata.Attribute("data-cfx-value"));
            Assert.Equal("Apr", (string?)metadata.Attribute("data-cfx-label"));
            Assert.Equal("false", (string?)metadata.Attribute("data-cfx-show-label"));
            chart.Series[2].WithPointLabel(0, "Changed");
            chart.Annotations.Clear();
            Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
            Assert.NotEqual(svg, chart.ToSvg());
        } finally {
            FontRegistry.Clear();
        }
    }

    [Fact]
    public void OrdinaryAnnotationPaintsItsCaptionByDefault() {
        var chart = Chart.Create().WithSize(520, 300).WithDataLabels(false)
            .AddLine("Observed", Points(10, 20, 30)).AddVerticalLine(2, "Milestone");
        Assert.True(Assert.Single(chart.Annotations).ShowLabel);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var label = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "annotation-label");
        Assert.Equal("Milestone", Assert.Single(label.Text.Lines).Text);
        Assert.Contains("Milestone", Assert.Single(prepared.Regions, region => region.Role == "annotation").Label);
    }

    private static ChartPoint[] Points(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value)).ToArray();
}
