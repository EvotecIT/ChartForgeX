using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class GitGraphDepthBrowserTests {
    [BrowserTheory]
    [InlineData("LR", false, 390)] [InlineData("LR", true, 390)] [InlineData("LR", false, 1100)] [InlineData("LR", true, 1100)]
    [InlineData("TB", false, 390)] [InlineData("TB", true, 390)] [InlineData("TB", false, 1100)] [InlineData("TB", true, 1100)]
    [InlineData("BT", false, 390)] [InlineData("BT", true, 390)] [InlineData("BT", false, 1100)] [InlineData("BT", true, 1100)]
    public async Task NativeGitDirectionsRetainAncestryAndScaledStaticSvgPngAtBothHostWidths(string direction, bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var options = MermaidGitGraphDepthTests.Options(width, dark); options.GitGraph!.Presentation!.FontFamily = BrowserProofFonts.Family;
        var result = MermaidRenderer.Render(MermaidGitGraphDepthTests.Source(direction, true), options); Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var body = "<style>body{background:" + (dark ? "#171b23" : "#fff") + ";padding:12px;box-sizing:border-box}section{max-width:100%;overflow:auto}svg,img{max-width:100%;height:auto;display:block}</style><section id='native'>"
            + svg + "</section><section><img id='pixels' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(body, width + 24, 900); var page = session.Page;
        var errors = new List<string>(); page.PageError += (_, message) => errors.Add(message);
        await page.EvaluateAsync("async () => { await document.fonts.ready; await document.querySelector('#pixels').decode(); }");
        var observed = await Observe(page); var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            if (!Path.IsPathRooted(output)) throw new InvalidOperationException("Captures require an absolute task-owned directory.");
            Directory.CreateDirectory(output); var stem = Path.Combine(output, "gitgraph-browser-" + direction + "-" + width + (dark ? "-dark" : "-light"));
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
            File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + ".native.png", png); File.WriteAllText(stem + ".observed.json", observed.GetRawText());
        }
        Assert.Equal(6, observed.GetProperty("commits").GetArrayLength()); Assert.Equal(7, observed.GetProperty("edges").GetArrayLength());
        Assert.Equal(width, observed.GetProperty("pngWidth").GetInt32()); Assert.Equal(620, observed.GetProperty("pngHeight").GetInt32());
        Assert.Empty(observed.GetProperty("clipped").EnumerateArray()); Assert.Equal(0, observed.GetProperty("activeElements").GetInt32());
        Assert.True(observed.GetProperty("fitsHost").GetBoolean());
        foreach (var other in new[] { width == 390 ? 1124 : 414, width + 24 }) {
            await page.SetViewportSizeAsync(other, 900); var resized = await Observe(page);
            Assert.Equal(observed.GetProperty("commits").GetRawText(), resized.GetProperty("commits").GetRawText());
            Assert.Equal(observed.GetProperty("edges").GetRawText(), resized.GetProperty("edges").GetRawText()); Assert.True(resized.GetProperty("fitsHost").GetBoolean());
        }
        Assert.Empty(errors); InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
    private static Task<JsonElement> Observe(IPage page) => page.EvaluateAsync<JsonElement>("""
        () => {
          const svg=document.querySelector('#native svg'),frame=svg.getBoundingClientRect(),clipped=[];
          for(const text of svg.querySelectorAll('text')){const b=text.getBoundingClientRect();if(b.width&&b.height&&text.textContent.trim()&&(b.left<frame.left-1.5||b.top<frame.top-1.5||b.right>frame.right+1.5||b.bottom>frame.bottom+1.5))clipped.push(text.textContent)}
          const png=document.querySelector('#pixels');
          return {commits:[...svg.querySelectorAll('[data-cfx-role=git-commit]')].map(n=>({id:n.dataset.commitId,branch:n.dataset.branch,x:+n.getAttribute('cx'),y:+n.getAttribute('cy')})),
            edges:[...svg.querySelectorAll('[data-cfx-role=git-commit-edge]')].map(n=>({id:n.dataset.commitId,parent:n.dataset.parentId})),clipped,
            activeElements:svg.querySelectorAll('script,image,foreignObject,[tabindex],button').length,pngWidth:png.naturalWidth,pngHeight:png.naturalHeight,
            fitsHost:document.documentElement.scrollWidth<=innerWidth+1};
        }
        """);
}
