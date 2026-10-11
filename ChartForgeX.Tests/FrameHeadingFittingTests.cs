using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class FrameHeadingFittingTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrdinaryLongTitlesRetainTheirSizeAndDoNotShrinkAShortSubtitle(bool dashboard) {
        var theme = dashboard ? ChartTheme.DashboardLight() : ChartTheme.Light();
        var chart = Chart.Create().WithTheme(theme).WithSize(320, 700)
            .WithTitle("An ordinary report heading that wraps across the available width")
            .WithSubtitle("Short subtitle").WithLegend(false)
            .AddLine("Values", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) });
        var document = XDocument.Parse(chart.ToSvg());
        var headings = document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "frame-heading")
            .SelectMany(element => element.Descendants().Where(child => child.Name.LocalName == "text")).ToArray();
        var subtitle = Assert.Single(headings, element => element.Value == "Short subtitle");
        Assert.Equal(theme.SubtitleFontSize, (double)subtitle.Attribute("font-size")!, 7);
        Assert.All(headings.Where(element => element != subtitle), element => Assert.Equal(theme.TitleFontSize, (double)element.Attribute("font-size")!, 7));
        Assert.Contains(headings, element => element != subtitle);
    }

    [Fact]
    public void OversizedTitleWidthFittingPreservesAnOrdinarySubtitle() {
        var chart = Chart.Create().WithSize(320, 1200).WithTitle("Oversized heading").WithSubtitle("Short subtitle")
            .ConfigureTitleStyle(style => style.WithFontSize(256)).WithLegend(false)
            .AddLine("Values", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) });
        var request = VisualExportRequest.ForChart(chart);
        var prepared = chart.Prepare(request.Context);
        var headings = XDocument.Parse(prepared.ToSvg()).Descendants().Where(element => element.Name.LocalName == "text").ToArray();
        Assert.Contains(headings, element => element.Value == "Oversized heading");
        var subtitle = Assert.Single(headings, element => element.Value == "Short subtitle");
        Assert.Equal(request.Context.Theme.Typography.SubtitleSize, (double)subtitle.Attribute("font-size")!, 7);
    }
}
