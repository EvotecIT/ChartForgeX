using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class MermaidQuadrantRegionBrowserTests {
    [Theory]
    [InlineData(360, false)]
    [InlineData(800, true)]
    public async Task NativeQuadrantCaptionsRemainInsideTheirRegionsInBrowserOutput(int width, bool dark) {
        if (!Enabled) return;
        var source = (dark ? "---\nconfig:\n  theme: dark\n---\n" : "")
            + "quadrantChart\ntitle Work priorities\nx-axis Low effort --> High effort\ny-axis Low impact --> High impact\n"
            + "quadrant-1 Plan\nquadrant-2 Prioritize\nquadrant-3 Consider\nquadrant-4 Reassess\n"
            + "Opportunity: [0.2, 0.8]\nInvestment: [0.8, 0.8]\n";
        var document = new MermaidParser().ParseQuadrant(source).Document!;
        var artifact = document.ToVisualArtifact(new MermaidQuadrantRenderOptions { Width = width, Height = 440 });
        var html = artifact.ToHtmlPage();
        await using var session = await OpenAsync(html, width + 24, 480);
        Assert.Equal(4, await session.Page.Locator("[data-cfx-role='plot-region-label']").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("""
            () => [...document.querySelectorAll('[data-cfx-role="plot-region"]')].every(region => {
              const fill = region.querySelector('[data-cfx-role="plot-region-fill"]').getBoundingClientRect();
              const caption = document.querySelector(`[data-cfx-source-id="${region.dataset.cfxSourceId}-caption"]`);
              const text = caption.querySelector('[data-cfx-role="plot-region-label"]').getBoundingClientRect();
              return text.left >= fill.left - 1 && text.right <= fill.right + 1
                && text.top >= fill.top - 1 && text.bottom <= fill.bottom + 1;
            })
            """));
        Assert.Equal(2, await session.Page.Locator("[data-cfx-role='mermaid-quadrant-point']").CountAsync());
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await File.WriteAllTextAsync(Path.Combine(capture, $"quadrant-browser-{width}-{dark}.html"), html);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, $"quadrant-browser-{width}-{dark}.png") });
        }
        AssertNoConsoleErrors(session);
    }
}
