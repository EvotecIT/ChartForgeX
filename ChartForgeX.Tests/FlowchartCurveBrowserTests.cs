using System.Text.Json;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class FlowchartCurveBrowserTests {
    [BrowserTheory]
    [InlineData("flowchart", "compact", false)] [InlineData("flowchart", "wide", true)]
    [InlineData("swimlane", "compact", false)] [InlineData("swimlane", "wide", true)]
    public async Task OrdinaryNativeCurvesKeepVisibleLabelsAndScrollableScriptFreeOutput(string family, string variant, bool dark) {
        var width = variant == "compact" ? 640 : 1100;
        var artifact = FlowchartCurveRecipe.Create(family, variant, dark ? VisualThemeMode.Dark : VisualThemeMode.Light, BrowserProofFonts.Family);
        var html = artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize });
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 900);
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            () => {
                const svg=document.querySelector('svg'),frame=svg.getBoundingClientRect();
                return {nodes:svg.querySelectorAll('[data-cfx-role="topology-node"]').length,
                    groups:svg.querySelectorAll('[data-cfx-role="topology-group"]').length,
                    text:[...svg.querySelectorAll('text')].map(n=>n.textContent).join(' '),
                    cubic:[...svg.querySelectorAll('[data-cfx-role="topology-edge"] path')].some(p=>/[Cc]/.test(p.getAttribute('d')||'')),
                    clips:[...svg.querySelectorAll('text')].filter(n=>{const b=n.getBoundingClientRect();return n.textContent.trim()&&b.width&&b.height&&(b.left<frame.left-1.5||b.right>frame.right+1.5||b.top<frame.top-1.5||b.bottom>frame.bottom+1.5)}).map(n=>n.textContent),
                    active:svg.querySelectorAll('script,foreignObject,image').length,overflow:document.documentElement.scrollWidth>innerWidth};
            }
            """);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "curve-browser-" + family + "-" + variant + (dark ? "-dark" : "-light"));
            File.WriteAllText(path + ".svg", artifact.ToSvg()); File.WriteAllBytes(path + ".native.png", artifact.ToPng()); File.WriteAllText(path + ".json", artifact.ToInterchangeJson());
            File.WriteAllText(path + ".html", html); File.WriteAllText(path + ".observed.json", observed.GetRawText());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
        }
        Assert.Equal(6, observed.GetProperty("nodes").GetInt32()); Assert.Equal(2, observed.GetProperty("groups").GetInt32()); Assert.True(observed.GetProperty("cubic").GetBoolean());
        foreach (var label in new[] { "Receive request", "Review request", "Clarify details", "Approve request", "Publish result", "Archive receipt" })
            Assert.Contains(label, observed.GetProperty("text").GetString()!, StringComparison.Ordinal);
        Assert.Empty(observed.GetProperty("clips").EnumerateArray()); Assert.Equal(0, observed.GetProperty("active").GetInt32()); Assert.False(observed.GetProperty("overflow").GetBoolean());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
