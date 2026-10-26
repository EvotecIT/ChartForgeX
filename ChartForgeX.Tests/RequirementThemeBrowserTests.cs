using System.Text.Json;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class RequirementThemeBrowserTests {
    [BrowserTheory]
    [InlineData("compact", 390, false)] [InlineData("compact", 390, true)]
    [InlineData("wide", 1100, false)] [InlineData("wide", 1100, true)]
    public async Task NativeRequirementRolesKeepVisibleInkAndScrollableOrdinaryHtml(string variant, int width, bool dark) {
        var artifact = RequirementThemeRecipe.Create(variant, dark ? VisualThemeMode.Dark : VisualThemeMode.Light, BrowserProofFonts.Family);
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize }), width, 880);
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            () => {
                const svg=document.querySelector('svg'),frame=svg.getBoundingClientRect();
                return { text:[...svg.querySelectorAll('text')].map(t=>t.textContent).join('\n'),
                    fills:[...svg.querySelectorAll('[fill]')].map(e=>e.getAttribute('fill').toUpperCase()),
                    clips:[...svg.querySelectorAll('text')].filter(t=>{const b=t.getBoundingClientRect();return t.textContent.trim()&&b.width&&b.height&&(b.left<frame.left-1||b.right>frame.right+1||b.top<frame.top-1||b.bottom>frame.bottom+1)}).map(t=>t.textContent),
                    unsafe:svg.querySelectorAll('script,image,foreignObject').length, pageOverflow:document.documentElement.scrollWidth>innerWidth };
            }
            """);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "requirement-browser-" + variant + "-" + dark);
            File.WriteAllText(path + ".svg", artifact.ToSvg()); File.WriteAllBytes(path + ".native.png", artifact.ToPng());
            File.WriteAllText(path + ".observed.json", observed.GetRawText());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
        }
        Assert.Equal(0, observed.GetProperty("unsafe").GetInt32()); Assert.False(observed.GetProperty("pageOverflow").GetBoolean());
        Assert.Empty(observed.GetProperty("clips").EnumerateArray());
        Assert.Contains("Review work", observed.GetProperty("text").GetString()); Assert.Contains("satisfies", observed.GetProperty("text").GetString());
        Assert.Contains(observed.GetProperty("fills").EnumerateArray(), item => item.GetString() == (dark ? "#382535" : "#F5DBE4"));
        Assert.Contains(observed.GetProperty("fills").EnumerateArray(), item => item.GetString() == (dark ? "#3B2755" : "#E7F2E9"));
        var host = session.Page.Locator("[data-cfx-html-sizing='preserve-size']");
        if (await host.EvaluateAsync<bool>("element=>element.scrollWidth>element.clientWidth")) {
            await host.FocusAsync(); await session.Page.Keyboard.PressAsync("ArrowRight",new KeyboardPressOptions{Delay=100});
            await session.Page.WaitForFunctionAsync("()=>document.querySelector('[data-cfx-html-sizing]').scrollLeft>0");
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
