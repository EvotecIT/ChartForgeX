using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class FrameHeadingFittingTests {
    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(18)]
    [InlineData(256)]
    public async Task ChangingOnlyDataLabelSizePreservesOrdinaryHeadingRoles(int size) {
        var theme = ChartTheme.DashboardLight();
        theme.DataLabelFontSize = size;
        var chart = Chart.Create().WithTheme(theme).WithSize(520, 260)
            .WithTitle("Average KPI").WithSubtitle("Company standard").WithLegend(false)
            .WithAxes(false).WithGrid(false).WithPadding(32, 80, 32, 26)
            .AddBar("KPI", new[] { new ChartPoint(1, 8), new ChartPoint(2, 9) });
        var texts = XDocument.Parse(chart.ToSvg()).Descendants().Where(element => element.Name.LocalName == "text").ToArray();
        Assert.Equal(theme.TitleFontSize, (double)Assert.Single(texts, element => element.Value == "Average KPI").Attribute("font-size")!, 7);
        Assert.Equal(theme.SubtitleFontSize, (double)Assert.Single(texts, element => element.Value == "Company standard").Attribute("font-size")!, 7);
        await Capture(chart, "dashboard-label-size-" + size, 520, 260);
    }

    [Trait("Category", "Browser")]
    [Fact]
    public async Task LargeCompactTitleRetainsItsOrdinarySubtitle() {
        var theme = ChartTheme.DashboardLight();
        var chart = Chart.Create().WithTheme(theme).WithSize(520, 260)
            .WithTitle("Average KPI").WithSubtitle("Company standard").WithLegend(false)
            .ConfigureTitleStyle(style => style.WithFontSize(64))
            .AddLine("KPI", new[] { new ChartPoint(1, 8), new ChartPoint(2, 9) });
        var texts = XDocument.Parse(chart.ToSvg()).Descendants().Where(element => element.Name.LocalName == "text").ToArray();
        Assert.Equal(64, (double)Assert.Single(texts, element => element.Value == "Average KPI").Attribute("font-size")!, 7);
        Assert.Equal(theme.SubtitleFontSize, (double)Assert.Single(texts, element => element.Value == "Company standard").Attribute("font-size")!, 7);
        await Capture(chart, "dashboard-large-title", 520, 260);
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(520, 260, 80)]
    [InlineData(300, 210, 34)]
    public async Task CompactDashboardHeadingsRetainTheThemesOrdinaryRoleSizes(int width, int height, int topPadding) {
        var theme = ChartTheme.DashboardLight();
        var chart = Chart.Create().WithTheme(theme).WithSize(width, height)
            .WithTitle("Average KPI").WithSubtitle("Company standard")
            .WithLegend(false).WithAxes(false).WithGrid(false)
            .WithPadding(24, topPadding, 24, 26)
            .AddBar("KPI", new[] { new ChartPoint(1, 8), new ChartPoint(2, 9) });
        var headings = XDocument.Parse(chart.ToSvg()).Descendants()
            .Where(element => element.Name.LocalName == "text").ToArray();
        Assert.Equal(theme.TitleFontSize, (double)Assert.Single(headings, element => element.Value == "Average KPI").Attribute("font-size")!, 7);
        Assert.Equal(theme.SubtitleFontSize, (double)Assert.Single(headings, element => element.Value == "Company standard").Attribute("font-size")!, 7);
        await Capture(chart, "dashboard-ordinary-" + width, width, height);
    }

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

    private static async Task Capture(Chart chart, string name, int width, int height) {
        var svg = chart.ToSvg();
        var png = chart.ToPng();
        Assert.NotEmpty(png);
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            File.WriteAllText(Path.Combine(capture, name + ".svg"), svg);
            File.WriteAllBytes(Path.Combine(capture, name + "-native.png"), png);
        }
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", width, height);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        if (!string.IsNullOrWhiteSpace(capture)) await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, name + "-browser.png"), FullPage = true });
    }
}
