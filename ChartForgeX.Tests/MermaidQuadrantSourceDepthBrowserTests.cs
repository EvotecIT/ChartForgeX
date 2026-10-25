using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class MermaidQuadrantSourceDepthBrowserTests {
    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(390, false)]
    [InlineData(390, true)]
    [InlineData(800, false)]
    [InlineData(800, true)]
    public async Task SharedSourceStylesAndIndependentCaptionsReachNativeSvgAndPng(int width, bool dark) {
        if (!Enabled) return;
        var rendered = MermaidRenderer.Render(QuadrantSourceDepthFixture.Read(dark), new MermaidRenderOptions {
            WarningPolicy = MermaidWarningPolicy.Reject,
            Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family },
            Quadrant = new MermaidQuadrantRenderOptions { Width = width, Height = 500 }
        });
        Assert.False(rendered.HasErrors); Assert.Empty(rendered.Diagnostics);
        var artifact = rendered.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = "<style>body{margin:0}svg,img{display:block;max-width:100%;height:auto}</style><section id='native'>" + svg
            + "</section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 1000);
        await session.Page.Locator("#png").EvaluateAsync("image=>image.decode()");
        var marks = session.Page.Locator("#native [data-cfx-role='mermaid-quadrant-point']");
        Assert.Equal(4, await marks.CountAsync());
        Assert.Equal(new[] { 9d, 7d, 6d, 5d }, await marks.EvaluateAllAsync<double[]>("nodes=>nodes.map(n=>Number(n.getAttribute('rx')??n.getAttribute('r')))") );
        var expectedColors = new[] { "#B45309", "#0369A1", "#7C3AED", "#334155" };
        for (var index = 0; index < expectedColors.Length; index++)
            await AssertCssColorsEqualAsync(session.Page, expectedColors[index], await marks.Nth(index).EvaluateAsync<string>("node=>getComputedStyle(node).fill"));
        Assert.Equal(new[] { 2d, 1d, 0d, 0d }, await marks.EvaluateAllAsync<double[]>("nodes=>nodes.map(n=>getComputedStyle(n).stroke==='none'?0:parseFloat(getComputedStyle(n).strokeWidth)||0)") );
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            () => {
                const svg=document.querySelector('#native svg'), frame=svg.getBoundingClientRect();
                const labels=[...svg.querySelectorAll('[data-cfx-role="axis-x-label"], [data-cfx-role="axis-y-label"]')];
                return {labels:labels.map(node=>node.textContent),clip:labels.filter(node=>{
                    const b=node.getBoundingClientRect(); return b.left<frame.left-1||b.right>frame.right+1||b.top<frame.top-1||b.bottom>frame.bottom+1;
                }).map(node=>node.textContent),
                    source:[...svg.querySelectorAll('[data-cfx-role="point"]')].map(node=>node.dataset.cfxSourcePoint),
                    scripts:svg.querySelectorAll('script,foreignObject').length,
                    pngWidth:document.querySelector('#png').naturalWidth,overflow:document.documentElement.scrollWidth>innerWidth};
            }
            """);
        foreach (var caption in new[] { "Low effort", "High effort", "Low impact", "High impact" })
            Assert.Contains(caption, observed.GetProperty("labels").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(new[] { "0", "1", "2", "3" }, observed.GetProperty("source").EnumerateArray().Select(value => value.GetString()));
        Assert.Empty(observed.GetProperty("clip").EnumerateArray());
        Assert.Equal(width, observed.GetProperty("pngWidth").GetInt32()); Assert.Equal(0, observed.GetProperty("scripts").GetInt32());
        Assert.False(observed.GetProperty("overflow").GetBoolean());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var stem = Path.Combine(output, $"quadrant-source-depth-{width}-{dark}");
            await File.WriteAllTextAsync(stem + ".html", html); await File.WriteAllTextAsync(stem + ".svg", svg);
            await File.WriteAllBytesAsync(stem + ".native.png", png); await File.WriteAllTextAsync(stem + ".runtime.json", observed.GetRawText());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        }
        AssertNoConsoleErrors(session);
    }
}
