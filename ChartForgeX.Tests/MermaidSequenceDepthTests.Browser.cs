using System.Globalization;
using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidSequenceDepthTests {
    internal static string PresentationSource(string fixture, bool dark) {
        var source = Fixture(fixture);
        if (!dark) return source;
        if (fixture == "sequence-native-depth") return source.Replace("theme: default", "theme: dark", StringComparison.Ordinal)
            .Replace("signalColor: \"#276749\"", "signalColor: \"#74c69d\"", StringComparison.Ordinal)
            .Replace("signalTextColor: \"#194d35\"", "signalTextColor: \"#b7e4c7\"", StringComparison.Ordinal);
        return "%%{init: {'theme':'dark'}}%%\n" + source;
    }

    [BrowserTheory]
    [InlineData(false, 390)]
    [InlineData(false, 1100)]
    [InlineData(true, 390)]
    [InlineData(true, 1100)]
    public async Task SequenceDepthAndHalfArrowsRemainReadableThroughDirectAndFencedScrollableHosts(bool dark, int width) {
        foreach (var fixture in new[] { "sequence-native-depth", "sequence-half-arrows" }) {
            var source = PresentationSource(fixture, dark);
            var options = new MermaidRenderOptions {
                Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family },
                Sequence = new MermaidSequenceRenderOptions { Width = width, Height = 620 }
            };
            var direct = MermaidRenderer.Render(source, options);
            Assert.Empty(direct.Diagnostics);
            await ObserveDepthAsync(direct.Artifact!, fixture, dark, width, "direct");
            var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
            Assert.Empty(fenced.Diagnostics);
            await ObserveDepthAsync(Assert.Single(fenced.Artifacts), fixture, dark, width, "fenced");
        }
    }

    private static async Task ObserveDepthAsync(VisualArtifact artifact, string fixture, bool dark, int width, string surface) {
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize });
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 800);
        var host = session.Page.Locator("[data-cfx-html-sizing='preserve-size']");
        var name = fixture + "-" + surface + "-" + width.ToString(CultureInfo.InvariantCulture) + (dark ? "-dark" : "-light");
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        // Capture both initial and scrolled surfaces before contract assertions, including failures.
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, name + ".svg"), svg);
            File.WriteAllBytes(Path.Combine(output, name + ".native.png"), png);
            File.WriteAllText(Path.Combine(output, name + ".native.json"), artifact.ToInterchangeJson());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
            await host.EvaluateAsync("element => element.scrollLeft = element.scrollWidth");
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".scrolled.browser.png"), FullPage = true });
            await host.EvaluateAsync("element => element.scrollLeft = 0");
        }
        var observation = await session.Page.EvaluateAsync<JsonElement>("""
            () => {
                const svg=document.querySelector('svg'), host=document.querySelector('[data-cfx-html-sizing="preserve-size"]');
                const frame=svg.getBoundingClientRect(), clip=[];
                for (const text of svg.querySelectorAll('text')) {
                    const box=text.getBoundingClientRect();
                    if(box.width && box.height && (box.left<frame.left-1.5 || box.top<frame.top-1.5 || box.right>frame.right+1.5 || box.bottom>frame.bottom+1.5))
                        clip.push(text.textContent);
                }
                return {clip, scripts:svg.querySelectorAll('script,foreignObject').length,
                    text:window.cfxCaptionText(svg), hostWidth:host.clientWidth, contentWidth:host.scrollWidth,
                    pageOverflow:document.documentElement.scrollWidth>innerWidth};
            }
            """);
        if (!string.IsNullOrWhiteSpace(output)) File.WriteAllText(Path.Combine(output, name + ".json"), observation.GetRawText());
        Assert.Equal(0, observation.GetProperty("scripts").GetInt32());
        Assert.False(observation.GetProperty("pageOverflow").GetBoolean());
        Assert.Empty(observation.GetProperty("clip").EnumerateArray());
        Assert.Equal(fixture == "sequence-half-arrows" ? 16 : 4, await session.Page.Locator("[data-cfx-role='sequence-message']").CountAsync());
        if (fixture == "sequence-half-arrows") {
            Assert.Equal(8, await session.Page.Locator("[data-cfx-role='sequence-half-call-arrow']").CountAsync());
            Assert.Equal(8, await session.Page.Locator("[data-cfx-role='sequence-half-async-arrow']").CountAsync());
            Assert.Contains("Variant 16", observation.GetProperty("text").GetString());
        } else {
            Assert.Equal(3, await session.Page.Locator("[data-cfx-role='sequence-note']").CountAsync());
            foreach (var label in new[] { "Cache fallback", "Receipt path", "cached policy", "for receipt", "Stored record" })
                Assert.Contains(label, observation.GetProperty("text").GetString());
            var participantFill = await session.Page.Locator("[data-cfx-role='sequence-participant-box']").First.GetAttributeAsync("fill");
            Assert.Equal("#E3EFFF", participantFill);
        }
        if (observation.GetProperty("contentWidth").GetInt32() > observation.GetProperty("hostWidth").GetInt32()) {
            await host.FocusAsync();
            await session.Page.Keyboard.PressAsync("ArrowRight");
            await session.Page.WaitForFunctionAsync("() => document.querySelector('[data-cfx-html-sizing=\"preserve-size\"]').scrollLeft > 0");
            Assert.True(await host.EvaluateAsync<double>("element => element.scrollLeft") > 0);
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
