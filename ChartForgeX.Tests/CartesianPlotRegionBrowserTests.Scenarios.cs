using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class CartesianPlotRegionBrowserTests {
    [BrowserTheory]
    [InlineData(false, 740)]
    [InlineData(true, 390)]
    public async Task ScenarioElementChildrenRevealWithoutBecomingIndependentInteractiveMarks(bool dark, int width) {
        if (!Enabled) return;
        var html = ScenarioFixture(dark, width);
        await using var session = await OpenAsync(html, width, 820);
        var page = session.Page;
        await RecordScenarioRevealsAsync(page);
        await page.Locator("[data-cfx-scenario=element-route]").ClickAsync();
        await ClickScenarioStepAsync(page, 0);
        Assert.Equal("Observed detail", await page.Locator(".cfx-reveal-label").InnerTextAsync());
        Assert.True(await page.Locator("#observed-detail").EvaluateAsync<bool>("n => n.classList.contains('cfx-scenario-step-active')"));
        Assert.Null(await page.Locator("#observed-detail").GetAttributeAsync("data-cfx-target-kind"));
        Assert.Null(await page.Locator("#observed-detail").GetAttributeAsync("tabindex"));
        var first = await ScenarioRevealFactsAsync(page);
        var target = first.GetProperty("events")[0].GetProperty("target");
        Assert.Equal("element", target.GetProperty("targetKind").GetString());
        Assert.Equal("observed-detail", target.GetProperty("targetId").GetString());
        Assert.Equal("scenario-step", first.GetProperty("events")[0].GetProperty("source").GetString());
        await page.Locator(".cfx-stage").ScrollIntoViewIfNeededAsync();
        await CapturePassiveAsync(session, html, "scenario-id-child-" + (dark ? "compact-dark" : "wide-light"), first);

        await ClickScenarioStepAsync(page, 1);
        Assert.Equal("1", await page.Locator(RegionRoot).GetAttributeAsync("data-cfx-reveal-count"));
        Assert.Equal("observations:1", await page.EvaluateAsync<string>("() => window.scenarioReveals.at(-1).target.targetId"));
        foreach (var index in new[] { 2, 3, 4 }) {
            // Series steps avoid a badge per mark; legend descendants and host controls
            // retain their existing reveal-layer boundaries.
            await ClickScenarioStepAsync(page, index);
            Assert.Equal(0, await page.Locator(".cfx-reveal-label").CountAsync());
        }
        Assert.Equal(2, await page.EvaluateAsync<int>("() => window.scenarioReveals.length"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false, 740)]
    [InlineData(true, 390)]
    public async Task ScenarioExplicitIdsAndRolesKeepPassiveGroupsAndCaptionsOutOfEmphasisAndReveals(bool dark, int width) {
        if (!Enabled) return;
        var html = ScenarioFixture(dark, width);
        await using var session = await OpenAsync(html, width, 820);
        var page = session.Page;
        await RecordScenarioRevealsAsync(page);
        await page.Locator("[data-cfx-scenario=passive-route]").ClickAsync();
        foreach (var index in Enumerable.Range(0, 4)) {
            await ClickScenarioStepAsync(page, index);
            var facts = await ScenarioRevealFactsAsync(page);
            Assert.Equal(0, facts.GetProperty("passiveEmphasis").GetInt32());
            Assert.Equal(0, facts.GetProperty("revealCount").GetInt32());
            Assert.Equal(0, facts.GetProperty("trailCount").GetInt32());
            Assert.Equal(0, facts.GetProperty("events").GetArrayLength());
        }
        await ClickScenarioStepAsync(page, 4);
        var observed = await ScenarioRevealFactsAsync(page);
        Assert.Equal(1, observed.GetProperty("revealCount").GetInt32());
        Assert.Equal(0, observed.GetProperty("passiveEmphasis").GetInt32());
        Assert.Equal("observations:0", observed.GetProperty("events")[0].GetProperty("target").GetProperty("targetId").GetString());
        Assert.Equal(1, observed.GetProperty("trailCount").GetInt32());
        await page.Locator(".cfx-stage").ScrollIntoViewIfNeededAsync();
        await CapturePassiveAsync(session, html, "scenario-passive-" + (dark ? "compact-dark" : "wide-light"), observed);
        AssertNoConsoleErrors(session);
    }

    private static string ScenarioFixture(bool dark, int width) {
        var chart = PassiveChart().WithSize(width < 500 ? 350 : 600, 360).WithLegend(true)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var html = chart.ToInteractiveHtmlPage(options => {
            options.Interaction.Features = ChartInteractionFeatures.RevealLabels | ChartInteractionFeatures.FocusTrail;
            options.Interaction.AddScenario("element-route", "Element route", scenario => scenario
                .AddElementStep("observed-detail", "Observed detail")
                .AddPointStep("observations:1", "Second observation")
                .AddSeriesStep("observations", "Observed series")
                .AddElementStep("legend-detail", "Legend detail")
                .AddElementStep("host-control", "Host control"));
            options.Interaction.AddScenario("passive-route", "Passive route", scenario => scenario
                .WithFocusMode(ChartInteractionScenarioFocusMode.Spotlight)
                .AddElementStep("guidance-group", "Guidance group")
                .AddElementStep("guidance-caption", "Guidance caption")
                .AddRoleStep("plot-region", Guidance, "Guidance role")
                .AddRoleStep("plot-region-caption", Guidance, "Caption role")
                .AddPointStep("observations:0", "Observed point"));
        });
        // Hosts may address SVG children by ID through AddElementStep. Retain actual
        // renderer geometry and metadata, adding only host IDs and a text alternative.
        var start = html.IndexOf("<svg", StringComparison.Ordinal);
        var end = html.IndexOf("</svg>", start, StringComparison.Ordinal) + 6;
        var svg = XElement.Parse(html.Substring(start, end - start));
        var point = svg.Descendants().Single(n => (string?)n.Attribute("data-cfx-role") == "point" && (string?)n.Attribute("data-cfx-point") == "0");
        var detail = point.Descendants().Single(n => (string?)n.Attribute("data-cfx-role") == "marker");
        detail.SetAttributeValue("id", "observed-detail");
        detail.SetAttributeValue("aria-label", "Observed detail");
        svg.Descendants().Single(n => (string?)n.Attribute("data-cfx-role") == "plot-region").SetAttributeValue("id", "guidance-group");
        svg.Descendants().Single(n => (string?)n.Attribute("data-cfx-role") == "plot-region-label").SetAttributeValue("id", "guidance-caption");
        var legend = svg.Descendants().Single(n => (string?)n.Attribute("data-cfx-role") == "legend-entry");
        legend.Descendants().Single(n => (string?)n.Attribute("data-cfx-role") == "legend-swatch").SetAttributeValue("id", "legend-detail");
        html = html.Substring(0, start) + svg.ToString(SaveOptions.DisableFormatting) + html.Substring(end);
        return html.Replace("<div class=\"cfx-stage\"", "<button id=\"host-control\" type=\"button\">Host control</button><div class=\"cfx-stage\"", StringComparison.Ordinal);
    }

    private static Task ClickScenarioStepAsync(IPage page, int index) =>
        page.Locator("[data-cfx-scenario-step-index='" + (index + 1) + "']").ClickAsync();

    private static Task RecordScenarioRevealsAsync(IPage page) => page.Locator(RegionRoot).EvaluateAsync("""
        root => { window.scenarioReveals=[]; root.addEventListener('cfxreveal', event => window.scenarioReveals.push(event.detail)); }
        """);

    private static Task<JsonElement> ScenarioRevealFactsAsync(IPage page) => page.Locator(RegionRoot).EvaluateAsync<JsonElement>("""
        root => ({events:window.scenarioReveals,revealCount:root.querySelectorAll('.cfx-reveal-label').length,
            trailCount:Number(root.dataset.cfxTrailCount || 0),
            passiveEmphasis:Array.from(root.querySelectorAll('[data-cfx-passive], [data-cfx-passive] *')).filter(n=>
                ['cfx-scenario-active','cfx-scenario-muted','cfx-scenario-step-active','cfx-scenario-step-muted','cfx-trail'].some(c=>n.classList.contains(c))).length})
        """);
}
