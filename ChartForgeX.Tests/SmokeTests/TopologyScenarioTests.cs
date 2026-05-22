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

    private static void TopologyScenarioMotionRendersSvgGifAndApng() {
        var chart = TopologyChart.Create()
            .WithId("motion-scenario")
            .WithViewport(420, 220, 20)
            .WithLegend(null)
            .AddNode("a", "A", 40, 80)
            .AddNode("b", "B", 220, 80)
            .AddEdge("a-b", "a", "b", "flow", TopologyEdgeKind.DataFlow, TopologyHealthStatus.Healthy)
            .AddScenario("route", "Route", scenario => scenario
                .WithColor("#2563EB")
                .AddNodeStep("a")
                .AddEdgeStep("a-b")
                .AddNodeStep("b"));

        var options = new TopologyRenderOptions { IncludeLegend = false }
            .WithMotion(new TopologyMotionOptions {
                ScenarioId = "route",
                DurationSeconds = 1,
                FramesPerSecond = 4,
                MarkerRadius = 4,
                Progress = 0.375
            });
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-cfx-role=\"topology-motion\"", StringComparison.Ordinal), "Topology SVG motion should emit a script-free motion layer.");
        Assert(svg.Contains("<animate", StringComparison.Ordinal) && svg.Contains("attributeName=\"stroke-dashoffset\"", StringComparison.Ordinal), "Topology SVG motion should use native SVG animation elements.");
        Assert(svg.Contains("data-cfx-role=\"topology-motion-tour-path\"", StringComparison.Ordinal), "Topology SVG motion should build one reusable tour path across the animated route.");
        Assert(svg.Contains("<animateMotion", StringComparison.Ordinal) && svg.Contains("href=\"#motion-scenario-motion-tour-route\"", StringComparison.Ordinal) && svg.Contains("xlink:href=\"#motion-scenario-motion-tour-route\"", StringComparison.Ordinal), "Topology SVG motion should render one marker that follows the generated tour path.");
        Assert(svg.IndexOf("data-cfx-role=\"topology-motion-route\"", StringComparison.Ordinal) < svg.IndexOf("data-cfx-role=\"topology-node\"", StringComparison.Ordinal), "Topology SVG route pulses should render under node surfaces.");
        Assert(svg.IndexOf("data-cfx-role=\"topology-motion-marker\"", StringComparison.Ordinal) > svg.IndexOf("data-cfx-role=\"topology-node\"", StringComparison.Ordinal), "Topology SVG moving markers should render above node surfaces to match PNG frame visibility.");
        var gif = chart.ToGif(options);
        Assert(gif.Length > 128, "Topology motion GIF should render encoded frames.");
        Assert(Math.Abs(options.Motion!.Progress - 0.375) < 0.0001, "Topology GIF export should not leak sampled frame progress back into caller-owned motion options.");
        Assert(gif[0] == (byte)'G' && gif[1] == (byte)'I' && gif[2] == (byte)'F', "Topology motion GIF should use the GIF header.");
        Assert(System.Text.Encoding.ASCII.GetString(gif).Contains("NETSCAPE2.0", StringComparison.Ordinal), "Looping topology GIFs should include the Netscape loop extension.");
        using var gifStream = new System.IO.MemoryStream();
        chart.WriteGif(gifStream, options);
        Assert(gifStream.ToArray().SequenceEqual(gif), "Topology motion GIF stream export should match byte-array export.");
        var gifPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chartforgex-motion-" + Guid.NewGuid().ToString("N") + ".gif");
        try {
            chart.SaveGif(gifPath, options);
            Assert(System.IO.File.ReadAllBytes(gifPath).SequenceEqual(gif), "Topology motion GIF file export should match byte-array export.");
        } finally {
            if (System.IO.File.Exists(gifPath)) System.IO.File.Delete(gifPath);
        }

        AssertThrows<ArgumentNullException>(() => chart.WriteGif(null!, options), "Topology motion GIF stream export should reject null streams.");
        var apng = chart.ToApng(options);
        Assert(apng.Length > 128, "Topology motion APNG should render encoded frames.");
        Assert(apng[0] == 137 && apng[1] == 80 && apng[2] == 78 && apng[3] == 71, "Topology motion APNG should use the PNG signature.");
        var apngAscii = System.Text.Encoding.ASCII.GetString(apng);
        Assert(apngAscii.Contains("acTL", StringComparison.Ordinal) && apngAscii.Contains("fcTL", StringComparison.Ordinal) && apngAscii.Contains("fdAT", StringComparison.Ordinal), "Topology motion APNG should include animation control and frame data chunks.");
        using var apngStream = new System.IO.MemoryStream();
        chart.WriteApng(apngStream, options);
        Assert(apngStream.ToArray().SequenceEqual(apng), "Topology motion APNG stream export should match byte-array export.");
        var apngPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chartforgex-motion-" + Guid.NewGuid().ToString("N") + ".apng");
        try {
            chart.SaveApng(apngPath, options);
            Assert(System.IO.File.ReadAllBytes(apngPath).SequenceEqual(apng), "Topology motion APNG file export should match byte-array export.");
        } finally {
            if (System.IO.File.Exists(apngPath)) System.IO.File.Delete(apngPath);
        }

        AssertThrows<ArgumentNullException>(() => chart.WriteApng(null!, options), "Topology motion APNG stream export should reject null streams.");

        var explicitEdgeOptions = new TopologyRenderOptions { IncludeLegend = false }
            .WithMotion(TopologyMotionOptions.RoutePulseForEdges("a-b"));
        var explicitEdgeSvg = chart.ToSvg(explicitEdgeOptions);
        Assert(explicitEdgeSvg.Contains("data-cfx-motion-source=\"explicit-edges\"", StringComparison.Ordinal), "Topology motion should animate explicit edge ids without requiring a scenario.");
        Assert(explicitEdgeSvg.Contains("data-cfx-role=\"topology-motion-node\"", StringComparison.Ordinal), "Explicit edge motion should expose endpoint node pulses through the same reusable motion layer.");

        var noScenarioChart = TopologyChart.Create()
            .WithId("motion-explicit-edge")
            .WithViewport(420, 220, 20)
            .WithLegend(null)
            .AddNode("a", "A", 40, 80)
            .AddNode("b", "B", 220, 80)
            .AddEdge("a-b", "a", "b", "flow", TopologyEdgeKind.DataFlow, TopologyHealthStatus.Healthy);
        var noScenarioSvg = noScenarioChart.ToSvg(explicitEdgeOptions);
        Assert(noScenarioSvg.Contains("data-cfx-motion-source=\"explicit-edges\"", StringComparison.Ordinal), "Explicit edge motion should render even when the topology has no scenarios.");
        Assert(noScenarioChart.ToGif(explicitEdgeOptions).Length > 128, "Explicit edge motion should export GIF frames even when the topology has no scenarios.");
        Assert(noScenarioChart.ToApng(explicitEdgeOptions).Length > 128, "Explicit edge motion should export APNG frames even when the topology has no scenarios.");
        AssertThrows<InvalidOperationException>(() => noScenarioChart.ToGif(new TopologyRenderOptions { IncludeLegend = false }), "Topology GIF export should fail clearly when no scenario or explicit edge route can be animated.");
        AssertThrows<InvalidOperationException>(() => noScenarioChart.ToApng(new TopologyRenderOptions { IncludeLegend = false }), "Topology APNG export should fail clearly when no scenario or explicit edge route can be animated.");

        var scenarioOptions = TopologyMotionOptions.RoutePulseForScenario(" route ");
        Assert(scenarioOptions.ScenarioId == "route", "Topology motion scenario factories should trim stable ids.");
        scenarioOptions
            .WithDuration(2)
            .WithFrameRate(4)
            .WithFrameLimit(4);
        AssertThrows<ArgumentOutOfRangeException>(() => chart.ToGif(new TopologyRenderOptions { IncludeLegend = false }.WithMotion(scenarioOptions)), "Topology GIF export should reject frame counts above the configured raster frame limit.");
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
        AssertThrows<ArgumentException>(() => TopologyMotionOptions.RoutePulseForScenario("bad id"), "Topology motion scenario helpers should reject ids that cannot be used as stable HTML tokens.");
        AssertThrows<ArgumentException>(() => TopologyMotionOptions.RoutePulseForEdges(), "Topology motion explicit edge helpers should require at least one edge id.");
        AssertThrows<ArgumentException>(() => TopologyMotionOptions.RoutePulseForEdges(" "), "Topology motion explicit edge helpers should reject empty edge ids.");
        var motion = TopologyMotionOptions.RoutePulseForEdges("edge")
            .WithDuration(2)
            .WithFrameRate(8)
            .WithFrameLimit(32)
            .WithMarker(6, " #2563EB ")
            .AtProgress(0.5)
            .WithEndpointPulses(false);
        Assert(motion.DurationSeconds == 2 && motion.FramesPerSecond == 8 && motion.MaximumRasterFrames == 32 && motion.MarkerRadius == 6 && motion.MarkerColor == "#2563EB" && Math.Abs(motion.Progress - 0.5) < 0.0001 && !motion.PulseRouteEndpoints, "Topology motion fluent helpers should tune reusable route animation options close to the caller.");
        Assert(motion.WithMarkerColor(" ").MarkerColor == null, "Topology motion marker color helpers should clear empty overrides.");
        AssertThrows<ArgumentOutOfRangeException>(() => TopologyMotionOptions.RoutePulseForEdges("edge").WithDuration(0), "Topology motion fluent duration helper should reject invalid values close to the caller.");
        AssertThrows<ArgumentOutOfRangeException>(() => TopologyMotionOptions.RoutePulseForEdges("edge").WithFrameRate(double.PositiveInfinity), "Topology motion fluent frame-rate helper should reject invalid values close to the caller.");
        AssertThrows<ArgumentOutOfRangeException>(() => TopologyMotionOptions.RoutePulseForEdges("edge").WithFrameLimit(0), "Topology motion fluent frame-limit helper should reject invalid values close to the caller.");
        AssertThrows<ArgumentOutOfRangeException>(() => TopologyMotionOptions.RoutePulseForEdges("edge").WithMarker(double.NaN), "Topology motion fluent marker helper should reject invalid values close to the caller.");
        AssertThrows<ArgumentOutOfRangeException>(() => TopologyMotionOptions.RoutePulseForEdges("edge").AtProgress(1.1), "Topology motion fluent progress helper should reject values outside the sampled progress range.");
        AssertThrows<ArgumentOutOfRangeException>(() => new TopologyRenderOptions().WithMotion(new TopologyMotionOptions { MaximumRasterFrames = 0 }), "Topology motion options should reject non-positive raster frame limits.");
    }

}
