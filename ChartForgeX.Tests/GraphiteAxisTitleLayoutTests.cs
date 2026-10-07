using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Requested horizontal axis titles occupy a measured row between the frame and Graphite plot.</summary>
public sealed class GraphiteAxisTitleLayoutTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void RequestedYAxisTitleSurvivesHeaderAndTopLegendInSvgAndPng(bool dark, bool legend) {
        var chart = Chart.Create().WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithSize(900, 520).WithTitle("Mermaid XY chart").WithSubtitle("xychart")
            .WithYAxis("Revenue").WithYAxisBounds(0, 50).WithXLabels("Jan", "Feb", "Mar")
            .AddLine("Revenue", new[] { new ChartPoint(1, 10), new ChartPoint(2, 25), new ChartPoint(3, 40) });
        if (legend) chart.AddLine("Expense", new[] { new ChartPoint(1, 5), new ChartPoint(2, 15), new ChartPoint(3, 30) });
        var svg = XDocument.Parse(chart.ToSvg());
        var title = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "y-axis-title");
        Assert.Equal("Revenue", title.Value);
        Assert.Equal("placed", (string?)title.Attribute("data-cfx-label-status"));
        Assert.NotEqual("none", (string?)title.Attribute("display"));
        Assert.Equal(chart.Options.Theme.AxisTitleFontSize, (double)title.Attribute("font-size")!);
        var bounds = Bounds(title);
        var neighbors = svg.Descendants().Where(element => element != title && element.Attribute("data-cfx-label-x") != null &&
            element.Attribute("display")?.Value != "none");
        Assert.All(neighbors, neighbor => Assert.False(Intersects(bounds, Bounds(neighbor)), "Axis title overlaps " + neighbor.Attribute("data-cfx-role")?.Value));

        // Graphite PNG paints the same placed label layer. Inspect its actual axis-title area,
        // which the SVG assertions prove contains no neighboring label or plot mark.
        var image = new PngChartRenderer().RenderImage(chart);
        var background = chart.Options.Theme.CardBackground;
        var ink = 0;
        for (var y = Math.Max(0, (int)Math.Floor(bounds.Top)); y < Math.Min(image.Height, (int)Math.Ceiling(bounds.Bottom)); y++)
            for (var x = Math.Max(0, (int)Math.Floor(bounds.Left)); x < Math.Min(image.Width, (int)Math.Ceiling(bounds.Right)); x++) {
                var offset = (y * image.Width + x) * 4;
                if (image.Pixels[offset + 3] > 0 && (image.Pixels[offset] != background.R || image.Pixels[offset + 1] != background.G || image.Pixels[offset + 2] != background.B)) ink++;
            }
        Assert.True(ink > 5, "The requested axis-title area should contain painted text pixels.");
    }

    private static ChartRect Bounds(XElement element) => new(Number(element, "x"), Number(element, "y"), Number(element, "width"), Number(element, "height"));
    private static double Number(XElement element, string suffix) => double.Parse(element.Attribute("data-cfx-label-" + suffix)!.Value, CultureInfo.InvariantCulture);
    private static bool Intersects(ChartRect a, ChartRect b) => a.Left < b.Right && a.Right > b.Left && a.Top < b.Bottom && a.Bottom > b.Top;
}
