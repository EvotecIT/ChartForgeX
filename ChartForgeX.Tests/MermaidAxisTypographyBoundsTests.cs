using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidAxisTypographyBoundsTests {
    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(false, false, 900)]
    [InlineData(false, false, 320)]
    [InlineData(false, true, 900)]
    [InlineData(false, true, 320)]
    [InlineData(true, false, 900)]
    [InlineData(true, false, 320)]
    [InlineData(true, true, 900)]
    [InlineData(true, true, 320)]
    public async Task MaximumFontsRetainAuthoredCategoriesAndNumericScale(bool horizontal, bool dark, int width) {
        var chart = horizontal ? Chart.Create().WithTheme(VisualTheme.Report().Configure(builder => builder.Typography = new VisualTypography(builder.Typography.Family, titleSize: 480, subtitleSize: 320, axisTitleSize: 256, axisSize: 256, legendSize: 256, dataLabelSize: 256)), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithTitle("Horizontal bars").WithSize(width, 520).WithXLabels("Jan", "Feb", "Mar")
            .WithXAxis("Count").WithYAxis("Month").WithXAxisBounds(0, 100)
            .AddHorizontalBar("Values", new[] { new ChartPoint(1, 20), new ChartPoint(2, 50), new ChartPoint(3, 80) })
            : MermaidVerticalChart(width, dark);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = prepared.ToSvg();
        var document = XDocument.Parse(svg);
        var png = prepared.ToPng();
        Assert.NotEmpty(png);
        var evidence = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(evidence)) {
            Directory.CreateDirectory(evidence);
            var key = "axis-font-ceiling-" + (horizontal ? "horizontal-" : "vertical-") + dark + "-" + width.ToString(CultureInfo.InvariantCulture);
            File.WriteAllText(Path.Combine(evidence, key + ".svg"), svg);
            File.WriteAllBytes(Path.Combine(evidence, key + "-native.png"), png);
        }
        var labels = document.Descendants().Where(element => element.Name.LocalName == "text"
            && ((string?)element.Parent?.Attribute("data-cfx-role")) is "axis-x-label" or "axis-y-label").ToArray();
        foreach (var caption in new[] { "Jan", "Feb", "Mar", "0", "100" }) Assert.Contains(caption, labels.Select(item => item.Value));
        var numericRole = horizontal ? "axis-x-label" : "axis-y-label";
        var numericLabels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(label => label.Role == numericRole).ToArray();
        var numericAxis = horizontal ? chart.Options.XAxis : chart.Options.YAxis;
        Assert.InRange(numericLabels.Length, 2, numericAxis.TickCount);
        var values = numericLabels.Select(label => double.Parse(label.Text.Lines.Single().Text, CultureInfo.InvariantCulture)).ToArray();
        for (var index = 2; index < values.Length; index++) Assert.Equal(values[1] - values[0], values[index] - values[index - 1], 7);
        Assert.All(numericLabels, label => {
            Assert.InRange(label.Text.Size, 10, 256);
            var text = label.Text.Lines.Single().Text;
            var source = Assert.Single(prepared.Regions, region => region.Role == numericRole && region.Label!.StartsWith(text + " (", StringComparison.Ordinal));
            var center = horizontal ? label.X + label.Text.Metrics.Width / 2 : label.Baseline - label.Text.Ascent + label.Text.Metrics.Height / 2;
            var tick = horizontal ? source.Bounds.Left : source.Bounds.Top;
            Assert.InRange(Math.Abs(center - tick), 0, .5);
        });
        foreach (var caption in new[] { "Month", "Count" }) Assert.Contains(document.Descendants(), item => item.Name.LocalName == "text" && item.Value == caption);
        Assert.Equal(256, chart.Options.Theme.Typography.DataLabelSize);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", width, 520);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            var name = "axis-font-ceiling-" + (horizontal ? "horizontal-" : "vertical-") + dark + "-" + width.ToString(CultureInfo.InvariantCulture);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
        }
        Assert.True(await session.Page.EvaluateAsync<bool>("() => [...document.querySelectorAll('[data-cfx-role^=axis-] text')].every(t=>{const b=t.getBoundingClientRect();return b.width>0&&b.height>0&&b.left>=-0.1&&b.right<=innerWidth+0.1&&b.top>=-0.1&&b.bottom<=520.1})"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    private static Chart MermaidVerticalChart(int width, bool dark) {
        var source = "%%{init: {'fontFamily':'Arial','theme':'" + (dark ? "dark" : "default") + "','themeVariables':{'fontSize':256}}}%%\nxychart-beta"
            + "\nx-axis Month [Jan,Feb,Mar]\ny-axis Count 0 --> 100\nbar [20,50,80]";
        var rendered = MermaidRenderer.Render(source);
        Assert.False(rendered.HasErrors);
        return Assert.IsType<Chart>(rendered.Artifact!.Model).WithSize(width, 520);
    }
}
