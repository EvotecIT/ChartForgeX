using System;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void InteractivityContractsAreHostNeutral() {
        var options = ChartInteractionOptions.ReportReview();
        Assert(options.HasFeature(ChartInteractionFeatures.Tooltips), "Report-review interactivity should include tooltips.");
        Assert(options.HasFeature(ChartInteractionFeatures.Selection), "Report-review interactivity should include selection.");
        Assert(options.HasFeature(ChartInteractionFeatures.LegendToggles), "Report-review interactivity should include legend toggles.");
        Assert(options.HasFeature(ChartInteractionFeatures.KeyboardNavigation), "Report-review interactivity should include keyboard navigation.");
        Assert(!options.HasFeature(ChartInteractionFeatures.Zoom), "Report-review interactivity should leave zoom as an opt-in host feature.");
        Assert(!options.HasFeature(ChartInteractionFeatures.Scenarios), "Report-review interactivity should leave scenario routes as an opt-in host feature.");

        options.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.SynchronizedCharts | ChartInteractionFeatures.Scenarios | ChartInteractionFeatures.StepPlayback | ChartInteractionFeatures.DeepLinks);
        options.ChartId = "security-posture";
        options.GroupName = "executive-dashboard";
        Assert(options.HasFeature(ChartInteractionFeatures.Zoom) && options.HasFeature(ChartInteractionFeatures.Pan), "Interactivity contracts should model future zoom and pan adapters.");
        Assert(options.HasFeature(ChartInteractionFeatures.SynchronizedCharts), "Interactivity contracts should model synchronized chart groups.");
        Assert(options.HasFeature(ChartInteractionFeatures.Scenarios) && options.HasFeature(ChartInteractionFeatures.StepPlayback) && options.HasFeature(ChartInteractionFeatures.DeepLinks), "Interactivity contracts should model reusable scenario routes, ordered playback, and deep-linked state.");
        Assert(options.ChartId == "security-posture" && options.GroupName == "executive-dashboard", "Interactivity identifiers should preserve trimmed values.");
        AssertThrows<ArgumentException>(() => options.ChartId = " ", "Interactivity chart IDs should reject empty values.");
        AssertThrows<ArgumentException>(() => options.GroupName = "", "Interactivity group names should reject empty values.");

        var scenarioOptions = ChartInteractionOptions.ReportReview()
            .AddScenario("ops.route", "Operations route", scenario => scenario
                .WithColor("#2563EB")
                .WithDescription("Operator review flow")
                .WithMetadata("owner", "noc")
                .AddSeriesStep("latency", "Latency", configure: step => step.WithMetadata("unit", "ms"))
                .AddAnnotationStep("failover-window", "Failover"))
            .WithActiveScenario("ops.route")
            .WithDeepLinkState();
        Assert(scenarioOptions.HasFeature(ChartInteractionFeatures.Scenarios), "Adding a scenario should enable reusable scenario interactivity.");
        Assert(scenarioOptions.HasFeature(ChartInteractionFeatures.StepPlayback), "Adding ordered scenario steps should enable reusable step playback.");
        Assert(scenarioOptions.HasFeature(ChartInteractionFeatures.DeepLinks) && scenarioOptions.EnableDeepLinkState, "Deep-link state should be a reusable opt-in interaction contract.");
        scenarioOptions.WithDeepLinkState(false);
        Assert(!scenarioOptions.HasFeature(ChartInteractionFeatures.DeepLinks) && !scenarioOptions.EnableDeepLinkState, "Disabling deep-link state should clear the reusable deep-link feature flag.");
        scenarioOptions.WithDeepLinkState();
        Assert(scenarioOptions.ActiveScenarioId == "ops.route" && scenarioOptions.Scenarios.Count == 1, "Interactivity options should preserve active scenario state and scenario definitions.");
        Assert(scenarioOptions.Scenarios[0].Steps.Count == 2 && scenarioOptions.Scenarios[0].Steps[0].TargetKind == ChartInteractionTargetKinds.Series, "Reusable scenarios should target adapter-defined chart elements.");
        Assert(scenarioOptions.Scenarios[0].Metadata["owner"] == "noc" && scenarioOptions.Scenarios[0].Steps[0].Metadata["unit"] == "ms", "Reusable scenarios should carry host metadata on scenarios and steps.");
        Assert(scenarioOptions.WithoutActiveScenario().ActiveScenarioId == null, "Interactivity options should expose a fluent active scenario clear helper.");
        AssertThrows<ArgumentException>(() => ChartInteractionOptions.ReportReview().AddScenario("bad id", "Route"), "Reusable scenario ids should reject tokens that cannot round-trip through host adapters.");
        AssertThrows<ArgumentException>(() => ChartInteractionOptions.ReportReview().AddScenario("route", " "), "Reusable scenario labels should reject empty labels.");
        AssertThrows<ArgumentException>(() => new ChartInteractionScenario { Id = "route", Label = "Route" }.AddStep("bad kind", "target"), "Reusable scenario target kinds should stay token-shaped for adapter attributes.");
        AssertThrows<ArgumentException>(() => new ChartInteractionScenario { Id = "route", Label = "Route" }.AddSeriesStep(" "), "Reusable scenario target ids should reject empty values.");
    }

    private static void InteractiveHtmlWrapsSvgWithoutExternalDependencies() {
        var html = SampleChart().ToInteractiveHtmlPage(options => {
            options.PageTitle = "Interactive security posture";
            options.IdScope = "security-posture";
            options.ScriptNonce = "nonce-1";
            options.Interaction.ChartId = "security-posture";
            options.Interaction.GroupName = "dashboard-a";
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush | ChartInteractionFeatures.Export | ChartInteractionFeatures.SynchronizedCharts);
            options.Interaction
                .AddScenario("ops-route", "Ops route", scenario => scenario
                    .WithColor("#2563EB")
                    .WithDescription("Operations review path")
                    .WithMetadata("owner", "noc")
                    .AddSeriesStep("0", "Passed", configure: step => step.WithMetadata("unit", "checks"))
                    .AddSeriesStep("1", "Failed"))
                .WithActiveScenario("ops-route")
                .WithDeepLinkState();
        });

        Assert(html.Contains("<title>Interactive security posture</title>", StringComparison.Ordinal), "Interactive HTML should use the configured page title.");
        Assert(html.Contains("data-cfx-chart-id=\"security-posture\"", StringComparison.Ordinal), "Interactive HTML should expose a stable chart ID.");
        Assert(html.Contains("data-cfx-interaction-group=\"dashboard-a\"", StringComparison.Ordinal), "Interactive HTML should expose a synchronized chart group.");
        Assert(html.Contains("data-cfx-interaction-features=\"", StringComparison.Ordinal) && html.Contains("Zoom", StringComparison.Ordinal) && html.Contains("Pan", StringComparison.Ordinal) && html.Contains("Brush", StringComparison.Ordinal) && html.Contains("SynchronizedCharts", StringComparison.Ordinal), "Interactive HTML should describe enabled host-neutral features.");
        Assert(html.Contains("<script nonce=\"nonce-1\">", StringComparison.Ordinal), "Interactive HTML should support CSP nonces.");
        Assert(html.Contains("class=\"cfx-tooltip\"", StringComparison.Ordinal), "Interactive HTML should include an HTML tooltip surface.");
        Assert(html.Contains("cfx-selected", StringComparison.Ordinal), "Interactive HTML should include selectable-region styling and behavior.");
        Assert(html.Contains("cfx-series-muted", StringComparison.Ordinal), "Interactive HTML should include legend-toggle behavior.");
        Assert(html.Contains("const setSeriesMuted = (root, series, muted)", StringComparison.Ordinal), "Interactive HTML should target SVG series metadata for legend toggles.");
        Assert(html.Contains("data-cfx-zoom=\"in\"", StringComparison.Ordinal) && html.Contains("data-cfx-zoom=\"out\"", StringComparison.Ordinal), "Interactive HTML should include zoom controls when zoom is enabled.");
        Assert(html.Contains("data-cfx-mode-button=\"pan\"", StringComparison.Ordinal), "Interactive HTML should include a pan mode control when pan is enabled.");
        Assert(html.Contains("data-cfx-mode-button=\"brush\"", StringComparison.Ordinal), "Interactive HTML should include a brush mode control when brush is enabled.");
        Assert(html.Contains("data-cfx-export=\"svg\"", StringComparison.Ordinal), "Interactive HTML should include an SVG export control when export is enabled.");
        Assert(html.Contains("data-cfx-export=\"png\"", StringComparison.Ordinal), "Interactive HTML should include a PNG export control when export is enabled.");
        Assert(html.Contains("data-cfx-scenario=\"ops-route\"", StringComparison.Ordinal), "Interactive HTML should include reusable scenario controls when scenarios are configured.");
        Assert(html.Contains("data-cfx-scenario-panel=\"true\"", StringComparison.Ordinal), "Interactive HTML should include a reusable scenario panel when scenarios are configured.");
        Assert(html.Contains("data-cfx-active-scenario=\"ops-route\"", StringComparison.Ordinal), "Interactive HTML should preserve the configured active scenario.");
        Assert(html.Contains("data-cfx-deep-link-state=\"true\"", StringComparison.Ordinal), "Interactive HTML should expose opt-in scenario deep-link state.");
        Assert(html.Contains("data-cfx-scenario-steps=\"[{&quot;index&quot;:0,&quot;targetKind&quot;:&quot;series&quot;,&quot;targetId&quot;:&quot;0&quot;", StringComparison.Ordinal), "Interactive HTML should expose reusable ordered scenario step metadata.");
        Assert(html.Contains("&quot;metadata&quot;:{&quot;unit&quot;:&quot;checks&quot;}", StringComparison.Ordinal), "Interactive HTML should expose reusable scenario step metadata.");
        Assert(html.Contains("data-cfx-scenario-step-control=\"play\"", StringComparison.Ordinal), "Interactive HTML should include reusable scenario playback controls.");
        Assert(html.Contains("data-cfx-scenario-step-control=\"link\"", StringComparison.Ordinal), "Interactive HTML should include reusable scenario link controls when deep-link state is enabled.");
        Assert(html.Contains("cfxscenario", StringComparison.Ordinal) && html.Contains("cfxscenariostep", StringComparison.Ordinal) && html.Contains("cfxscenariolink", StringComparison.Ordinal), "Interactive HTML should publish reusable scenario host events.");
        Assert(html.Contains("scenarioTargetMatches", StringComparison.Ordinal) && html.Contains("data.cfxSeries === step.targetId", StringComparison.Ordinal), "Interactive HTML should map reusable scenario steps onto adapter-defined chart elements.");
        Assert(html.Contains("scenarioTargetCandidates(root, route)", StringComparison.Ordinal) && html.Contains("node.querySelector('.cfx-interactive-region", StringComparison.Ordinal), "Interactive HTML should apply id-targeted scenario classes without muting parent SVG containers.");
        Assert(html.Contains("scenarioUrlParam(root, 'scenario')", StringComparison.Ordinal) && html.Contains("scenarioUrlKey(root, 'scenario')", StringComparison.Ordinal), "Interactive HTML should restore reusable scenario state from chart-scoped opt-in deep links.");
        Assert(html.Contains("const initialScenarioStep = scenarioUrlParam(root, 'scenarioStep')", StringComparison.Ordinal), "Interactive HTML should read the initial legacy scenario step before normalizing query parameters.");
        Assert(html.Contains("(step.targetKind || '') === 'element'", StringComparison.Ordinal), "Interactive HTML should keep id-based scenario matching scoped to element targets.");
        Assert(html.Contains(".cfx-scenario-step-muted", StringComparison.Ordinal), "Interactive HTML should visually mute non-current route members during step playback.");
        Assert(html.Contains("else root.style.removeProperty('--cfx-scenario-color')", StringComparison.Ordinal), "Interactive HTML should clear stale scenario colors when switching to uncolored scenarios.");
        Assert(html.Contains("class=\"cfx-brush-box\"", StringComparison.Ordinal), "Interactive HTML should include a brush selection overlay.");
        Assert(html.Contains("stage.addEventListener('wheel'", StringComparison.Ordinal), "Interactive HTML should support wheel zoom.");
        Assert(html.Contains("root.dataset.cfxBrush", StringComparison.Ordinal) && html.Contains("'cfxbrush'", StringComparison.Ordinal), "Interactive HTML should publish brush selection state for host code.");
        Assert(html.Contains("new XMLSerializer().serializeToString(svg)", StringComparison.Ordinal) && html.Contains("'cfxexport'", StringComparison.Ordinal), "Interactive HTML should export inline SVG without external libraries.");
        Assert(html.Contains("canvas.toBlob((blob)", StringComparison.Ordinal) && html.Contains("image/png", StringComparison.Ordinal), "Interactive HTML should export inline SVG as PNG without external libraries.");
        Assert(html.Contains("new CustomEvent(name", StringComparison.Ordinal) && html.Contains("'cfxviewport'", StringComparison.Ordinal) && html.Contains("'cfxselect'", StringComparison.Ordinal) && html.Contains("'cfxseries'", StringComparison.Ordinal) && html.Contains("'cfxreset'", StringComparison.Ordinal), "Interactive HTML should publish host events for viewport, selection, series toggles, and reset.");
        Assert(html.Contains("new CustomEvent('cfxsync'", StringComparison.Ordinal) && html.Contains("applySync(peer, detail)", StringComparison.Ordinal), "Interactive HTML should synchronize grouped chart state without external libraries.");
        Assert(html.Contains("action: 'viewport'", StringComparison.Ordinal) && html.Contains("action: 'series'", StringComparison.Ordinal) && html.Contains("action: 'selection'", StringComparison.Ordinal) && html.Contains("action: 'brush'", StringComparison.Ordinal), "Interactive HTML should synchronize viewport, series, selection, and brush changes.");
        Assert(html.Contains("<button class=\"cfx-tool\" type=\"button\" data-cfx-reset=\"true\">Reset</button>", StringComparison.Ordinal), "Interactive HTML should include a reset control by default.");
        Assert(html.Contains("<svg", StringComparison.Ordinal), "Interactive HTML should embed the static SVG output.");
        Assert(html.Contains("aria-live=\"polite\"", StringComparison.Ordinal), "Interactive tooltips should expose polite assistive announcements.");
        Assert(!html.Contains("<link", StringComparison.OrdinalIgnoreCase), "Interactive HTML should not reference external stylesheets.");
        Assert(!html.Contains("@import", StringComparison.OrdinalIgnoreCase), "Interactive HTML should not import external stylesheets.");
        var withoutSvgNamespace = html.Replace("http://www.w3.org/2000/svg", string.Empty);
        Assert(!withoutSvgNamespace.Contains("http://", StringComparison.OrdinalIgnoreCase) && !withoutSvgNamespace.Contains("https://", StringComparison.OrdinalIgnoreCase), "Interactive HTML should remain self-contained.");

        var noReset = SampleChart().ToInteractiveHtmlPage(options => {
            options.IncludeResetButton = false;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips;
        });
        Assert(!noReset.Contains("<button class=\"cfx-tool\" type=\"button\" data-cfx-reset=\"true\">Reset</button>", StringComparison.Ordinal), "Interactive HTML should allow reset controls to be suppressed.");
        Assert(!noReset.Contains("data-cfx-zoom=\"in\"", StringComparison.Ordinal), "Interactive HTML should hide zoom controls when zoom is disabled.");
        Assert(!noReset.Contains("data-cfx-mode-button=\"pan\"", StringComparison.Ordinal), "Interactive HTML should hide pan controls when pan is disabled.");
        Assert(!noReset.Contains("data-cfx-mode-button=\"brush\"", StringComparison.Ordinal), "Interactive HTML should hide brush controls when brush is disabled.");
        Assert(!noReset.Contains("<button class=\"cfx-tool\" type=\"button\" data-cfx-export=\"svg\" title=\"Download SVG\">SVG</button>", StringComparison.Ordinal), "Interactive HTML should hide SVG export controls when export is disabled.");
        Assert(!noReset.Contains("<button class=\"cfx-tool\" type=\"button\" data-cfx-export=\"png\" title=\"Download PNG\">PNG</button>", StringComparison.Ordinal), "Interactive HTML should hide PNG export controls when export is disabled.");
        Assert(noReset.Contains("data-cfx-interaction-features=\"Tooltips\"", StringComparison.Ordinal), "Interactive HTML should honor narrowed feature profiles.");

        var noPlayback = SampleChart().ToInteractiveHtmlPage(options => {
            options.Interaction
                .AddScenario("review", "Review", scenario => scenario.AddSeriesStep("0", "Passed"))
                .Disable(ChartInteractionFeatures.StepPlayback);
        });
        Assert(!noPlayback.Contains("<button class=\"cfx-tool\" type=\"button\" data-cfx-scenario-step-control=\"play\"", StringComparison.Ordinal), "Interactive HTML should hide scenario playback controls when StepPlayback is disabled.");

        var noDeepLink = SampleChart().ToInteractiveHtmlPage(options => {
            options.Interaction
                .AddScenario("review", "Review", scenario => scenario.AddSeriesStep("0", "Passed"))
                .Enable(ChartInteractionFeatures.StepPlayback);
        });
        Assert(!noDeepLink.Contains("data-cfx-scenario-step-control=\"link\"", StringComparison.Ordinal), "Interactive HTML should hide scenario link controls when deep-link state is disabled.");

        var escapedScenario = SampleChart().ToInteractiveHtmlPage(options => {
            options.Interaction.AddScenario("escaped", "Escaped", scenario => scenario
                .WithDescription("Line\tTab")
                .WithMetadata("z", "last")
                .WithMetadata("a", "first")
                .AddElementStep("target-id", "Line\tTab"));
        });
        Assert(escapedScenario.Contains("Line\\tTab", StringComparison.Ordinal), "Scenario JSON payloads should escape tabs and other control characters.");
        Assert(escapedScenario.Contains("data-cfx-scenario-metadata=\"{&quot;a&quot;:&quot;first&quot;,&quot;z&quot;:&quot;last&quot;}\"", StringComparison.Ordinal), "Scenario metadata JSON should be deterministic by key order.");
    }

    private static void InteractiveHtmlEscapesHostProvidedAttributes() {
        var html = SampleChart().ToInteractiveHtmlPage(options => {
            options.PageTitle = "A < B & \"C\"";
            options.ScriptNonce = "nonce\" data-bad=\"1";
            options.Interaction.ChartId = "chart\" onmouseover=\"bad";
            options.Interaction.GroupName = "group<one>&\"two\"";
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Export);
        });

        Assert(html.Contains("<title>A &lt; B &amp; &quot;C&quot;</title>", StringComparison.Ordinal), "Interactive HTML should escape page titles.");
        Assert(html.Contains("<script nonce=\"nonce&quot; data-bad=&quot;1\">", StringComparison.Ordinal), "Interactive HTML should escape CSP nonces without creating extra attributes.");
        Assert(html.Contains("data-cfx-chart-id=\"chart&quot; onmouseover=&quot;bad\"", StringComparison.Ordinal), "Interactive HTML should escape host-provided chart IDs.");
        Assert(html.Contains("data-cfx-interaction-group=\"group&lt;one&gt;&amp;&quot;two&quot;\"", StringComparison.Ordinal), "Interactive HTML should escape synchronized group names.");
        Assert(!html.Contains("data-bad=\"1\"", StringComparison.Ordinal), "Escaped nonces should not become additional HTML attributes.");
        Assert(!html.Contains("onmouseover=\"bad\"", StringComparison.Ordinal), "Escaped chart IDs should not become event handler attributes.");
        Assert(html.Contains("<button class=\"cfx-tool\" type=\"button\" data-cfx-zoom=\"in\" title=\"Zoom in\">Zoom +</button>", StringComparison.Ordinal), "Typed toolbar rendering should preserve zoom button markup.");
        Assert(html.Contains("<button class=\"cfx-tool\" type=\"button\" data-cfx-export=\"png\" title=\"Download PNG\">PNG</button>", StringComparison.Ordinal), "Typed toolbar rendering should preserve PNG export button markup.");
    }

    private static void InteractiveHtmlDashboardSynchronizesMultipleCharts() {
        var html = new[] { SampleChart(), SampleChart() }.ToInteractiveHtmlDashboardPage(options => {
            options.PageTitle = "Executive interactive dashboard";
            options.IdScope = "exec-dashboard";
            options.Columns = 2;
            options.ScriptNonce = "nonce-dashboard";
            options.Interaction.GroupName = "exec-review";
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush | ChartInteractionFeatures.Export | ChartInteractionFeatures.SynchronizedCharts);
            options.Interaction
                .AddScenario("ops-route", "Ops route", scenario => scenario
                    .WithColor("#2563EB")
                    .WithDescription("Operations review path")
                    .AddSeriesStep("0", "Passed")
                    .AddSeriesStep("1", "Failed"))
                .WithActiveScenario("ops-route")
                .WithDeepLinkState();
        });

        Assert(html.Contains("<title>Executive interactive dashboard</title>", StringComparison.Ordinal), "Interactive dashboards should use the configured page title.");
        Assert(html.Contains("<script nonce=\"nonce-dashboard\">", StringComparison.Ordinal), "Interactive dashboards should support CSP nonces.");
        Assert(html.Contains("class=\"cfx-dashboard\"", StringComparison.Ordinal), "Interactive dashboards should render a grouped dashboard surface.");
        Assert(html.Contains("--cfx-dashboard-columns:2", StringComparison.Ordinal), "Interactive dashboards should honor configured column counts.");
        Assert(CountOccurrences(html, "class=\"cfx-interactive-chart\"") == 2, "Interactive dashboards should render one interactive chart section per chart.");
        Assert(html.Contains("data-cfx-chart-id=\"exec-dashboard-1\"", StringComparison.Ordinal) && html.Contains("data-cfx-chart-id=\"exec-dashboard-2\"", StringComparison.Ordinal), "Interactive dashboards should assign deterministic child chart IDs.");
        Assert(CountOccurrences(html, "data-cfx-interaction-group=\"exec-review\"") == 2, "Interactive dashboards should place every child chart in the shared interaction group.");
        Assert(html.Contains("new CustomEvent('cfxsync'", StringComparison.Ordinal) && html.Contains("applySync(peer, detail)", StringComparison.Ordinal), "Interactive dashboards should include grouped synchronization runtime.");
        Assert(CountOccurrences(html, "data-cfx-scenario=\"ops-route\"") == 2 && CountOccurrences(html, "data-cfx-active-scenario=\"ops-route\"") == 2, "Interactive dashboards should copy reusable scenarios into each synchronized child chart.");
        Assert(CountOccurrences(html, "data-cfx-deep-link-state=\"true\"") == 2, "Interactive dashboards should preserve reusable scenario deep-link state for each child chart.");
        Assert(html.Contains("setScenario(root, detail.scenarioId || '', false, false)", StringComparison.Ordinal) && html.Contains("if (sync !== false) emitSync(root, { action: 'scenario', scenarioId: route.id })", StringComparison.Ordinal), "Interactive scenario synchronization should not rebroadcast peer-applied scenario state.");
        Assert(html.Contains("if (detail.scenarioId && root.dataset.cfxActiveScenario !== detail.scenarioId) setScenario(root, detail.scenarioId, false, false)", StringComparison.Ordinal), "Interactive scenario step synchronization should apply the declared scenario before focusing the synced step.");
        Assert(html.Contains("data-cfx-export=\"svg\"", StringComparison.Ordinal) && html.Contains("data-cfx-export=\"png\"", StringComparison.Ordinal), "Interactive dashboards should include enabled per-chart export controls.");
        Assert(!html.Contains("<link", StringComparison.OrdinalIgnoreCase), "Interactive dashboards should not reference external stylesheets.");
        Assert(!html.Contains("@import", StringComparison.OrdinalIgnoreCase), "Interactive dashboards should not import external stylesheets.");
        var withoutSvgNamespace = html.Replace("http://www.w3.org/2000/svg", string.Empty);
        Assert(!withoutSvgNamespace.Contains("http://", StringComparison.OrdinalIgnoreCase) && !withoutSvgNamespace.Contains("https://", StringComparison.OrdinalIgnoreCase), "Interactive dashboards should remain self-contained.");
        AssertThrows<ArgumentException>(() => new HtmlInteractiveDashboardRenderer().RenderPage(Array.Empty<ChartForgeX.Core.Chart>()), "Interactive dashboards should reject empty chart sets.");
    }
}
