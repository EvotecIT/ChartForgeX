using System.Globalization;
using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidSequenceParOverTests {
    [BrowserTheory]
    [InlineData(false, 390)]
    [InlineData(false, 1100)]
    [InlineData(true, 390)]
    [InlineData(true, 1100)]
    public async Task ParOverLabelsNotesAndActivationsRemainVisibleInDirectAndFencedScrollableHosts(bool dark, int width) {
        var source = Source(dark);
        var options = new MermaidRenderOptions { Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family },
            Sequence = new MermaidSequenceRenderOptions { Width = width, Height = 620 } };
        var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics);
        await ObserveAsync(direct.Artifact!, dark, width, "direct");
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.Empty(fenced.Diagnostics);
        await ObserveAsync(Assert.Single(fenced.Artifacts), dark, width, "fenced");
    }

    private static async Task ObserveAsync(VisualArtifact artifact, bool dark, int width, string surface) {
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize });
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 800);
        var host = session.Page.Locator("[data-cfx-html-sizing='preserve-size']");
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var name = "sequence-par-over-" + surface + "-" + width.ToString(CultureInfo.InvariantCulture) + (dark ? "-dark" : "-light");
        // Capture the real surface before meaningful assertions, so failures retain their evidence.
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, name + ".svg"), svg); File.WriteAllBytes(Path.Combine(output, name + ".native.png"), png);
            File.WriteAllText(Path.Combine(output, name + ".native.json"), artifact.ToInterchangeJson());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
            await host.EvaluateAsync("element => element.scrollLeft = element.scrollWidth");
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".scrolled.browser.png"), FullPage = true });
            await host.EvaluateAsync("element => element.scrollLeft = 0");
        }
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            () => {
                const svg=document.querySelector('svg'), frame=svg.getBoundingClientRect(), clip=[];
                const labels=[...svg.querySelectorAll('text')].map(text=>[...text.querySelectorAll('tspan')].map(span=>span.textContent).join(' ')||text.textContent);
                for(const text of svg.querySelectorAll('text')) {
                    const box=text.getBoundingClientRect();
                    if(box.width&&box.height&&(box.left<frame.left-1.5||box.top<frame.top-1.5||box.right>frame.right+1.5||box.bottom>frame.bottom+1.5)) clip.push(text.textContent);
                }
                const host=document.querySelector('[data-cfx-html-sizing="preserve-size"]');
                return {labels,clip,blocks:svg.querySelectorAll('[data-cfx-role="sequence-block"]').length,
                    activations:svg.querySelectorAll('[data-cfx-role="sequence-activation-bar"]').length,
                    scripts:svg.querySelectorAll('script,foreignObject').length, pageOverflow:document.documentElement.scrollWidth>innerWidth,
                    contentWidth:host.scrollWidth,hostWidth:host.clientWidth};
            }
            """);
        if (!string.IsNullOrWhiteSpace(output)) File.WriteAllText(Path.Combine(output, name + ".json"), observed.GetRawText());
        Assert.Equal(3, observed.GetProperty("blocks").GetInt32()); Assert.Equal(0, observed.GetProperty("scripts").GetInt32());
        Assert.Equal(2, observed.GetProperty("activations").GetInt32());
        Assert.Empty(observed.GetProperty("clip").EnumerateArray()); Assert.False(observed.GetProperty("pageOverflow").GetBoolean());
        var labels = observed.GetProperty("labels").EnumerateArray().Select(value => value.GetString()).ToArray();
        foreach (var label in new[] { "3. Alpha request", "4. Forward", "5. Reply", "6. Alpha done", "7. Beta request", "8. Recovery", "9. Beta done", "10. After fragment", "First task note", "Guard note", "Terminal task note", "and  [Second task]", "and  [Notes only]" }) Assert.Contains(label, labels);
        Assert.DoesNotContain(labels, label => label?.Contains("parover", StringComparison.OrdinalIgnoreCase) == true);
        if (observed.GetProperty("contentWidth").GetInt32() > observed.GetProperty("hostWidth").GetInt32()) {
            await host.FocusAsync(); await session.Page.Keyboard.PressAsync("ArrowRight");
            await session.Page.WaitForFunctionAsync("() => document.querySelector('[data-cfx-html-sizing=\"preserve-size\"]').scrollLeft > 0");
            Assert.True(await host.EvaluateAsync<double>("element => element.scrollLeft") > 0);
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
