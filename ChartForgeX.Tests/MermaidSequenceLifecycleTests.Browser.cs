using System.Globalization;
using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidSequenceLifecycleTests {
    [BrowserTheory]
    [InlineData(false, 360)]
    [InlineData(false, 1100)]
    [InlineData(true, 360)]
    [InlineData(true, 1100)]
    public async Task LifetimeHeadersGroupRangesAndMessagesRemainVisibleInScrollableHosts(bool dark, int width) {
        var source = "%%{init: {'fontFamily':'" + BrowserProofFonts.Family + "','theme':'" + (dark ? "dark" : "default") + "'}}%%\n" + Source;
        var options = new MermaidRenderOptions {
            Sequence = new MermaidSequenceRenderOptions { Width = width, Height = 620 }
        };
        var rendered = MermaidRenderer.Render(source, options);
        Assert.Empty(rendered.Diagnostics);
        await ObserveLifecycleAsync(rendered.Artifact!, dark, width, "direct");
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        await ObserveLifecycleAsync(Assert.Single(fenced.Artifacts), dark, width, "fenced");
    }

    private static async Task ObserveLifecycleAsync(VisualArtifact artifact, bool dark, int width, string surface) {
        var svg = artifact.ToSvg();
        var png = artifact.ToPng();
        var html = artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize });
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 800);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        Assert.Equal(2, await session.Page.Locator("[data-cfx-role='sequence-participant-group']").CountAsync());
        // Mirrored lifetime endpoints use the retained participant footer, rather than a second cross.
        Assert.Equal(0, await session.Page.Locator("[data-cfx-role='sequence-destruction']").CountAsync());
        Assert.Equal(1, await session.Page.Locator("[data-cfx-role='sequence-participant-footer'][data-source-id='W']").CountAsync());
        var worker = await session.Page.Locator("[data-cfx-role='sequence-participant'][data-label='Worker']").BoundingBoxAsync();
        var service = await session.Page.Locator("[data-cfx-role='sequence-participant'][data-label='Service']").BoundingBoxAsync();
        Assert.NotNull(worker);
        Assert.NotNull(service);
        Assert.True(worker.Y > service.Y + service.Height);
        var host = session.Page.Locator("[data-cfx-html-sizing='preserve-size']");
        await host.FocusAsync();
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var name = "sequence-lifecycle-" + surface + "-" + width.ToString(CultureInfo.InvariantCulture) + (dark ? "-dark" : "-light");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
        }
        var scroll = await host.EvaluateAsync<double>("element => { element.scrollLeft = element.scrollWidth; return element.scrollLeft; }");
        Assert.True(scroll >= 0);
        if (string.IsNullOrWhiteSpace(output)) return;
        File.WriteAllText(Path.Combine(output, name + ".svg"), svg);
        File.WriteAllBytes(Path.Combine(output, name + ".native.png"), png);
        File.WriteAllText(Path.Combine(output, name + ".json"), JsonSerializer.Serialize(new { width, dark, surface, scroll, consoleErrors = 0 }));
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".scrolled.browser.png"), FullPage = true });
    }
}
