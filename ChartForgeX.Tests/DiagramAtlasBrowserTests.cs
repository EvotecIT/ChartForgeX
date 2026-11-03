using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Exercises the published diagram sources through the native exporters and a real SVG host.</summary>
public sealed class DiagramAtlasBrowserTests {
    [BrowserTheory]
    [InlineData("flowchart")] [InlineData("sequence")] [InlineData("class")] [InlineData("state")]
    [InlineData("er")] [InlineData("mindmap")] [InlineData("gitgraph")] [InlineData("gantt")]
    [InlineData("c4")] [InlineData("architecture")] [InlineData("topology")]
    public async Task AtlasCaptionsRemainInsideTheirCanvasInBothAspectRatiosAndModes(string family) {
        if (!InteractiveChartBrowser.Enabled) return;
        foreach (var dark in new[] { false, true }) foreach (var wide in new[] { false, true }) {
            var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
            var width = wide ? 1100 : 390;
            var artifact = family == "topology" ? TopologyRecipe.Create(wide ? "wide" : "compact", mode).ToVisualArtifact()
                : MermaidRenderer.Render(File.ReadAllText(Path.Combine(TestRepository.Root, "Website", "static", "site", "atlas", family + ".mmd")), Options(width, mode)).Artifact!;
            Assert.NotNull(artifact);
            var svg = artifact.ToSvg(); var png = artifact.ToPng();
            var xml = XDocument.Parse(svg);
            Assert.DoesNotContain(xml.Descendants(), element => element.Name.LocalName is "script" or "foreignObject");
            var naturalWidth = double.Parse(xml.Root!.Attribute("width")!.Value, CultureInfo.InvariantCulture);
            await using var session = await InteractiveChartBrowser.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions {
                HtmlSizing = VisualArtifactHtmlSizing.PreserveSize
            }), (int)Math.Ceiling(Math.Max(width, naturalWidth) + 64), 850);
            var escaped = await session.Page.EvaluateAsync<string[]>("""
                () => [...document.querySelectorAll('svg text')].filter(text => {
                    const root = text.ownerSVGElement.getBoundingClientRect(), box = text.getBoundingClientRect();
                    return box.width > 0 && (box.left < root.left - 1 || box.right > root.right + 1 || box.top < root.top - 1 || box.bottom > root.bottom + 1);
                }).map(text => text.textContent)
                """);
            Assert.Empty(escaped);
            Assert.True(await session.Page.Locator("svg").CountAsync() > 0);
            InteractiveChartBrowser.AssertNoConsoleErrors(session);
            if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
                Directory.CreateDirectory(output);
                var path = Path.Combine(output, family + "-" + (wide ? "wide" : "compact") + "-" + (dark ? "dark" : "light"));
                File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".native.png", png);
                await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
            }
        }
    }

    private static MermaidRenderOptions Options(int width, VisualThemeMode mode) {
        var topology = new MermaidTopologyRenderOptions { Width = width, Height = 620 };
        return new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { ThemeMode = mode },
            Flowchart = new MermaidFlowchartRenderOptions { Width = width, Height = 620 },
            Sequence = new MermaidSequenceRenderOptions { Width = width, Height = 620 },
            Class = topology, State = topology, EntityRelationship = topology, MindMap = topology, C4 = topology, Architecture = topology,
            GitGraph = new MermaidGitGraphRenderOptions { Width = width, Height = 620 },
            Gantt = new MermaidGanttRenderOptions { Width = width, Height = 620 }
        };
    }
}
