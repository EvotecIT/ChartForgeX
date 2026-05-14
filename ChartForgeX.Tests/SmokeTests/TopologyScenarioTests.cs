using System;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyScenariosValidateReusableReferences() {
        var chart = TopologyChart.Create()
            .WithId("scenario-validation")
            .WithViewport(420, 240, 20)
            .AddNode("a", "A", 40, 80)
            .AddNode("b", "B", 220, 80)
            .AddEdge("a-b", "a", "b")
            .AddScenario("route", "Route", scenario => scenario
                .AddNodeStep("missing-node")
                .AddEdgeStep("missing-edge"))
            .AddScenario("route", "Duplicate", scenario => scenario
                .AddNodeStep("a"));

        var result = new TopologyChartValidator().Validate(chart);
        Assert(!result.IsValid, "Invalid topology scenarios should report validation errors.");
        Assert(result.Errors.Any(error => error.Code == "duplicate-scenario-id"), "Topology validator should detect duplicate scenario ids.");
        Assert(result.Errors.Any(error => error.Code == "missing-scenario-node"), "Topology validator should detect scenarios referencing missing nodes.");
        Assert(result.Errors.Any(error => error.Code == "missing-scenario-edge"), "Topology validator should detect scenarios referencing missing edges.");
        AssertThrows<TopologyValidationException>(() => chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false }), "Topology rendering should fail before rendering scenarios with missing references.");

        var emptyScenario = TopologyChart.Create()
            .AddNode("a", "A", 40, 80)
            .AddScenario("empty", "Empty");
        var emptyResult = new TopologyChartValidator().Validate(emptyScenario);
        Assert(emptyResult.Errors.Any(error => error.Code == "scenario-empty"), "Topology validator should reject scenarios without any node or edge steps.");
    }

    private static void TopologyScenarioMetadataIncludesEdgeEndpoints() {
        var chart = TopologyChart.Create()
            .WithId("edge-only-scenario")
            .WithViewport(420, 240, 20)
            .WithLegend(null)
            .AddNode("a", "A", 40, 80)
            .AddNode("b", "B", 220, 80)
            .AddEdge("a-b", "a", "b")
            .AddScenario("edge-only", "Edge only", scenario => scenario
                .AddEdgeStep("a-b", "Segment"));

        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false });
        Assert(CountOccurrences(svg, "data-scenario-ids=\"edge-only\"") == 3, "Edge-only scenarios should mark the edge and both endpoint nodes as scenario members.");
        Assert(CountOccurrences(svg, "data-scenario-step-indices=\"edge-only:0\"") == 3, "Endpoint nodes should carry the edge step index for static scenario consumers.");
        Assert(svg.Contains("data-node-id=\"a\"", StringComparison.Ordinal) && svg.Contains("data-node-id=\"b\"", StringComparison.Ordinal), "Scenario endpoint metadata should preserve normal node ids.");
        Assert(svg.Contains("data-edge-id=\"a-b\"", StringComparison.Ordinal), "Scenario edge metadata should preserve normal edge ids.");

        var duplicateEdges = TopologyChart.Create()
            .WithId("duplicate-edge-scenario")
            .WithViewport(520, 260, 20)
            .WithLegend(null)
            .AddNode("a", "A", 40, 80)
            .AddNode("b", "B", 220, 80)
            .AddNode("c", "C", 380, 80)
            .AddEdge("dup", "a", "b")
            .AddEdge("dup", "b", "c")
            .AddScenario("duplicate-edge", "Duplicate edge", scenario => scenario.AddEdgeStep("dup"));
        var duplicateSvg = duplicateEdges.ToSvg(new TopologyRenderOptions { IncludeLegend = false });
        Assert(CountOccurrences(duplicateSvg, "data-scenario-ids=\"duplicate-edge\"") == 5, "Duplicate-id edge scenario steps should mark every matching edge and all endpoint nodes.");
        Assert(CountOccurrences(duplicateSvg, "data-scenario-step-indices=\"duplicate-edge:0\"") == 5, "Duplicate-id edge scenario steps should keep node metadata complete for every matching edge instance.");
    }

    private static void TopologyActiveScenarioHighlightsStaticOutput() {
        var chart = TopologyChart.Create()
            .WithId("active-scenario")
            .WithViewport(560, 260, 20)
            .WithLegend(null)
            .AddGroup("main", "Main", 20, 70, 360, 130)
            .AddNode("a", "A", 60, 110, groupId: "main")
            .AddNode("b", "B", 220, 110, groupId: "main")
            .AddNode("c", "C", 410, 110)
            .AddEdge("a-b", "a", "b")
            .AddEdge("b-c", "b", "c")
            .AddScenario("route", "Route", scenario => scenario
                .AddEdgeStep("a-b"));

        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false, ActiveScenarioId = "route" });
        Assert(svg.Contains("data-cfx-active-scenario=\"route\"", StringComparison.Ordinal), "Static topology SVG should expose the resolved active scenario id.");
        Assert(svg.Contains("data-group-id=\"main\"", StringComparison.Ordinal) && svg.Contains("cfx-topology--highlighted", StringComparison.Ordinal), "Static active scenarios should keep groups that contain route nodes highlighted.");
        Assert(svg.Contains("data-edge-id=\"a-b\"", StringComparison.Ordinal) && svg.Contains("cfx-topology--highlighted", StringComparison.Ordinal), "Static active scenarios should highlight route edges.");
        Assert(svg.Contains("data-edge-id=\"b-c\"", StringComparison.Ordinal) && svg.Contains("cfx-topology--dimmed", StringComparison.Ordinal), "Static active scenarios should not highlight unrelated connected edges.");
        Assert(svg.Contains("data-node-id=\"c\"", StringComparison.Ordinal) && svg.Contains("opacity=\"0.28\"", StringComparison.Ordinal), "Static active scenarios should dim nodes outside the scenario.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false, ActiveScenarioId = "route" }).Length > 64, "Static active scenario highlighting should render as PNG.");

        var missing = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false, ActiveScenarioId = "missing" });
        Assert(!missing.Contains("data-cfx-active-scenario=\"missing\"", StringComparison.Ordinal), "Unknown active scenario ids should not be emitted as resolved static scenario state.");

        var focusedViewSvg = chart.ToSvg(new TopologyRenderOptions {
            IncludeLegend = false,
            View = new TopologyView { NodeIds = { "a", "b" } }
        });
        Assert(focusedViewSvg.Contains("data-chart-id=\"active-scenario\"", StringComparison.Ordinal), "Focused topology views should render when global scenarios reference nodes or edges outside the current view.");
    }

    private static void TopologyScenarioModelsRejectInvalidInputs() {
        var scenario = new TopologyScenario { Id = "scenario", Label = "Scenario" }
            .WithMetadata("owner", "network")
            .WithMetadata("empty", null);
        Assert(scenario.Metadata["owner"] == "network" && scenario.Metadata["empty"] == string.Empty, "Topology scenario metadata helpers should keep host metadata reusable and null-safe.");
        scenario.AddEdgeStep("edge", "Route", configure: step => step.WithMetadata("protocol", "https").WithMetadata("empty", null));
        Assert(scenario.Steps[0].Metadata["protocol"] == "https" && scenario.Steps[0].Metadata["empty"] == string.Empty, "Topology scenario step metadata helpers should keep route-step metadata reusable and null-safe.");

        var options = new TopologyRenderOptions()
            .WithHtmlScenarioControls(false)
            .WithHtmlScenarioPanel(false)
            .WithHtmlScenarioUrlState()
            .WithActiveScenario("scenario");
        Assert(options.ActiveScenarioId == "scenario", "Topology render options should expose a fluent active scenario helper.");
        Assert(!options.EnableHtmlScenarioControls, "Topology render options should expose a fluent scenario control toggle.");
        Assert(!options.EnableHtmlScenarioPanel, "Topology render options should expose a fluent scenario panel toggle.");
        Assert(options.EnableHtmlScenarioUrlState, "Topology render options should expose a fluent scenario URL-state toggle.");
        Assert(options.WithoutActiveScenario().ActiveScenarioId == null, "Topology render options should expose a fluent active scenario clear helper.");

        AssertThrows<ArgumentException>(() => new TopologyScenario { Id = " " }, "Topology scenario models should reject empty ids close to the caller.");
        AssertThrows<ArgumentException>(() => new TopologyScenario { Id = "bad id" }, "Topology scenario models should reject ids that cannot be used as stable HTML tokens.");
        AssertThrows<ArgumentException>(() => new TopologyScenario { Label = " " }, "Topology scenario models should reject empty labels close to the caller.");
        AssertThrows<ArgumentException>(() => new TopologyScenarioStep { Id = " " }, "Topology scenario step models should reject empty ids close to the caller.");
        AssertThrows<ArgumentOutOfRangeException>(() => new TopologyScenarioStep { Kind = (TopologyScenarioStepKind)999 }, "Topology scenario step models should reject undefined step kinds close to the caller.");
        AssertThrows<ArgumentException>(() => TopologyChart.Create().AddScenario(" ", "Scenario"), "Topology scenarios should reject empty ids close to the caller.");
        AssertThrows<ArgumentException>(() => TopologyChart.Create().AddScenario("bad id", "Scenario"), "Topology scenarios should reject ids that would break scenario membership attributes.");
        AssertThrows<ArgumentException>(() => TopologyChart.Create().AddScenario("scenario", " "), "Topology scenarios should reject empty labels close to the caller.");
        AssertThrows<ArgumentException>(() => new TopologyScenario { Id = "scenario", Label = "Scenario" }.AddNodeStep(" "), "Topology scenario steps should reject empty ids close to the caller.");
        AssertThrows<ArgumentException>(() => new TopologyScenario { Id = "scenario", Label = "Scenario" }.WithMetadata(" ", "value"), "Topology scenario metadata helpers should reject empty keys close to the caller.");
        AssertThrows<ArgumentException>(() => new TopologyScenarioStep { Id = "node", Kind = TopologyScenarioStepKind.Node }.WithMetadata(" ", "value"), "Topology scenario step metadata helpers should reject empty keys close to the caller.");
        AssertThrows<ArgumentException>(() => new TopologyRenderOptions().WithActiveScenario("bad id"), "Topology active scenario helpers should reject ids that cannot be used as stable HTML tokens.");
    }

}
