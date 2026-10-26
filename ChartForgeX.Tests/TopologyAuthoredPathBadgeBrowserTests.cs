using System.Text.Json;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class TopologyAuthoredPathBadgeBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390)] [InlineData(true, 1200)]
    public async Task PortableOrdinaryWorkflowKeepsNativeCurveBadgesAndEditableFactsInScriptFreeHtml(bool dark, int width) {
        var source = TopologyAuthoredPathBadgeRecipe.Create().WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var saved = source.ToVisualArtifact().ToInterchangeEnvelope();
        var restored = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();
        restored.Nodes.Single(node => node.Id == "gateway").Details[0].Text = "Pending review";
        var artifact = restored.ToVisualArtifact();
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.FitToWidth }), width, 720);
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            () => {
                const svg=document.querySelector('svg'), badges=[...svg.querySelectorAll('[data-cfx-role="topology-detail-badge"]')];
                const paths=[...svg.querySelectorAll('[data-cfx-role="topology-edge-line"]')];
                const escaped=badges.flatMap(rect=>{
                    const b=rect.getBBox(), node=rect.closest('[data-cfx-role="topology-node"]');
                    return [...node.querySelectorAll('[data-cfx-role="topology-node-detail"]')].filter(text=>{
                        const t=text.getBBox();return t.width&&t.height&&(t.x<b.x-.5||t.y<b.y-.5||t.x+t.width>b.x+b.width+.5||t.y+t.height>b.y+b.height+.5);
                    }).map(text=>text.textContent);
                });
                return {badges:badges.length, paths:paths.map(path=>path.getAttribute('d')), escaped,
                    italic:[...svg.querySelectorAll('text')].filter(text=>getComputedStyle(text).fontStyle==='italic').map(text=>text.textContent),
                    labelSurfaces:[...svg.querySelectorAll('[data-cfx-role="topology-edge-label-surface"]')].map(item=>item.getAttribute("fill").toUpperCase()),
                    text:svg.textContent, scripts:svg.querySelectorAll('script,image,foreignObject').length,
                    overflow:document.documentElement.scrollWidth>innerWidth};
            }
            """);
        Assert.Equal(2, observed.GetProperty("badges").GetInt32());
        Assert.Contains(observed.GetProperty("labelSurfaces").EnumerateArray(), item => item.GetString() == "#193857");
        Assert.Equal("#193857", restored.Edges[0].LabelBackgroundColor);
        Assert.Contains(observed.GetProperty("paths").EnumerateArray(), path => path.GetString()!.Contains('C'));
        Assert.Empty(observed.GetProperty("escaped").EnumerateArray()); Assert.Contains("Pending review", observed.GetProperty("text").GetString());
        Assert.Equal(0, observed.GetProperty("scripts").GetInt32()); Assert.False(observed.GetProperty("overflow").GetBoolean());
        Assert.Equal(saved.Edges[0].Topology!.PathGeometry!.PathData, restored.Edges[0].PathGeometry!.PathData);
        Assert.Equal("Queued", source.Nodes[0].Details[0].Text); InteractiveChartBrowser.AssertNoConsoleErrors(session);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "authored-path-badges-" + width + (dark ? "-dark" : "-light"));
            File.WriteAllText(path + ".svg", artifact.ToSvg()); File.WriteAllBytes(path + ".native.png", artifact.ToPng());
            File.WriteAllText(path + ".portable.json", artifact.ToInterchangeJson()); File.WriteAllText(path + ".observed.json", observed.GetRawText());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
        }
    }
    [BrowserTheory]
    [InlineData(false, 390)] [InlineData(true, 1200)]
    public async Task CynefinDescriptionSlantBadgesAndCurvedTransitionsSurvivePortableHtml(bool dark, int width) {
        // Geometry/portable paint proof uses bounded Latin faces; the original Unicode source retains its native parser/render coverage.
        var source = File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Tests", "Fixtures", "Mermaid", "cynefin-source-semantics.mmd"))
            .Replace("\\u1234", "abcd", StringComparison.Ordinal).Replace("\\t", " ", StringComparison.Ordinal);
        var document = new ChartForgeX.Mermaid.MermaidParser().ParseCynefin(source).Document!;
        var chart = ChartForgeX.Mermaid.MermaidCynefinRendering.ToTopologyChart(document)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var saved = chart.ToVisualArtifact().ToInterchangeEnvelope(); var restored = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();
        var artifact = restored.ToVisualArtifact();
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize }), width, 900);
        Assert.Equal(4, await session.Page.Locator("[data-cfx-role=topology-node][data-node-id=confusion] [data-cfx-role=topology-detail-badge]").CountAsync());
        Assert.Contains("Four", await session.Page.Locator("svg").EvaluateAsync<string>("node=>window.cfxCaptionText(node)"));
        Assert.True(await session.Page.EvaluateAsync<bool>("() => [...document.querySelectorAll('[data-cfx-role=topology-node-subtitle] text')].some(text=>getComputedStyle(text).fontStyle==='italic'&&text.getBoundingClientRect().width>0)"));
        Assert.True(await session.Page.EvaluateAsync<bool>("() => [...document.querySelectorAll('[data-cfx-role=topology-edge-line]')].every(path=>path.getAttribute('d').includes('C'))"));
        Assert.Equal(0, await session.Page.Locator("svg script,svg image,svg foreignObject").CountAsync());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "cynefin-source-depth-" + width + (dark ? "-dark" : "-light"));
            File.WriteAllText(path + ".source.mmd", source); File.WriteAllText(path + ".svg", artifact.ToSvg());
            File.WriteAllBytes(path + ".native.png", artifact.ToPng()); File.WriteAllText(path + ".portable.json", artifact.ToInterchangeJson());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
        }
    }

}
