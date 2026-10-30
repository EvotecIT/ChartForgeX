using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class TopologyScenarioPlaybackBrowserTests {
    [BrowserFact]
    public async Task DownloadedSvg_ExcludesPayloadButKeepsRouteAndLivePlayer() {
        var chart = TopologyScenarioPlaybackTests.ReplicationChart();
        var options = new TopologyRenderOptions {
            ActiveScenarioId = "replicate", EnableHtmlExportControls = true,
            EnableHtmlScenarioPanel = true, EnableHtmlScenarioControls = true
        };
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options), 1024, 800);
        var page = session.Page;
        var metadata = page.Locator("svg [data-cfx-scenarios], svg[data-cfx-scenarios]").First;
        var live = await metadata.GetAttributeAsync("data-cfx-scenarios");
        Assert.Contains("Site manager", live);

        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-topology-export='svg']").ClickAsync());
        var path = await download.PathAsync();
        var document = XDocument.Parse(await File.ReadAllTextAsync(path!));
        var summaries = document.Root!.DescendantsAndSelf().Attributes("data-cfx-scenarios").Single().Value;
        using var json = JsonDocument.Parse(summaries);
        var steps = json.RootElement[0].GetProperty("steps");
        Assert.Equal(6, steps.GetArrayLength());
        Assert.Equal("ldap", steps[0].GetProperty("id").GetString());
        Assert.All(steps.EnumerateArray(), step => {
            Assert.False(step.TryGetProperty("payload", out _));
            Assert.False(step.TryGetProperty("payloadFormat", out _));
        });
        Assert.Equal(live, await metadata.GetAttributeAsync("data-cfx-scenarios"));
        await page.Locator("[data-cfx-scenario-panel-steps] li").First.ClickAsync();
        await Assertions.Expect(page.Locator("[data-cfx-scenario-payload-body]")).ToContainTextAsync("Site manager");
    }

    [BrowserTheory]
    [InlineData(false, "#ffffff")]
    [InlineData(true, "#ffff00")]
    public async Task SelectedCaptions_WithLightScenarioColor_RetainTextContrast(bool dark, string color) {
        var chart = TopologyScenarioPlaybackTests.ReplicationChart()
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        chart.Scenarios[0].Color = color;
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(
            new TopologyRenderOptions { ActiveScenarioId = "replicate" }.WithHtmlScenarioPanel()), 390, 900);
        var page = session.Page;
        await page.Locator("[data-cfx-scenario-panel-steps] li").First.ClickAsync();
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            await page.ScreenshotAsync(new() { Path = Path.Combine(captures, "scenario-" + (dark ? "dark" : "light") + ".png"), FullPage = true });
        }
        foreach (var selector in new[] { ".cfx-topology-scenarios button[aria-pressed=true]", ".cfx-topology-scenario-panel__steps li[aria-current=step]" }) {
            var paints = await page.Locator(selector).First.EvaluateAsync<string[]>("node => [getComputedStyle(node).color, getComputedStyle(node).backgroundColor]");
            var rgba = await ResolveCssColorsAsync(page, paints);
            ChartColor Color(int[] c) => ChartColor.FromRgba((byte)c[0], (byte)c[1], (byte)c[2], (byte)c[3]);
            Assert.True(ChartColorMath.ContrastRatio(Color(rgba[0]), Color(rgba[1])) >= 4.5,
                "Selected caption ink must contrast with its actual background.");
        }
    }

    [BrowserFact]
    public async Task OverflowedTopologyAndSequence_ArrowKeyScrollsFocusedRegion() {
        var chart = TopologyScenarioPlaybackTests.ReplicationChart().WithViewport(1600, 600, 24);
        chart.Nodes[0].Label = "Administrative control plane";
        chart.Nodes[1].Label = "Primary directory service";
        chart.Nodes[2].Label = "Secondary directory service";
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(
            new TopologyRenderOptions { ActiveScenarioId = "replicate" }.WithHtmlScenarioPanel().WithHtmlScenarioSequence()), 390, 1000);
        foreach (var selector in new[] { ".cfx-topology-viewport", "[data-cfx-scenario-sequence=replicate]" }) {
            var region = session.Page.Locator(selector);
            Assert.True(await region.EvaluateAsync<bool>("node => node.scrollWidth > node.clientWidth"));
            Assert.Equal("0", await region.GetAttributeAsync("tabindex"));
            Assert.False(string.IsNullOrWhiteSpace(await region.GetAttributeAsync("aria-label")));
            await region.FocusAsync();
            Assert.True(await region.EvaluateAsync<bool>("node => document.activeElement === node"));
            await session.Page.Keyboard.PressAsync("ArrowRight");
            await session.Page.WaitForFunctionAsync("selector => document.querySelector(selector).scrollLeft > 0", selector);
        }
    }
}
