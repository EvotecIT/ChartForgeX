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

    // A response travels its edge from target to source, so while it plays the arrow points back at the caller;
    // the step's interaction is named by the host's localized catalog.
    [BrowserFact]
    public async Task ResponseStep_PointsTheActiveArrowBackAndNamesItsInteractionInTheLocalizedCatalog() {
        var chart = TopologyChart.Create().WithId("notes").WithTitle("Save a note")
            .WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)
            .AddNode("app", "Notes app", 0, 0, TopologyNodeKind.Person)
            .AddNode("api", "Notes API", 0, 0, TopologyNodeKind.Server)
            .AddEdge("http", "app", "api", "HTTPS", direction: VisualLinkDirection.Forward)
            .AddScenario("save", "Save a note", scenario => scenario
                .AddMessageStep("http", TopologyScenarioInteraction.Request, "POST /notes")
                .AddMessageStep("http", TopologyScenarioInteraction.Response, "201 Created", reverse: true));
        var html = chart.ToInteractiveHtmlPage(new TopologyRenderOptions { ActiveScenarioId = "save" }.WithHtmlScenarioPanel(),
            configureLocalization: ui => { ui.Language = "pl"; ui[HtmlChartText.ScenarioRequest] = "żądanie"; ui[HtmlChartText.ScenarioResponse] = "odpowiedź"; });
        await using var session = await OpenAsync(html, 1024, 800);
        var page = session.Page;
        const string Arrow = """
            () => {
                const edge = document.querySelector('[data-cfx-role=topology-edge][data-edge-id=http]'), svg = edge.ownerSVGElement, matrix = svg.getScreenCTM();
                const screen = (x, y) => new DOMPoint(Number(x), Number(y)).matrixTransform(matrix);
                const visible = [...edge.querySelectorAll('[data-cfx-role=topology-marker],[data-cfx-scenario-arrow]')].filter(node => getComputedStyle(node).visibility !== 'hidden');
                const box = visible[0].getBoundingClientRect();
                return { count: visible.length, reversed: edge.getAttribute('data-cfx-scenario-arrow-reversed'), x: box.x + box.width / 2,
                    start: screen(edge.dataset.routeStartX, edge.dataset.routeStartY).x, end: screen(edge.dataset.routeEndX, edge.dataset.routeEndY).x,
                    route: document.querySelector('[data-cfx-scenario-payload-route]').textContent };
            }
            """;
        var steps = page.Locator("[data-cfx-scenario-panel-steps] li");
        await steps.Nth(0).ClickAsync();
        var request = await page.EvaluateAsync<JsonElement>(Arrow);
        Assert.Equal(1, request.GetProperty("count").GetInt32());
        Assert.Equal(JsonValueKind.Null, request.GetProperty("reversed").ValueKind);
        Assert.InRange(request.GetProperty("x").GetDouble(), (request.GetProperty("start").GetDouble() + request.GetProperty("end").GetDouble()) / 2, request.GetProperty("end").GetDouble());
        Assert.Contains("żądanie", request.GetProperty("route").GetString());

        await steps.Nth(1).ClickAsync();
        var response = await page.EvaluateAsync<JsonElement>(Arrow);
        Assert.Equal(1, response.GetProperty("count").GetInt32());
        Assert.Equal("true", response.GetProperty("reversed").GetString());
        // The arrowhead now sits at the caller's end, its body along the edge.
        Assert.InRange(response.GetProperty("x").GetDouble(), response.GetProperty("start").GetDouble(), (response.GetProperty("start").GetDouble() + response.GetProperty("end").GetDouble()) / 2);
        Assert.Contains("odpowiedź", response.GetProperty("route").GetString());
        Assert.Contains("odpowiedź", await steps.Nth(1).GetAttributeAsync("title"));

        await page.Locator("[data-cfx-scenario-step-control=reset]").ClickAsync();
        var cleared = await page.EvaluateAsync<JsonElement>(Arrow);
        Assert.Equal(JsonValueKind.Null, cleared.GetProperty("reversed").ValueKind);
        Assert.InRange(cleared.GetProperty("x").GetDouble(), (cleared.GetProperty("start").GetDouble() + cleared.GetProperty("end").GetDouble()) / 2, cleared.GetProperty("end").GetDouble());
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false, "#ffffff")]
    [InlineData(false, "#ffff00")]
    [InlineData(false, null)]
    [InlineData(true, "#ffffff")]
    [InlineData(true, "#ffff00")]
    [InlineData(true, null)]
    public async Task ScenarioControls_WithAccentFills_RetainTextAndBadgeContrast(bool dark, string? color) {
        var chart = TopologyScenarioPlaybackTests.ReplicationChart()
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        chart.Scenarios[0].Color = color;
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(
            new TopologyRenderOptions { ActiveScenarioId = "replicate" }.WithHtmlScenarioPanel()), 390, 900);
        var page = session.Page;
        await page.Locator("[data-cfx-topology-scenario='']").ClickAsync();
        await page.Locator("[data-cfx-topology-scenario=replicate]").ClickAsync();
        await page.Locator("[data-cfx-scenario-panel-steps] li").First.ClickAsync();
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            await page.ScreenshotAsync(new() { Path = Path.Combine(captures, "scenario-" + (color?.TrimStart('#') ?? "default") + "-" + (dark ? "dark" : "light") + ".png"), FullPage = true });
        }
        ChartColor Color(int[] c) => ChartColor.FromRgba((byte)c[0], (byte)c[1], (byte)c[2], (byte)c[3]);
        foreach (var step in await page.Locator("[data-cfx-scenario-panel-steps] li").AllAsync()) {
            var paints = await step.EvaluateAsync<string[]>("node => { const badge = getComputedStyle(node, '::before'); return [badge.color, badge.backgroundColor, badge.borderColor, getComputedStyle(node).backgroundColor, badge.borderWidth]; }");
            var rgba = await ResolveCssColorsAsync(page, paints.Take(4).ToArray());
            Assert.True(ChartColorMath.ContrastRatio(Color(rgba[0]), Color(rgba[1])) >= 4.5,
                "Every step number, including unselected steps, must contrast with its badge fill.");
            Assert.True(ChartColorMath.ContrastRatio(Color(rgba[1]), Color(rgba[3])) >= 3 ||
                paints[4] != "0px" && ChartColorMath.ContrastRatio(Color(rgba[2]), Color(rgba[3])) >= 3,
                "A badge must have a visible fill or ring against its step chip.");
        }
        var play = page.Locator("[data-cfx-scenario-step-control=play]");
        await play.ClickAsync();
        await Assertions.Expect(play).ToHaveAttributeAsync("aria-pressed", "true");
        foreach (var selector in new[] { ".cfx-topology-scenarios button", ".cfx-topology-scenario-panel__steps li[aria-current=step]", "[data-cfx-scenario-step-control=play][aria-pressed=true]" }) {
            var nodes = await page.Locator(selector).AllAsync();
            Assert.NotEmpty(nodes);
            foreach (var node in nodes) {
                var paints = await node.EvaluateAsync<string[]>("node => [getComputedStyle(node).color, getComputedStyle(node).backgroundColor]");
                var rgba = await ResolveCssColorsAsync(page, paints);
                Assert.True(ChartColorMath.ContrastRatio(Color(rgba[0]), Color(rgba[1])) >= 4.5,
                    "Scenario, step and playing captions must contrast with their actual backgrounds.");
            }
        }
    }

    [BrowserFact]
    public async Task OverflowedTopologyAndSequence_ArrowKeyScrollsFocusedRegion() {
        var chart = TopologyScenarioPlaybackTests.ReplicationChart().WithViewport(1600, 600, 24);
        chart.Nodes[0].Label = "Administrative control plane";
        chart.Nodes[1].Label = "Primary directory service";
        chart.Nodes[2].Label = "Secondary directory service";
        chart.Title = "   ";
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
