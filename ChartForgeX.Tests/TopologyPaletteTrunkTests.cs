using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Collection(nameof(TopologyDensePlanCacheTests))]
public sealed class TopologyPaletteTrunkTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DistinctIncomingPaletteSlotsKeepCompleteRoutesAndTargetMarkers(bool namedPort) {
        var chart = FanIn(namedPort);
        var options = Options();
        var prepared = chart.Prepare(options);
        var svg = XDocument.Parse(prepared.ToSvg());
        var reference = FanIn(namedPort);
        foreach (var edge in reference.Edges) { edge.Color = reference.Theme.Resolve(VisualThemeMode.Light).Palette[edge.PaletteIndex!.Value].ToCss(); edge.PaletteIndex = null; }
        var referencePixels = reference.ToPng(options);
        TopologyPaletteRegressionEvidence.Write("trunk-distinct-" + namedPort, prepared, Receipt(svg), referencePixels);
        Assert.Equal(0, Count(svg, "topology-shared-trunk-tail"));
        Assert.Equal(0, Count(svg, "topology-shared-trunk-hit"));
        Assert.Equal(3, Count(svg, "topology-marker"));
        Assert.All(chart.Edges, edge => Assert.Equal(chart.Theme.Resolve(VisualThemeMode.Light).Palette[edge.PaletteIndex!.Value].ToCss(),
            (string?)Owner(svg, edge.Id).Descendants().Single(element => Role(element) == "topology-marker").Attribute("stroke")));
        Assert.Equal(referencePixels, prepared.ToPng());
    }

    [Fact]
    public void PaletteMutationInvalidatesTheWarmSharedRoutePlan() {
        TopologyDenseRoutePlanner.ClearPlanCache();
        var chart = FanIn(false); foreach (var edge in chart.Edges) edge.PaletteIndex = 0;
        var options = Options(); var initial = chart.Prepare(options);
        Assert.Equal(1, Count(XDocument.Parse(initial.ToSvg()), "topology-shared-trunk-tail"));
        var initialPlans = TopologyDenseRoutePlanner.CachedPlanCount;
        chart.Edges[1].PaletteIndex = 1;
        var changed = chart.Prepare(options); var changedSvg = XDocument.Parse(changed.ToSvg());
        var changedPlans = TopologyDenseRoutePlanner.CachedPlanCount;
        TopologyDenseRoutePlanner.ClearPlanCache();
        var fresh = chart.Prepare(options);
        TopologyPaletteRegressionEvidence.Write("trunk-cache-initial", initial, new { initialPlans, painted = Receipt(XDocument.Parse(initial.ToSvg())) });
        TopologyPaletteRegressionEvidence.Write("trunk-cache-changed", changed, new { initialPlans, changedPlans, painted = Receipt(changedSvg), fresh = Receipt(XDocument.Parse(fresh.ToSvg())) }, fresh.ToPng());
        Assert.Null(Owner(changedSvg, "e1").Attribute("data-trunk-owner-id"));
        Assert.Equal(initialPlans + 1, changedPlans);
        Assert.Equal(fresh.ToSvg(), changed.ToSvg());
        Assert.Equal(fresh.ToPng(), changed.ToPng());
    }

    [Theory]
    [InlineData("default")]
    [InlineData("explicit")]
    [InlineData("muted")]
    public void ExistingSharedPaintPoliciesKeepOneTail(string policy) {
        TopologyDenseRoutePlanner.ClearPlanCache();
        var chart = FanIn(false);
        foreach (var edge in chart.Edges) {
            if (policy == "default") edge.PaletteIndex = null;
            if (policy == "explicit") edge.Color = "#DC2626";
            if (policy == "muted") edge.IsMuted = true;
        }
        var options = Options(); var prepared = chart.Prepare(options);
        var svg = XDocument.Parse(prepared.ToSvg()); var plans = TopologyDenseRoutePlanner.CachedPlanCount;
        var pixels = prepared.ToPng();
        if (policy != "default") chart.Edges[1].PaletteIndex = 7;
        var updated = chart.Prepare(options);
        TopologyPaletteRegressionEvidence.Write("trunk-control-" + policy, updated, new { plans, updatedPlans = TopologyDenseRoutePlanner.CachedPlanCount, painted = Receipt(svg) });
        Assert.Equal(1, Count(svg, "topology-shared-trunk-tail"));
        Assert.Equal(1, Count(svg, "topology-marker"));
        Assert.Equal(1, Count(XDocument.Parse(updated.ToSvg()), "topology-shared-trunk-tail"));
        Assert.Equal(plans, TopologyDenseRoutePlanner.CachedPlanCount);
        Assert.Equal(pixels, updated.ToPng());
    }

    [Fact]
    public void OutgoingFanOutKeepsIndependentPaletteRoutes() {
        var chart = FanIn(false);
        foreach (var edge in chart.Edges) {
            (edge.SourceNodeId, edge.TargetNodeId) = (edge.TargetNodeId, edge.SourceNodeId);
            edge.SourcePort = TopologyEdgePort.Left; edge.TargetPort = TopologyEdgePort.Right;
        }
        var prepared = chart.Prepare(Options()); var svg = XDocument.Parse(prepared.ToSvg());
        TopologyPaletteRegressionEvidence.Write("trunk-control-outgoing", prepared, Receipt(svg));
        Assert.Equal(0, Count(svg, "topology-shared-trunk-tail")); Assert.Equal(3, Count(svg, "topology-marker"));
        Assert.All(chart.Edges, edge => Assert.Equal(chart.Theme.Resolve(VisualThemeMode.Light).Palette[edge.PaletteIndex!.Value].ToCss(),
            (string?)Owner(svg, edge.Id).Descendants().Single(element => Role(element) == "topology-edge-line").Attribute("stroke")));
    }

    [BrowserFact]
    public async Task EqualRgbIncomingSlotsKeepIndependentHostColorsAndMarkers() {
        if (!Enabled) return;
        var tokens = new VisualDesignTokens { Palette = ChartPalettes.FromHex("#607080", "#607080", "#607080") };
        var chart = FanIn(false).WithTheme(new VisualTheme(tokens, tokens)); var options = Options();
        options.SvgColorVariables = tokens.ToSvgColorVariables(path => "--host-" + path.Replace('.', '-'));
        var prepared = chart.Prepare(options); var svg = prepared.ToSvg();
        await using var session = await OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", 940, 500);
        var target = tokens.Clone(); target.Palette = ChartPalettes.FromHex("#BE123C", "#0F766E", "#4338CA");
        var variables = target.ToSvgColorVariables(path => "--host-" + path.Replace('.', '-'));
        await session.Page.Locator("svg").EvaluateAsync("(svg,css)=>svg.setAttribute('style',css)",
            string.Join(";", variables.GetVariablesForSvg(svg).Select(variable => variable.Name + ":" + variable.Color.ToCss())));
        var receipt = await session.Page.Locator("[data-cfx-role=topology-edge]").EvaluateAllAsync<object>("""
            edges=>edges.map(edge=>({id:edge.dataset.edgeId,slot:edge.dataset.edgePaletteIndex,owner:edge.dataset.trunkOwnerId,
              routes:[...edge.querySelectorAll('[data-cfx-role=topology-edge-line]')].map(line=>getComputedStyle(line).stroke),
              markers:[...edge.querySelectorAll('[data-cfx-role=topology-marker]')].map(marker=>getComputedStyle(marker).stroke)}))
            """);
        chart.WithTheme(new VisualTheme(target, target));
        TopologyPaletteRegressionEvidence.Write("trunk-equal-rgb-host", chart.Prepare(options), receipt);
        await TopologyPaletteRegressionEvidence.CaptureAsync(session.Page, "trunk-equal-rgb-host");
        Assert.Equal(0, await session.Page.Locator("[data-cfx-role=topology-shared-trunk-tail]").CountAsync());
        Assert.Equal(3, await session.Page.Locator("[data-cfx-role=topology-marker]").CountAsync());
        for (var index = 0; index < 3; index++) {
            var edge = session.Page.Locator("[data-cfx-role=topology-edge][data-edge-id=e" + index + "]");
            await AssertCssColorsEqualAsync(session.Page, target.Palette[index].ToCss(), await edge.Locator("[data-cfx-role=topology-marker]").EvaluateAsync<string>("node=>getComputedStyle(node).stroke"));
            Assert.All(await edge.Locator("[data-cfx-role=topology-edge-line]").EvaluateAllAsync<string[]>("nodes=>nodes.map(node=>node.getAttribute('stroke'))"),
                stroke => Assert.Contains("--host-series-" + (index + 1), stroke, StringComparison.Ordinal));
        }
        AssertNoConsoleErrors(session);
    }

    private static TopologyChart FanIn(bool namedPort) {
        var chart = TopologyRoutingFixtures.FanIn(namedPort).WithTheme(VisualTheme.Graphite().WithPalette(VisualThemeMode.Light,
            ChartColor.FromHex("#BE123C"), ChartColor.FromHex("#0F766E"), ChartColor.FromHex("#4338CA")));
        for (var index = 0; index < chart.Edges.Count; index++) chart.Edges[index].PaletteIndex = index;
        return chart;
    }

    private static TopologyRenderOptions Options() { var options = TopologyRoutingFixtures.Options().WithPlainTopologyEdges(); options.ShareIncomingTrunks = true; return options; }
    private static string? Role(XElement element) => (string?)element.Attribute("data-cfx-role");
    private static int Count(XDocument svg, string role) => svg.Descendants().Count(element => Role(element) == role);
    private static XElement Owner(XDocument svg, string id) => svg.Descendants().Single(element => Role(element) == "topology-edge" && (string?)element.Attribute("data-edge-id") == id);
    private static object Receipt(XDocument svg) => new { tails = Count(svg, "topology-shared-trunk-tail"), markers = Count(svg, "topology-marker"), edges = svg.Descendants()
        .Where(element => Role(element) == "topology-edge").Select(edge => new { id = (string?)edge.Attribute("data-edge-id"), slot = (string?)edge.Attribute("data-edge-palette-index"),
            owner = (string?)edge.Attribute("data-trunk-owner-id"), routes = edge.Descendants().Where(element => Role(element) == "topology-edge-line").Select(line => new { path = (string?)line.Attribute("d"), stroke = (string?)line.Attribute("stroke") }) }) };
}
