using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Axis titles reserve measured space and use the same prepared text in both static exports.</summary>
public sealed class GraphiteAxisTitleLayoutTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void RequestedYAxisTitleSurvivesHeaderAndTopLegendInSvgAndPng(bool dark, bool legend) {
        var chart = Chart.Create().Theme(VisualTheme.Graphite()).ThemeMode(dark ? ThemeMode.Dark : ThemeMode.Light)
            .Size(900, 520).Title("Mermaid XY chart").Subtitle("xychart")
            .YAxis(yAxis => yAxis.Title("Revenue").Bounds(0, 50)).XLabels("Jan", "Feb", "Mar")
            .AddLine("Revenue", new[] { new ChartPoint(1, 10), new ChartPoint(2, 25), new ChartPoint(3, 40) });
        chart.Legend(legend).Legend(l => l.Position(ChartLegendPosition.Top));
        var request = VisualExportRequest.ForChart(chart); var prepared = chart.Prepare(request.Context);
        var title = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "axis-y-title");
        Assert.Equal("Revenue", Assert.Single(title.Text.Lines).Text);
        var bounds = Bounds(title);
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text != title),
            neighbor => Assert.False(Intersects(bounds, Bounds(neighbor)), "Axis title overlaps " + neighbor.Role));
        var image = prepared.ToRgbaImage(request.RasterOptions); var ink = 0;
        for (var y = Math.Max(0, (int)Math.Floor(bounds.Top)); y < Math.Min(image.Height, (int)Math.Ceiling(bounds.Bottom)); y++)
            for (var x = Math.Max(0, (int)Math.Floor(bounds.Left)); x < Math.Min(image.Width, (int)Math.Ceiling(bounds.Right)); x++) {
                var offset = (y * image.Width + x) * 4;
                if (image.Pixels[offset + 3] > 0 && (image.Pixels[offset] != chart.Options.Theme.Resolve(chart.Options.ThemeMode).Surface.R
                    || image.Pixels[offset + 1] != chart.Options.Theme.Resolve(chart.Options.ThemeMode).Surface.G || image.Pixels[offset + 2] != chart.Options.Theme.Resolve(chart.Options.ThemeMode).Surface.B)) ink++;
            }
        Assert.True(ink > 5, "The requested axis-title area should contain native text pixels.");
        Assert.Contains("Revenue", chart.ToSvg());
    }

    private static ChartRect Bounds(VisualSceneText text) => new(text.X, text.Baseline - text.Text.Ascent, text.Text.Metrics.Width, text.Text.Metrics.Height);
    private static bool Intersects(ChartRect a, ChartRect b) => a.Left < b.Right && a.Right > b.Left && a.Top < b.Bottom && a.Bottom > b.Top;
}