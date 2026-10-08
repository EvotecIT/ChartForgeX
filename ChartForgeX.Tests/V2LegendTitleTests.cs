using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2LegendTitleTests {
    [Theory]
    [InlineData(ChartLegendPosition.Bottom)]
    [InlineData(ChartLegendPosition.Right)]
    public void LegendHeadingReservesMeasuredSpaceAndRetainsItsSourceAlternative(ChartLegendPosition position) {
        var chart = Chart.Create().AddLine("First", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) })
            .AddLine("Second", new[] { new ChartPoint(0, 2), new ChartPoint(1, 3) });
        var frame = new VisualFrame(legendPosition: position, legendTitle: "Legend heading");
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(480, 320)), frame: frame));
        var title = Assert.Single(prepared.Regions, region => region.Role == "legend-title");
        Assert.Equal("Legend heading", title.Label);
        Assert.True(title.Bounds.Width > 0 && title.Bounds.Height > 0);
        var entries = prepared.Regions.Where(region => region.Role == "legend").ToArray();
        Assert.Equal(2, entries.Length);
        Assert.All(entries, entry => Assert.True(entry.Bounds.Top >= title.Bounds.Bottom));
        var root = XDocument.Parse(prepared.ToSvg());
        Assert.Equal("Legend heading", Assert.Single(root.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "legend-title").Value);
        Assert.Equal(frame.LegendTitle, frame.WithHeadings("Changed heading", null).LegendTitle);
        Assert.Equal(480, prepared.ToRgba().Width);
    }

    [Fact]
    public void BoundedLegendKeepsFullHeadingWhenItCannotBePainted() {
        const string title = "Complete legend heading retained in a compact viewport";
        var chart = Chart.Create().AddLine("Data", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) });
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(180, 100)),
            frame: new VisualFrame(showLegend: true, legendTitle: title, legendMaximumHeightFraction: .1)));
        Assert.Equal(title, Assert.Single(prepared.Regions, region => region.Role == "legend-title").Label);
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "frame.legend-title-overflow");
    }
}
