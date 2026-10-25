using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MermaidSankeyPaintBrowserTests {
    [BrowserTheory]
    [InlineData(390, false)] [InlineData(390, true)] [InlineData(1100, false)] [InlineData(1100, true)]
    public async Task SourceGradientAndOutlinedLabelsKeepExactNodeAndParallelFlowFactsInBothStaticConsumers(int width, bool dark) {
        if (!InteractiveChartBrowser.Enabled) return;
        var result = MermaidRenderer.Render(MermaidSankeyPaintTests.Source(), MermaidSankeyPaintTests.Options(width, dark)); Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var body = "<style>body{padding:12px;box-sizing:border-box;background:" + (dark ? "#171b23" : "#fff")
            + ";}section{max-width:100%;overflow:auto}svg,img{display:block;max-width:100%;height:auto}</style><section id='native'>" + svg
            + "</section><section><img id='pixels' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(body, width + 24, 1000); var page = session.Page;
        var errors = new List<string>(); page.PageError += (_, message) => errors.Add(message);
        await page.EvaluateAsync("async () => {await document.fonts.ready; await document.querySelector('#pixels').decode()}");
        var observed = await Observe(page); var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory); var stem = Path.Combine(directory, "sankey-source-paint-" + width + (dark ? "-dark" : "-light"));
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
            File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + ".native.png", png); File.WriteAllText(stem + ".observed.json", observed.GetRawText());
        }
        Assert.Equal(new[] { "Intake.eu[1]", "Review", "Ready 東京" }, observed.GetProperty("nodes").EnumerateArray().Select(node => node.GetString()));
        Assert.Equal(new[] { 6d, 2d, 8d }, observed.GetProperty("weights").EnumerateArray().Select(node => node.GetDouble()));
        Assert.Equal(3, observed.GetProperty("gradients").GetInt32()); Assert.Equal(3, observed.GetProperty("outlined").GetInt32());
        Assert.Equal(width, observed.GetProperty("pngWidth").GetInt32()); Assert.Equal(460, observed.GetProperty("pngHeight").GetInt32());
        Assert.Empty(observed.GetProperty("clippedLabels").EnumerateArray()); Assert.Equal(0, observed.GetProperty("activeElements").GetInt32());
        Assert.True(observed.GetProperty("fitsHost").GetBoolean());
        foreach (var otherWidth in new[] { width == 390 ? 1124 : 414, width + 24 }) {
            await page.SetViewportSizeAsync(otherWidth, 1000); var resized = await Observe(page);
            Assert.Equal(observed.GetProperty("nodes").GetRawText(), resized.GetProperty("nodes").GetRawText());
            Assert.Equal(observed.GetProperty("weights").GetRawText(), resized.GetProperty("weights").GetRawText()); Assert.True(resized.GetProperty("fitsHost").GetBoolean());
        }
        Assert.Empty(errors); InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
    private static Task<JsonElement> Observe(IPage page) => page.EvaluateAsync<JsonElement>("""
        () => {
          const svg=document.querySelector('#native svg'),box=svg.getBoundingClientRect();
          const labels=[...svg.querySelectorAll('[data-cfx-role="sankey-node-label"]')];
          return {nodes:[...svg.querySelectorAll('[data-cfx-role="sankey-node"]')].map(n=>n.getAttribute('data-cfx-target-id')),
            weights:[...svg.querySelectorAll('[data-cfx-role="sankey-link"]')].map(n=>Number(n.getAttribute('data-cfx-value'))),
            gradients:svg.querySelectorAll('linearGradient').length,outlined:labels.filter(n=>n.querySelector('text[stroke-width="4"]')).length,
            clippedLabels:labels.filter(n=>{const b=n.getBoundingClientRect();return b.left<box.left-1||b.top<box.top-1||b.right>box.right+1||b.bottom>box.bottom+1}).map(n=>n.textContent),
            pngWidth:document.querySelector('#pixels').naturalWidth,pngHeight:document.querySelector('#pixels').naturalHeight,
            activeElements:svg.querySelectorAll('script,image,foreignObject').length,fitsHost:document.documentElement.scrollWidth<=innerWidth};
        }
        """);
}
