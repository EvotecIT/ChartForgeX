using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Stories;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class VisualStoryChartMediaTests {
    [Fact]
    public void DefaultPaletteChartRetainsBothRasterAndVectorStoryMedia() {
        var chart = Chart.Create().WithSize(420, 260).WithTitle("Weekly builds").WithXLabels("Mon", "Tue", "Wed")
            .AddSmoothArea("Builds", new[] { new ChartPoint(1, 12), new ChartPoint(2, 18), new ChartPoint(3, 15) });
        var svg = chart.ToSvg(); var png = chart.ToPng();
        var text = XDocument.Parse(svg).Descendants().Where(element => element.Name.LocalName == "text").ToArray();
        Assert.NotEmpty(text);
        Assert.All(text, element => Assert.Contains("font-palette:normal", (string)element.Attribute("style")!));
        var media = new VisualStoryMediaSurface(png, "Weekly builds chart", svg);
        Assert.Equal(svg, media.Svg);
        Assert.Equal(420, media.Raster.Width); Assert.Equal(260, media.Raster.Height);
        var story = VisualStory.Create("Build results").WithSize(640, 400);
        story.Scene("result", "See the generated chart", .25).Panel("chart", media);
        story.Outcome("chart-visible", "The chart is visible", "chart");
        Assert.Contains("Weekly builds", story.ToSvg());
        Assert.True(story.ToPng().Length > 64);
    }
}
