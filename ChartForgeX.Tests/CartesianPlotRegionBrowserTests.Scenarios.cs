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

    [BrowserTheory]
    [InlineData(false, 740)]
    [InlineData(true, 390)]
    public async Task ScenarioRevealLabelsSeparateNearbyNativeTargetsWithoutDimmingChartStructure(bool dark, int width) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(width < 500 ? 350 : 600, 360).WithTitle("Nearby observations")
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10).WithLegend(false)
            .AddSeries(ChartSeries.CreateScatter(new ChartSeriesId("observations"), "Observed",
                new[] { new ChartPoint(4.98, 5), new ChartPoint(5.02, 5), new ChartPoint(8, 1) }));
        var html = chart.ToInteractiveHtmlPage(options => {
            options.Interaction.Features = ChartInteractionFeatures.RevealLabels | ChartInteractionFeatures.FocusTrail;
            options.Interaction.AddScenario("neighbors", "Nearby measurements", scenario => scenario
                .WithFocusMode(ChartInteractionScenarioFocusMode.Spotlight).AddRoleStep("point", "5", "Both measured observations"));
        });
        await using var session = await OpenAsync(html, width, 820);
        var page = session.Page;
        await RecordScenarioRevealsAsync(page);
        await page.Locator("[data-cfx-scenario=neighbors]").ClickAsync();
        await ClickScenarioStepAsync(page, 0);
        var facts = await page.Locator(RegionRoot).EvaluateAsync<JsonElement>("""
            root => {
                const box=node=>{const b=node.getBoundingClientRect();return{left:b.left,right:b.right,top:b.top,bottom:b.bottom,width:b.width,height:b.height};};
                return{stage:box(root.querySelector('.cfx-stage')),labels:[...root.querySelectorAll('.cfx-reveal-label')].map(box),
                    targets:[...root.querySelectorAll('[data-cfx-role=point].cfx-scenario-step-active')].map(box),
                    trail:[...root.querySelectorAll('.cfx-trail')].map(node=>({id:node.dataset.cfxTargetId,index:node.dataset.cfxTrailIndex})),
                    muted:root.querySelectorAll('[data-cfx-role=point].cfx-scenario-muted,[data-cfx-role=point].cfx-scenario-step-muted').length,
                    structure:[root.querySelector('svg'),...root.querySelectorAll('[data-cfx-role=title],[data-cfx-role^=axis],[data-cfx-role^=grid]')]
                        .filter(node=>node.classList.contains('cfx-scenario-active')||node.classList.contains('cfx-scenario-muted')||node.classList.contains('cfx-scenario-step-active')||node.classList.contains('cfx-scenario-step-muted')).length};
            }
            """);
        var labels = facts.GetProperty("labels").EnumerateArray().ToArray();
        var targets = facts.GetProperty("targets").EnumerateArray().ToArray();
        Assert.Equal(2, labels.Length); Assert.Equal(2, targets.Length);
        Assert.True(targets[0].GetProperty("right").GetDouble() > targets[1].GetProperty("left").GetDouble()
            && targets[1].GetProperty("right").GetDouble() > targets[0].GetProperty("left").GetDouble(),
            "The native targets must actually compete for the same preferred label location.");
        var stage = facts.GetProperty("stage");
        foreach (var label in labels) {
            Assert.True(label.GetProperty("width").GetDouble() > 0 && label.GetProperty("height").GetDouble() > 0);
            Assert.True(label.GetProperty("left").GetDouble() >= stage.GetProperty("left").GetDouble()
                && label.GetProperty("right").GetDouble() <= stage.GetProperty("right").GetDouble()
                && label.GetProperty("top").GetDouble() >= stage.GetProperty("top").GetDouble()
                && label.GetProperty("bottom").GetDouble() <= stage.GetProperty("bottom").GetDouble());
        }
        Assert.False(labels[0].GetProperty("left").GetDouble() < labels[1].GetProperty("right").GetDouble()
            && labels[1].GetProperty("left").GetDouble() < labels[0].GetProperty("right").GetDouble()
            && labels[0].GetProperty("top").GetDouble() < labels[1].GetProperty("bottom").GetDouble()
            && labels[1].GetProperty("top").GetDouble() < labels[0].GetProperty("bottom").GetDouble(), "Accepted reveal labels must not overlap.");
        Assert.Equal(new[] { "observations:0", "observations:1" }, facts.GetProperty("trail").EnumerateArray().Select(item => item.GetProperty("id").GetString()));
        Assert.Equal(new[] { "1", "2" }, facts.GetProperty("trail").EnumerateArray().Select(item => item.GetProperty("index").GetString()));
        Assert.Equal(1, facts.GetProperty("muted").GetInt32());
        Assert.Equal(0, facts.GetProperty("structure").GetInt32());
        var revealed = await ScenarioRevealFactsAsync(page);
        Assert.Equal(2, revealed.GetProperty("events")[0].GetProperty("count").GetInt32());
        Assert.Equal(2, revealed.GetProperty("trailCount").GetInt32());
        await CapturePassiveAsync(session, html, "scenario-collision-" + (dark ? "compact-dark" : "wide-light"), new { facts, revealed });
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
