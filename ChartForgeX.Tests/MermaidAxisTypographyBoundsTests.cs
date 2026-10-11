using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidAxisTypographyBoundsTests {
    [Theory]
    [InlineData(false, false, 900)]
    [InlineData(false, false, 320)]
    [InlineData(false, true, 900)]
    [InlineData(false, true, 320)]
    [InlineData(true, false, 900)]
    [InlineData(true, false, 320)]
    [InlineData(true, true, 900)]
    [InlineData(true, true, 320)]
    public async Task MaximumFontsRetainAuthoredCategoriesAndNumericScale(bool horizontal, bool graphite, int width) {
        var chart = horizontal ? Chart.Create().WithTheme(ChartTheme.Light().WithTypography(480, 320, 256, 256, 256, 256))
            .WithTitle("Horizontal bars").WithSize(width, 520).WithXLabels("Jan", "Feb", "Mar")
            .WithXAxis("Count").WithYAxis("Month").WithXAxisBounds(0, 100)
            .AddHorizontalBar("Values", new[] { new ChartPoint(1, 20), new ChartPoint(2, 50), new ChartPoint(3, 80) })
            : MermaidVerticalChart(width);
        chart.Options.Theme.UseGraphiteLayout = graphite;
        var svg = chart.ToSvg();
        var document = XDocument.Parse(svg);
        var png = chart.ToPng();
        Assert.NotEmpty(png);
        var evidence = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(evidence)) {
            Directory.CreateDirectory(evidence);
            var key = "axis-font-ceiling-" + (horizontal ? "horizontal-" : "vertical-") + graphite + "-" + width.ToString(CultureInfo.InvariantCulture);
            File.WriteAllText(Path.Combine(evidence, key + ".svg"), svg);
            File.WriteAllBytes(Path.Combine(evidence, key + "-native.png"), png);
        }
        var labels = document.Descendants().Where(element => element.Name.LocalName == "text"
            && ((string?)element.Parent?.Attribute("data-cfx-role")) is "axis-x-label" or "axis-y-label").ToArray();
        foreach (var caption in new[] { "Jan", "Feb", "Mar", "0", "100" }) Assert.Contains(caption, labels.Select(item => item.Value));
        foreach (var caption in new[] { "Month", "Count" }) Assert.Contains(document.Descendants(), item => item.Name.LocalName == "text" && item.Value == caption);
        Assert.Equal(256, chart.Options.Theme.DataLabelFontSize);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", width, 520);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            var name = "axis-font-ceiling-" + (horizontal ? "horizontal-" : "vertical-") + graphite + "-" + width.ToString(CultureInfo.InvariantCulture);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
        }
        Assert.True(await session.Page.EvaluateAsync<bool>("() => [...document.querySelectorAll('[data-cfx-role^=axis-] text')].every(t=>{const b=t.getBoundingClientRect();return b.width>0&&b.height>0&&b.left>=-0.1&&b.right<=innerWidth+0.1&&b.top>=-0.1&&b.bottom<=520.1})"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    private static Chart MermaidVerticalChart(int width) {
        const string source = "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\nxychart-beta"
            + "\nx-axis Month [Jan,Feb,Mar]\ny-axis Count 0 --> 100\nbar [20,50,80]";
        var rendered = MermaidRenderer.Render(source);
        Assert.False(rendered.HasErrors);
        return Assert.IsType<Chart>(rendered.Artifact!.Model).WithSize(width, 520);
    }
}
