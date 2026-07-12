using System;
using ChartForgeX.Interactivity.Html;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void GraphExplorerAccessibilityAndFrameTelemetryStayHonest() {
        var html = new HtmlGraphExplorerRenderer().RenderPage(SampleGraphScene());
        Assert(html.Contains("class=\"cfx-graph-canvas\" data-cfx-role=\"graph-canvas\" width=\"960\" height=\"560\" role=\"img\" aria-hidden=\"true\" aria-label=\"Service map\"", StringComparison.Ordinal), "Graph explorer pages should initially hide the inactive Canvas rendering target from assistive technology.");
        Assert(html.Contains("data-cfx-role=\"graph-scene\" width=\"960\" height=\"560\" viewBox=\"0 0 960 560\" role=\"img\" aria-hidden=\"false\"", StringComparison.Ordinal), "Graph explorer pages should expose exactly the initially active SVG surface.");
        Assert(html.Contains("syncRendererAccessibility(root, useCanvas)", StringComparison.Ordinal) && html.Contains("root.querySelector('[data-cfx-role=\"graph-canvas\"]')", StringComparison.Ordinal) && html.Contains("canvas.setAttribute('aria-hidden', useCanvas ? 'false' : 'true')", StringComparison.Ordinal) && html.Contains("svg.setAttribute('aria-hidden', useCanvas ? 'true' : 'false')", StringComparison.Ordinal), "Graph explorer runtime should expose exactly one accessible rendering surface after both LOD and explicitly configured backend decisions.");
        Assert(html.Contains("detail.mode === 'frame'", StringComparison.Ordinal) && html.Contains("recordFramePerformance(root", StringComparison.Ordinal) && html.Contains("cfxGraphPerformanceMaxFrameMs", StringComparison.Ordinal), "Graph explorer runtime should measure browser frame cadence and render duration separately from physics throughput.");
        Assert(html.Contains("physicsBudgetMisses", StringComparison.Ordinal) && html.Contains("frameSample && sampleMs > sampleBudgetMs", StringComparison.Ordinal), "Graph explorer frame-budget status should not treat multi-tick worker physics batches as animation frames.");
        Assert(html.Contains("data-cfx-performance-warmup-frames=\"4\"", StringComparison.Ordinal) && html.Contains("mode: 'warmup'", StringComparison.Ordinal) && html.Contains("const publish = steadyCount === 1", StringComparison.Ordinal) && html.Contains("cfxGraphPerformanceMaxWarmupFrameMs", StringComparison.Ordinal), "Graph explorer runtime should report configurable startup warmup cost separately and always publish the first steady-state frame without permanently failing its budget.");
        Assert(html.Contains("data-cfx-performance-worker-progress-interval=\"4\"", StringComparison.Ordinal) && html.Contains("data-cfx-performance-worker-progress-interval', 4", StringComparison.Ordinal), "Worker layout cadence should use its own performance option instead of reusing telemetry sampling cadence.");
        Assert(html.Contains("root.dataset.cfxGraphRendererActive !== 'canvas'", StringComparison.Ordinal) && html.Contains("syncSvgLayout(root, graphState(root))", StringComparison.Ordinal), "Canvas physics should avoid rewriting the hidden SVG on every frame while synchronizing it before SVG export.");
        Assert(html.Contains("const moving = root.dataset.cfxGraphPhysicsState === 'running'", StringComparison.Ordinal) && html.Contains("node.shadow && !moving", StringComparison.Ordinal) && html.Contains("(!compact && !moving)", StringComparison.Ordinal), "Canvas physics should use a lightweight moving-state pass and restore labels, icons, arrows, and shadows after stabilization.");
        Assert(html.Contains("summary.budgetMissRate", StringComparison.Ordinal) && html.Contains("summary.budgetMisses >= 2 && summary.budgetMissRate > .2", StringComparison.Ordinal) && html.Contains("publish !== false", StringComparison.Ordinal), "Frame budget state should use all observed steady frames and a sustained miss rate while throttling host telemetry events.");
        var negativeWarmup = SampleGraphScene();
        negativeWarmup.Options.Performance.WarmupFrameCount = -1;
        AssertThrows<InvalidOperationException>(() => negativeWarmup.Validate(), "Graph scenes should reject negative performance warmup frame counts.");
        negativeWarmup.Options.Performance.WarmupFrameCount = 0;
        negativeWarmup.Options.Performance.WorkerProgressInterval = 0;
        AssertThrows<InvalidOperationException>(() => negativeWarmup.Validate(), "Graph scenes should reject non-positive worker progress intervals.");
    }
}
