using System.Text.Json;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyPreparedSceneRestorationBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390)] [InlineData(true, 1200)]
    public async Task ImportedSceneBoundsAndCompleteTitleStayVisibleThroughOrdinaryArtifactHtml(bool dark, int width) {
        var source = TopologyPreparedSceneRestorationCausalTests.Source(true).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var saved = source.Prepare().ToInterchangeEnvelope(); var chart = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();
        var artifact = chart.ToVisualArtifact(); var after = artifact.ToInterchangeEnvelope();
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize }), width, 760);
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            () => {
                const svg=document.querySelector('svg'), frame=svg.getBoundingClientRect();
                const groups=[...svg.querySelectorAll('[data-cfx-role="topology-group"]')].map(group=>{
                    const b=group.querySelector('[data-cfx-role="topology-group-surface"]').getBBox();
                    return {id:group.getAttribute('data-group-id'),x:b.x,y:b.y,width:b.width,height:b.height};
                });
                const clipped=[...svg.querySelectorAll('text')].filter(text=>{
                    const b=text.getBoundingClientRect(); return b.width&&b.height&&text.textContent.trim()&&(b.left<frame.left-1||b.top<frame.top-1||b.right>frame.right+1||b.bottom>frame.bottom+1);
                }).map(text=>text.textContent);
                const clips=[...svg.querySelectorAll('clipPath rect')].map(rect=>({x:rect.x.baseVal.value,y:rect.y.baseVal.value,width:rect.width.baseVal.value,height:rect.height.baseVal.value}));
                return {groups,clipped,clips,viewBox:{x:svg.viewBox.baseVal.x,y:svg.viewBox.baseVal.y,width:svg.viewBox.baseVal.width,height:svg.viewBox.baseVal.height},text:[...svg.querySelectorAll('text')].map(text=>text.textContent),
                    scripts:svg.querySelectorAll('script,image,foreignObject').length,overflow:document.documentElement.scrollWidth>innerWidth};
            }
            """);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "prepared-scene-restored-browser-" + width + (dark ? "-dark" : "-light"));
            File.WriteAllText(path + ".svg", artifact.ToSvg()); File.WriteAllBytes(path + ".native.png", artifact.ToPng());
            File.WriteAllText(path + ".portable.json", after.ToJson()); File.WriteAllText(path + ".observed.json", observed.GetRawText());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
        }
        TopologyPreparedSceneRestorationTests.AssertGeometry(saved, after); Assert.Equal(saved.Width, after.Width); Assert.Equal(saved.Height, after.Height);
        var group = Assert.Single(observed.GetProperty("groups").EnumerateArray()); var expected = Assert.Single(saved.Groups);
        // SVG getBBox exposes single-precision geometry; native roundtrip geometry above stays exact.
        Assert.Equal((double)(float)expected.X!.Value, group.GetProperty("x").GetDouble()); Assert.Equal((double)(float)expected.Y!.Value, group.GetProperty("y").GetDouble());
        Assert.Equal((double)(float)expected.Width!.Value, group.GetProperty("width").GetDouble()); Assert.Equal((double)(float)expected.Height!.Value, group.GetProperty("height").GetDouble());
        var text = observed.GetProperty("text").EnumerateArray().Select(value => value.GetString()).ToArray();
        foreach (var caption in new[] { "Saved title", "Saved subtitle", "Scope", "Accept", "Build" }) Assert.Contains(caption, text);
        var viewBox = observed.GetProperty("viewBox");
        Assert.Equal(0, viewBox.GetProperty("x").GetDouble()); Assert.Equal(0, viewBox.GetProperty("y").GetDouble());
        Assert.Equal(saved.Width, viewBox.GetProperty("width").GetDouble()); Assert.Equal(saved.Height, viewBox.GetProperty("height").GetDouble());
        Assert.All(observed.GetProperty("clips").EnumerateArray(), clip => {
            Assert.InRange(clip.GetProperty("x").GetDouble(), 0, saved.Width!.Value);
            Assert.InRange(clip.GetProperty("y").GetDouble(), 0, saved.Height!.Value);
            Assert.True(clip.GetProperty("x").GetDouble() + clip.GetProperty("width").GetDouble() <= saved.Width + .001);
            Assert.True(clip.GetProperty("y").GetDouble() + clip.GetProperty("height").GetDouble() <= saved.Height + .001);
        });
        Assert.Empty(observed.GetProperty("clipped").EnumerateArray()); Assert.Equal(0, observed.GetProperty("scripts").GetInt32()); Assert.False(observed.GetProperty("overflow").GetBoolean());
        var host = session.Page.Locator("[data-cfx-html-sizing='preserve-size']");
        if (await host.EvaluateAsync<bool>("element=>element.scrollWidth>element.clientWidth")) {
            await host.FocusAsync(); await session.Page.Keyboard.PressAsync("ArrowRight",new KeyboardPressOptions{Delay=100});
            await session.Page.WaitForFunctionAsync("() => document.querySelector('[data-cfx-html-sizing]').scrollLeft>0");
            Assert.True(await host.EvaluateAsync<double>("element=>element.scrollLeft") > 0);
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
