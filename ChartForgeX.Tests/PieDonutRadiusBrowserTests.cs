using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class PieDonutRadiusBrowserTests {
    [BrowserTheory]
    [InlineData(false, false, 390)]
    [InlineData(false, true, 390)]
    [InlineData(false, false, 900)]
    [InlineData(false, true, 900)]
    [InlineData(true, false, 390)]
    [InlineData(true, true, 390)]
    [InlineData(true, false, 900)]
    [InlineData(true, true, 900)]
    public async Task VariableRadiusAndAutomaticRingsRetainNativeGeometrySourceLinksAndCompleteBrowserOutput(bool automatic, bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = automatic ? PieDonutAutomaticBandTests.Fixture(dark, width) : PieDonutRadiusDepthTests.Fixture(ChartSeriesKind.Pie, dark, width, true);
        chart.Series[chart.Series.Count - 1].WithDataLabels(); chart.Series[chart.Series.Count - 1].DataLabelPlacement = ChartDataLabelPlacement.Outside;
        chart.WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent);
        var prepared = PieDonutRadiusDepthTests.Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var rows = prepared.Scene.Nodes.OfType<VisualSceneSlice>().Select(mark => new {
            id = mark.Id, cx = mark.Cx, cy = mark.Cy, radius = mark.Outer, inner = mark.Inner, angle = mark.Start
        }).ToArray();
        Assert.Equal(automatic ? 9 : 3, rows.Length);
        var html = chart.ToVisualArtifact().ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize });
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 720);
        Assert.Equal(0, await session.Page.Locator("script, image, foreignObject").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("""
            rows => rows.every(row => {
                const path = [...document.querySelectorAll('path[data-cfx-source-id]')].find(path=>path.dataset.cfxSourceId===row.id);
                if (!path) return false;
                const first=path.getPointAtLength(row.inner>0 ? 0 : row.radius);
                return Math.abs(first.x-(row.cx+Math.cos(row.angle)*row.radius))<.02
                    && Math.abs(first.y-(row.cy+Math.sin(row.angle)*row.radius))<.02;
            })
            """, rows));
        var outer = chart.Series.Count - 1;
        var marks = prepared.Scene.Nodes.OfType<VisualSceneSlice>().Where(mark => mark.Id!.StartsWith("series-" + outer + "-point-", StringComparison.Ordinal)).ToArray();
        var connectors = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "data-label-connector").ToArray();
        Assert.Equal(3, connectors.Length);
        foreach (var connector in connectors) {
            var id = connector.Id!.Substring(0, connector.Id.Length - "-connector".Length); var mark = Assert.Single(marks, item => item.Id == id);
            var first = connector.Commands[0]; var angle = mark.Start + mark.Sweep / 2;
            Assert.Equal(mark.Cx + Math.Cos(angle) * mark.Outer, first.X, 8); Assert.Equal(mark.Cy + Math.Sin(angle) * mark.Outer, first.Y, 8);
        }
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        Assert.Equal(3, await session.Page.Locator("[data-cfx-label-for]:has(text)").CountAsync());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, (automatic ? "automatic-rings" : "variable-pie") + "-" + width + (dark ? "-dark" : "-light"));
            File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".native.png", png);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
        }
    }
}
