using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidXYOrientationTests {
    [Theory]
    [InlineData("bar")]
    [InlineData("line")]
    [InlineData("mixed")]
    public void HorizontalSourceAxesAndObservationsReachTheirPhysicalAxes(string kind) {
        var series = kind == "mixed" ? "bar [20,50,80]\nline [30,60,90]" : kind + " [20,50,80]";
        var source = "xychart-beta horizontal\nx-axis Month [Jan,Feb,Mar]\ny-axis Count 0 --> 100\n" + series;
        var parsed = new MermaidParser().ParseXYChart(source);
        Assert.False(parsed.HasErrors);
        var document = Assert.IsType<MermaidXYChartDocument>(parsed.Document);
        var chart = document.ToChart();
        chart.WithBarStyle(ChartBarStyle.Flat).WithLineMarkers(ChartLineMarkerMode.None);
        var barColor = ChartColor.FromHex("#1767CB");
        var lineColor = ChartColor.FromHex("#D017CB");
        foreach (var item in chart.Series) item.Color = item.Kind == ChartSeriesKind.HorizontalBar ? barColor : lineColor;
        Assert.Equal("Count", chart.XAxisTitle);
        Assert.Equal("Month", chart.YAxisTitle);
        Assert.Equal(0, chart.Options.XAxis.Minimum);
        Assert.Equal(100, chart.Options.XAxis.Maximum);
        Assert.Equal(new[] { "Jan", "Feb", "Mar" }, chart.Options.YAxis.Labels.Select(label => label.Text));
        Assert.Equal(new[] { 1d, 2d, 3d }, chart.Series[0].Points.Select(point => point.X));
        Assert.Equal(new[] { 20d, 50d, 80d }, chart.Series[0].Points.Select(point => point.Y));

        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var points = prepared.Regions.Where(region => region.Role == "point").Take(3).ToArray();
        Assert.Equal(3, points.Length);
        Assert.True(points[0].Bounds.Right < points[1].Bounds.Right && points[1].Bounds.Right < points[2].Bounds.Right);
        Assert.True(points[0].Bounds.Top > points[1].Bounds.Top && points[1].Bounds.Top > points[2].Bounds.Top);
        var svg = prepared.ToSvg();
        foreach (var label in new[] { "Jan", "Feb", "Mar", "Month", "Count" }) Assert.Contains(">" + label + "<", svg);
        var raster = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        if (kind != "line") foreach (var point in points)
            Assert.True(HorizontalRasterAssertions.HasColor(raster, point.Bounds.Left + point.Bounds.Width / 2, point.Bounds.Top + point.Bounds.Height / 2, barColor), "The native horizontal bar must occupy its prepared value/category position.");
        if (kind != "bar") {
            var linePoints = prepared.Regions.Where(region => region.Role == "point").Skip(kind == "mixed" ? 3 : 0).Take(3).ToArray();
            for (var index = 1; index < linePoints.Length; index++) {
                var first = linePoints[index - 1].Bounds; var second = linePoints[index].Bounds;
                Assert.True(HorizontalRasterAssertions.HasColor(raster, (first.Left + first.Width / 2 + second.Left + second.Width / 2) / 2,
                    (first.Top + first.Height / 2 + second.Top + second.Height / 2) / 2, lineColor), "The native line must connect the transposed observations.");
            }
        }
        var direct = MermaidRenderer.Render(source);
        Assert.False(direct.HasErrors);
        Assert.NotNull(direct.Artifact);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```\n");
        Assert.False(fenced.HasErrors);
        var artifact = Assert.Single(fenced.Artifacts);
        Assert.Equal("Horizontal", artifact.Metadata["mermaid.orientation"]);
        Assert.Equal("Count", Assert.IsType<Chart>(artifact.Model).XAxisTitle);
        Assert.True(artifact.ToPng().Length > 64);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void NumericSourceDomainKeepsItsBoundsAndAuthoredSampling(bool horizontal, bool single) {
        var parsed = new MermaidParser().ParseXYChart("xychart-beta" + (horizontal ? " horizontal" : "") +
            "\nx-axis Time 2 --> 8\ny-axis Count -10 --> 100\nline " + (single ? "[20]" : "[20,50,80]"));
        Assert.False(parsed.HasErrors);
        var chart = parsed.Document!.ToChart();
        var category = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        var value = horizontal ? chart.Options.XAxis : chart.Options.YAxis;
        Assert.Equal(2, category.Minimum); Assert.Equal(8, category.Maximum);
        Assert.Equal(-10, value.Minimum); Assert.Equal(100, value.Maximum);
        Assert.Equal(single ? new[] { 2d } : new[] { 2d, 5d, 8d }, chart.Series[0].Points.Select(point => point.X));
        Assert.True(chart.ToPng().Length > 64);
    }
}
