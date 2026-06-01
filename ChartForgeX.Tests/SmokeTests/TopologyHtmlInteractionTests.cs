using System;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyHtmlPagesExposeScenarioInteractions() {
        var chart = CreateSampleTopologyChart()
            .AddScenario("primary", "Primary flow", scenario => scenario
                .WithColor("#2563EB")
                .WithDescription("Nominal request path")
                .WithMetadata("owner", "routing")
                .WithMetadata("request.type", "interactive")
                .AddNodeStep("amer-hub", "Start")
                .AddEdgeStep("amer-emea", "WAN", configure: step => step.WithMetadata("protocol", "https"))
                .AddNodeStep("emea-hub", "Policy"))
            .AddScenario("failover", "Failover flow", scenario => scenario
                .WithColor("#EF4444")
                .AddNodeStep("emea-hub")
                .AddEdgeStep("emea-tr")
                .AddNodeStep("tr-branch"));

        var staticHtml = chart.ToHtmlPage(new TopologyRenderOptions { EnableHtmlInteractions = false });
        Assert(staticHtml.Contains("data-cfx-scenario-count=\"2\"", StringComparison.Ordinal), "Static topology SVG should expose scenario count as metadata.");
        Assert(staticHtml.Contains("data-cfx-scenario-ids=\"primary failover\"", StringComparison.Ordinal), "Static topology SVG should expose a root scenario id index.");
        Assert(staticHtml.Contains("data-cfx-scenarios=\"[{&quot;id&quot;:&quot;primary&quot;", StringComparison.Ordinal), "Static topology SVG should expose compact scenario summaries for host route panels.");
        Assert(staticHtml.Contains("data-scenario-ids=\"primary\"", StringComparison.Ordinal), "Static topology SVG should expose scenario membership on reused topology elements.");
        Assert(staticHtml.Contains("data-scenario-step-indices=\"primary:0,1\"", StringComparison.Ordinal), "Static topology SVG should expose ordered scenario step indices on participating elements.");
        Assert(staticHtml.Contains("data-cfx-scenario-controls=\"false\"", StringComparison.Ordinal), "Static topology HTML should not render scenario controls.");
        Assert(staticHtml.Contains("data-cfx-scenario-panel=\"false\"", StringComparison.Ordinal), "Static topology HTML should not render scenario panels.");
        Assert(staticHtml.Contains("data-cfx-scenario-url-state=\"false\"", StringComparison.Ordinal), "Static topology HTML should not enable query-string scenario state.");
        Assert(!staticHtml.Contains("data-cfx-topology-scenario=\"primary\"", StringComparison.Ordinal), "Static topology HTML should omit scenario picker controls.");

        var html = chart.ToHtmlPage(new TopologyRenderOptions { EnableHtmlInteractions = true, ActiveScenarioId = "failover" });
        Assert(html.Contains("data-cfx-scenario-controls=\"true\"", StringComparison.Ordinal), "Interactive topology HTML should enable scenario controls when scenarios are present.");
        Assert(html.Contains("data-cfx-scenario-panel=\"true\"", StringComparison.Ordinal), "Interactive topology HTML should enable scenario panels when scenarios are present.");
        Assert(html.Contains("data-cfx-scenario-url-state=\"false\"", StringComparison.Ordinal), "Interactive topology HTML should keep query-string scenario state opt-in.");
        Assert(html.Contains("data-cfx-active-scenario=\"failover\"", StringComparison.Ordinal), "Interactive topology HTML should preserve the requested active scenario id.");
        Assert(!html.Contains("cfx-topology--dimmed", StringComparison.Ordinal), "Interactive topology HTML should not bake active-scenario dimming into the embedded SVG because the All control must restore the full-color topology.");
        Assert(html.Contains("data-cfx-topology-scenario=\"primary\"", StringComparison.Ordinal), "Interactive topology HTML should render a scenario picker button.");
        Assert(html.Contains("data-cfx-topology-scenario-panel=\"true\"", StringComparison.Ordinal), "Interactive topology HTML should render a compact scenario route panel.");
        Assert(html.Contains("data-cfx-scenario-panel-title=\"true\"", StringComparison.Ordinal), "Scenario panels should expose a title target for route detail updates.");
        Assert(html.Contains("data-cfx-scenario-panel-steps=\"true\"", StringComparison.Ordinal), "Scenario panels should expose a step-list target for route detail updates.");
        Assert(html.Contains("data-cfx-scenario-step-controls=\"true\"", StringComparison.Ordinal), "Scenario panels should expose route step playback controls.");
        Assert(html.Contains("data-cfx-scenario-step-control=\"play\"", StringComparison.Ordinal), "Scenario panels should include a play control for route playback.");
        Assert(!html.Contains("data-cfx-scenario-step-control=\"link\"", StringComparison.Ordinal), "Scenario panels should hide copy-link controls until query-string scenario state is enabled.");
        Assert(html.Contains(">Failover flow</div>", StringComparison.Ordinal), "Scenario panels should server-render the active scenario title before JavaScript updates run.");
        Assert(html.Contains("data-cfx-scenario-step-id=\"emea-hub\"", StringComparison.Ordinal), "Scenario panels should server-render active route steps before JavaScript updates run.");
        Assert(html.Contains("data-cfx-scenario-step-index=\"1\" role=\"button\" tabindex=\"0\"", StringComparison.Ordinal), "Scenario panel steps should be directly keyboard-focusable.");
        Assert(html.Contains("data-cfx-scenario-color=\"#2563EB\"", StringComparison.Ordinal), "Scenario picker controls should expose scenario accent colors.");
        Assert(html.Contains("data-cfx-scenario-label=\"Primary flow\"", StringComparison.Ordinal), "Scenario picker controls should expose scenario labels for host event payloads.");
        Assert(html.Contains("data-cfx-scenario-description=\"Nominal request path\"", StringComparison.Ordinal), "Scenario picker controls should expose scenario descriptions for host event payloads.");
        Assert(html.Contains("data-cfx-scenario-step-count=\"3\"", StringComparison.Ordinal), "Scenario picker controls should expose scenario step counts for host event payloads.");
        Assert(html.Contains("data-cfx-scenario-steps=\"[{&quot;index&quot;:0,&quot;kind&quot;:&quot;Node&quot;,&quot;id&quot;:&quot;amer-hub&quot;,&quot;label&quot;:&quot;Start&quot;}", StringComparison.Ordinal), "Scenario picker controls should expose ordered step details as escaped JSON.");
        Assert(html.Contains("&quot;metadata&quot;:{&quot;protocol&quot;:&quot;https&quot;}", StringComparison.Ordinal), "Scenario step details should include optional step metadata for host route inspectors.");
        Assert(html.Contains("data-cfx-scenario-metadata=\"{&quot;owner&quot;:&quot;routing&quot;,&quot;request.type&quot;:&quot;interactive&quot;}\"", StringComparison.Ordinal), "Scenario picker controls should expose scenario metadata as escaped JSON.");
        Assert(html.Contains("scenarioStepIndices(edge, scenarioId).join(' ')", StringComparison.Ordinal), "Scenario interactions should project active step indices onto highlighted topology elements.");
        Assert(html.Contains("metadata: route.metadata, steps: route.steps, nodeIds: Array.from(route.nodeIds)", StringComparison.Ordinal), "Scenario events should include metadata and ordered step details alongside resolved node and edge ids.");
        Assert(html.Contains("const scenarioRoute = scenarioId =>", StringComparison.Ordinal), "Scenario interactions should reuse one resolved route model for panels, preview, and active selection.");
        Assert(html.Contains("findScenarioSummary(scenarioId)", StringComparison.Ordinal), "Scenario interactions should resolve routes from embedded SVG metadata when built-in picker buttons are hidden.");
        Assert(html.Contains("renderScenarioPanel(route)", StringComparison.Ordinal), "Scenario interactions should keep the route panel synchronized with the active scenario.");
        Assert(html.Contains("cfx-topology-html-scenario-preview", StringComparison.Ordinal), "Scenario picker hover should preview routes without changing the active scenario.");
        Assert(html.Contains("cfx-topology-scenario-preview", StringComparison.Ordinal), "Scenario preview should dispatch host-friendly events.");
        Assert(html.Contains("button.addEventListener('pointerenter', () => previewScenario(scenarioId))", StringComparison.Ordinal), "Scenario picker hover should preview routes with pointer input.");
        Assert(html.Contains("button.addEventListener('focus', () => previewScenario(scenarioId))", StringComparison.Ordinal), "Scenario picker focus should preview routes with keyboard input.");
        Assert(html.Contains("const setScenarioStep = (stepIndex, keepPlaying, emit = true, sync = true) =>", StringComparison.Ordinal), "Scenario interactions should let hosts focus individual route steps.");
        Assert(html.Contains("cfx-topology-html-scenario-step-active", StringComparison.Ordinal), "Scenario step focus should emphasize the current route step without drawing duplicate overlays.");
        Assert(html.Contains("cfx-topology-scenario-step", StringComparison.Ordinal), "Scenario step focus should dispatch host-friendly events.");
        Assert(html.Contains("cfx-topology-set-scenario-step", StringComparison.Ordinal), "Scenario step focus should allow hosts to drive the current step.");
        Assert(html.Contains("cfx-topology-scenario-link", StringComparison.Ordinal) && html.Contains("navigator.clipboard.writeText", StringComparison.Ordinal), "Scenario link controls should publish and copy deep links without external dependencies.");
        Assert(html.Contains("stepsList.addEventListener('click'", StringComparison.Ordinal) && html.Contains("stepsList.addEventListener('keydown'", StringComparison.Ordinal), "Scenario panel steps should support direct pointer and keyboard activation.");
        Assert(html.Contains("window.setInterval", StringComparison.Ordinal), "Scenario panels should support lightweight route playback.");
        Assert(html.Contains("findScenarioButton(scenarioId)", StringComparison.Ordinal), "Scenario interactions should ignore unknown scenario ids instead of muting the whole chart.");
        Assert(html.Contains("label: route.label", StringComparison.Ordinal), "Scenario events should include host-friendly scenario metadata.");
        Assert(html.Contains("cfx-topology-scenario", StringComparison.Ordinal), "Interactive topology HTML should dispatch host-friendly scenario events.");
        Assert(html.Contains("cfx-topology-set-scenario", StringComparison.Ordinal), "Interactive topology HTML should allow hosts to activate scenarios.");
        Assert(html.Contains("cfx-topology-clear-scenario", StringComparison.Ordinal), "Interactive topology HTML should allow hosts to clear active scenarios.");
        Assert(html.Contains("wrapper.removeAttribute('data-cfx-active-scenarios')", StringComparison.Ordinal), "Clearing an active scenario should also clear stale multi-route checkbox state.");
        Assert(html.Contains("const initialScenarioStep = scenarioUrlParam('scenarioStep')", StringComparison.Ordinal), "Interactive topology HTML should read the initial legacy route step before normalizing query parameters.");
        Assert(html.Contains("setScenario(initialScenario, false, false)", StringComparison.Ordinal) && html.Contains("setScenarioStep(initialScenarioStep, false, false, false)", StringComparison.Ordinal), "Interactive topology HTML should restore initial scenario state without emitting startup sync or host events.");
        Assert(html.Contains("if (!scenarioId) return null", StringComparison.Ordinal), "Scenario step controls should disable when All routes are visible.");
        Assert(html.Contains("querySelectorAll('[data-cfx-role=\"topology-edge-label\"]')", StringComparison.Ordinal), "Scenario interactions should dim edge labels with their route edges.");
        Assert(html.Contains("querySelectorAll('[data-cfx-role=\"topology-node-status\"]')", StringComparison.Ordinal), "Scenario interactions should dim node status badges with their route nodes.");
        Assert(html.Contains("querySelectorAll('[data-cfx-role=\"topology-group\"]')", StringComparison.Ordinal), "Scenario interactions should dim group shells outside the active route.");
        Assert(html.Contains("emitSync('scenario-step'", StringComparison.Ordinal) && html.Contains("detail.action === 'scenario-step'", StringComparison.Ordinal), "Topology scenario steps should synchronize across grouped wrappers.");
        Assert(html.Contains("data-scenario-ids=\"primary failover\"", StringComparison.Ordinal) || html.Contains("data-scenario-ids=\"failover primary\"", StringComparison.Ordinal), "Shared topology elements should expose all scenario memberships.");
        Assert(html.Contains("data-scenario-step-indices=\"primary:1,2 failover:0,1\"", StringComparison.Ordinal), "Shared topology elements should expose step order for each scenario membership.");
        Assert(html.Contains("cfx-topology-html-scenario-muted", StringComparison.Ordinal), "Scenario interactions should dim non-participating elements instead of drawing every path at once.");
        Assert(html.Contains("cfx-topology-html-scenario-active", StringComparison.Ordinal), "Scenario interactions should mark participating elements for route emphasis.");

        var fallbackHtml = chart.ToHtmlPage(new TopologyRenderOptions { EnableHtmlInteractions = true, ActiveScenarioId = "missing" });
        Assert(!fallbackHtml.Contains("data-cfx-active-scenario=\"primary\"", StringComparison.Ordinal), "Interactive topology HTML should default to All when no requested scenario resolves.");

        var urlStateHtml = chart.ToHtmlPage(new TopologyRenderOptions { EnableHtmlInteractions = true, EnableHtmlScenarioUrlState = true });
        Assert(urlStateHtml.Contains("data-cfx-scenario-url-state=\"true\"", StringComparison.Ordinal), "Interactive topology HTML should expose opt-in query-string scenario state.");
        Assert(urlStateHtml.Contains("scenarioUrlParam('scenario')", StringComparison.Ordinal), "Scenario URL state should allow links to open a specific scenario.");
        Assert(urlStateHtml.Contains("scenarioUrlParam('scenarioStep')", StringComparison.Ordinal), "Scenario URL state should allow links to open a specific route step.");
        Assert(urlStateHtml.Contains("data-cfx-scenario-step-control=\"link\"", StringComparison.Ordinal), "Scenario panels should include copy-link controls when deep-link route state is enabled.");
        Assert(urlStateHtml.Contains("scenarioUrlKey('scenario')", StringComparison.Ordinal) && urlStateHtml.Contains("params.get(name)", StringComparison.Ordinal), "Scenario URL state should use chart-scoped query keys while still reading legacy single-chart links.");
        Assert(urlStateHtml.Contains("syncScenarioUrl(route.scenarioId, index)", StringComparison.Ordinal), "Scenario step focus should keep URL state synchronized.");
        Assert(urlStateHtml.Contains("syncScenarioUrl(scenarioId, '')", StringComparison.Ordinal), "Scenario selection should keep URL state synchronized.");

        var panelDisabledHtml = chart.ToHtmlPage(new TopologyRenderOptions { EnableHtmlInteractions = true, EnableHtmlScenarioPanel = false });
        Assert(panelDisabledHtml.Contains("data-cfx-scenario-panel=\"false\"", StringComparison.Ordinal), "Interactive topology HTML should allow hosts to disable the built-in scenario panel.");
        Assert(!panelDisabledHtml.Contains("data-cfx-topology-scenario-panel=\"true\"", StringComparison.Ordinal), "Disabled scenario panels should omit panel markup while keeping scenario controls available.");

        var pickerDisabledHtml = chart.ToHtmlPage(new TopologyRenderOptions { EnableHtmlInteractions = true, EnableHtmlScenarioControls = false });
        Assert(pickerDisabledHtml.Contains("data-cfx-scenario-controls=\"true\"", StringComparison.Ordinal), "Interactive topology HTML should keep scenario runtime enabled when built-in picker controls are hidden.");
        Assert(!pickerDisabledHtml.Contains("data-cfx-topology-scenario=\"primary\"", StringComparison.Ordinal), "Hidden scenario controls should omit only the built-in picker buttons.");
        Assert(pickerDisabledHtml.Contains("cfx-topology-set-scenario", StringComparison.Ordinal), "Hidden scenario controls should keep host-driven scenario events active.");
        Assert(pickerDisabledHtml.Contains("findScenarioSummary(scenarioId)", StringComparison.Ordinal) && pickerDisabledHtml.Contains("data-cfx-scenarios=", StringComparison.Ordinal), "Hidden scenario controls should still resolve route metadata from the embedded topology SVG.");

        var checkboxHtml = chart.ToHtmlPage(new TopologyRenderOptions { EnableHtmlInteractions = true, HtmlScenarioControlMode = TopologyHtmlScenarioControlMode.Checkboxes, EnableHtmlFullscreenControl = true, HtmlControlPlacement = TopologyHtmlControlPlacement.LeftRail });
        Assert(checkboxHtml.Contains("data-cfx-scenario-control-mode=\"checkboxes\"", StringComparison.Ordinal), "Interactive topology HTML should expose checkbox scenario mode.");
        Assert(checkboxHtml.Contains("data-cfx-topology-scenario-toggle=\"primary\"", StringComparison.Ordinal), "Checkbox scenario mode should render independent route toggles.");
        Assert(checkboxHtml.Contains("data-cfx-fullscreen-control=\"true\"", StringComparison.Ordinal), "Interactive topology HTML should expose opt-in fullscreen controls.");
        Assert(checkboxHtml.Contains("data-cfx-topology-fullscreen=\"true\"", StringComparison.Ordinal), "Fullscreen controls should render a host-visible control.");
        Assert(checkboxHtml.Contains("data-cfx-controls-placement=\"left-rail\"", StringComparison.Ordinal), "Topology controls should support dashboard rail placement.");
        Assert(checkboxHtml.Contains("const setScenarioFilters = (scenarioIds, emit = true, sync = true) =>", StringComparison.Ordinal), "Checkbox scenario mode should support enabling multiple route filters.");
        Assert(checkboxHtml.Contains("cfx-topology-scenario-filter", StringComparison.Ordinal), "Checkbox scenario mode should dispatch host-friendly route-filter events.");
        Assert(checkboxHtml.Contains("cfx-topology-set-scenario-filters", StringComparison.Ordinal), "Checkbox scenario mode should allow hosts to drive route filters.");
        Assert(checkboxHtml.Contains("input.checked = attr(input, 'data-cfx-topology-scenario-toggle') === scenarioId", StringComparison.Ordinal), "Singular scenario updates should keep checkbox route controls visually synchronized.");
        Assert(checkboxHtml.Contains("const toggleFullscreen = () =>", StringComparison.Ordinal), "Fullscreen controls should be handled by the topology runtime.");
        Assert(checkboxHtml.Contains("fullscreenchange", StringComparison.Ordinal) && checkboxHtml.Contains("emitFullscreenState", StringComparison.Ordinal), "Fullscreen state events should be emitted after browser fullscreen state changes.");
        Assert(checkboxHtml.Contains("scenarioIdTokens(initialScenario)", StringComparison.Ordinal), "Checkbox scenario mode should restore multi-route filters from URL state.");
        Assert(checkboxHtml.Contains("initialScenarioStep && (scenarioControlMode !== 'checkboxes' || attr(wrapper, 'data-cfx-active-scenario'))", StringComparison.Ordinal), "Checkbox scenario mode should restore route step deep links only when the URL names one active route.");
        Assert(checkboxHtml.Contains("syncScenarioUrl(routes.map(route => route.scenarioId).join(','), '')", StringComparison.Ordinal), "Checkbox scenario mode should serialize route filters into URL state when enabled.");
    }

    private static void TopologyHtmlPagesExposeSelectionInteractions() {
        var defaultHtml = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { IncludeLegend = false });
        Assert(defaultHtml.Contains("data-cfx-interactive=\"false\"", StringComparison.Ordinal), "Topology HTML pages should be static by default.");
        Assert(!defaultHtml.Contains("new CustomEvent('cfx-topology-select'", StringComparison.Ordinal), "Default topology HTML pages should omit the interaction script.");
        Assert(defaultHtml.Contains("linear-gradient(180deg", StringComparison.Ordinal), "Topology HTML pages should use the shared polished page surface.");
        Assert(defaultHtml.Contains("-webkit-font-smoothing:antialiased", StringComparison.Ordinal) && defaultHtml.Contains("text-rendering:geometricPrecision", StringComparison.Ordinal), "Topology HTML pages should use the shared text polish.");
        Assert(defaultHtml.Contains("overflow:visible", StringComparison.Ordinal), "Topology HTML pages should keep exported topology strokes and labels visible.");
        Assert(defaultHtml.Contains("@media print", StringComparison.Ordinal) && defaultHtml.Contains("background:transparent", StringComparison.Ordinal), "Topology HTML pages should include print-friendly framing.");

        var cssBackgroundChart = CreateSampleTopologyChart()
            .WithTheme(theme => theme.Background = "rgb(245, 247, 250)");
        var cssBackgroundHtml = cssBackgroundChart.ToHtmlPage(new TopologyRenderOptions { IncludeLegend = false });
        Assert(cssBackgroundHtml.Contains("background:rgb(245, 247, 250)", StringComparison.Ordinal), "Topology HTML pages should preserve caller-provided CSS background values.");

        var html = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { IncludeLegend = false, EnableHtmlInteractions = true });
        Assert(html.Contains("data-cfx-interactive=\"true\"", StringComparison.Ordinal), "Topology HTML pages should mark interactive wrappers.");
        Assert(html.Contains("data-cfx-selection-panel=\"false\"", StringComparison.Ordinal), "Topology selection panels should be opt-in for embedders.");
        Assert(html.Contains("cfx-topology-select", StringComparison.Ordinal), "Topology HTML pages should dispatch host-friendly selection events.");
        Assert(html.Contains("data-cfx-selection-kind", StringComparison.Ordinal), "Topology HTML pages should track selected topology element kind.");
        Assert(html.Contains("data-cfx-selection-status", StringComparison.Ordinal), "Topology HTML pages should track selected topology element status.");
        Assert(html.Contains("metadata: collectPrefixed(element, 'data-cfx-meta-')", StringComparison.Ordinal), "Topology selection events should expose metadata attributes as a host-friendly object.");
        Assert(html.Contains("metrics: collectPrefixed(element, 'data-cfx-metric-')", StringComparison.Ordinal), "Topology selection events should expose metric attributes as a host-friendly object.");
        Assert(html.Contains("layoutInference: attr(element, 'data-edge-layout-inference')", StringComparison.Ordinal), "Topology selection events should expose inferred edge layout diagnostics.");
        Assert(html.Contains("candidateCount: attr(element, 'data-route-candidate-count')", StringComparison.Ordinal), "Topology selection events should expose route diagnostics.");
        Assert(html.Contains("curve: attr(element, 'data-route-curve')", StringComparison.Ordinal), "Topology selection events should expose geographic route diagnostics.");
        Assert(html.Contains("controlX: attr(element, 'data-route-control-x')", StringComparison.Ordinal), "Topology selection events should expose geographic route control points.");
        Assert(html.Contains("detail.related = related(detail)", StringComparison.Ordinal), "Topology selection events should include related nodes, edges, groups, and edge summaries.");
        Assert(html.Contains("element.setAttribute('tabindex', '0')", StringComparison.Ordinal), "Topology HTML pages should make topology elements keyboard focusable.");
        Assert(html.Contains("event.key !== 'Enter' && event.key !== ' '", StringComparison.Ordinal), "Topology HTML pages should support keyboard activation.");
        Assert(html.Contains("cfx-topology-navigate", StringComparison.Ordinal), "Topology HTML pages should dispatch host-friendly keyboard navigation events.");
        Assert(html.Contains("data-cfx-navigation-source", StringComparison.Ordinal), "Topology HTML pages should track keyboard navigation source state.");
        Assert(html.Contains("data-cfx-navigation-target", StringComparison.Ordinal), "Topology HTML pages should track keyboard navigation target state.");
        Assert(html.Contains("event.key === 'ArrowRight'", StringComparison.Ordinal) && html.Contains("focusRelated(element", StringComparison.Ordinal), "Topology HTML pages should support arrow-key navigation across related topology elements.");
        Assert(html.Contains("cfx-topology-set-selection", StringComparison.Ordinal), "Topology HTML pages should allow hosts to drive selection state.");
        Assert(html.Contains("cfx-topology-clear-selection", StringComparison.Ordinal), "Topology HTML pages should allow hosts to clear selection state.");
        Assert(html.Contains("cfx-topology-hover", StringComparison.Ordinal), "Topology HTML pages should dispatch host-friendly hover events.");
        Assert(html.Contains("cfx-topology-hover-clear", StringComparison.Ordinal), "Topology HTML pages should dispatch host-friendly hover-clear events.");
        Assert(html.Contains("data-cfx-hover-kind", StringComparison.Ordinal), "Topology HTML pages should track hovered topology element kind.");
        Assert(html.Contains("data-cfx-hover-id", StringComparison.Ordinal), "Topology HTML pages should track hovered topology element id.");
        Assert(html.Contains("cfx-topology-html-hover-related", StringComparison.Ordinal), "Topology HTML pages should mark hover-related elements.");
        Assert(html.Contains("pointerenter", StringComparison.Ordinal) && html.Contains("focus', () => hover(element)", StringComparison.Ordinal), "Topology HTML pages should support pointer and keyboard-focus hover highlighting.");

        var staticHtml = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { EnableHtmlInteractions = false });
        Assert(staticHtml.Contains("data-cfx-interactive=\"false\"", StringComparison.Ordinal), "Topology HTML interactions should be optional.");
        Assert(!staticHtml.Contains("new CustomEvent('cfx-topology-select'", StringComparison.Ordinal), "Static topology HTML pages should omit the interaction script.");
        Assert(!staticHtml.Contains("new CustomEvent('cfx-topology-hover'", StringComparison.Ordinal), "Static topology HTML pages should omit hover interaction hooks.");
        Assert(!staticHtml.Contains("cfx-topology-navigate", StringComparison.Ordinal), "Static topology HTML pages should omit keyboard navigation hooks.");

        var selectionPanelHtml = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { IncludeLegend = false, EnableHtmlInteractions = true, EnableHtmlSelectionPanel = true });
        Assert(selectionPanelHtml.Contains("data-cfx-selection-panel=\"true\"", StringComparison.Ordinal), "Interactive topology HTML should expose opt-in selected-record panels.");
        Assert(selectionPanelHtml.Contains("data-cfx-topology-selection-panel=\"true\"", StringComparison.Ordinal), "Selection panels should render a host-independent detail surface.");
        Assert(selectionPanelHtml.Contains("data-cfx-selection-title=\"true\"", StringComparison.Ordinal), "Selection panels should expose a title target for runtime updates.");
        Assert(selectionPanelHtml.Contains("const renderSelectionPanel = detail =>", StringComparison.Ordinal), "Selection interactions should update built-in detail panels from the same host event detail.");
        Assert(selectionPanelHtml.Contains("hydrateSelected(initiallySelected)", StringComparison.Ordinal), "Selection panels should hydrate server-selected elements without emitting startup events or dimming unrelated topology elements.");
        Assert(selectionPanelHtml.Contains("cfx-topology-selection-panel__row", StringComparison.Ordinal), "Selection panels should include compact fact rows for metadata, metrics, and related edges.");
        Assert(selectionPanelHtml.Contains(".cfx-topology-selection-panel,.cfx-topology-force-controls", StringComparison.Ordinal), "Viewport gestures should ignore selection panel interactions.");
        var helperSelectionPanelHtml = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions().WithHtmlSelectionPanel());
        Assert(helperSelectionPanelHtml.Contains("data-cfx-interactive=\"true\"", StringComparison.Ordinal), "Selection panel fluent helper should enable topology HTML interactions.");
        Assert(helperSelectionPanelHtml.Contains("data-cfx-topology-selection-panel=\"true\"", StringComparison.Ordinal), "Selection panel fluent helper should render the panel without requiring a separate interactions toggle.");

        var viewportHtml = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { IncludeLegend = false, EnableHtmlInteractions = true, EnableHtmlViewportControls = true });
        Assert(viewportHtml.Contains("data-cfx-viewport-controls=\"true\"", StringComparison.Ordinal), "Topology HTML pages should expose opt-in viewport controls.");
        Assert(viewportHtml.Contains("data-cfx-controls-placement=\"top-left\"", StringComparison.Ordinal), "Topology viewport controls should default to upper-left placement.");
        Assert(viewportHtml.Contains("data-cfx-topology-zoom=\"in\"", StringComparison.Ordinal) && viewportHtml.Contains("data-cfx-topology-zoom=\"out\"", StringComparison.Ordinal), "Topology viewport controls should include zoom actions.");
        Assert(viewportHtml.Contains("data-cfx-topology-fit=\"true\"", StringComparison.Ordinal), "Topology viewport controls should include fit-to-view.");
        Assert(viewportHtml.Contains("data-cfx-topology-dragging", StringComparison.Ordinal), "Topology viewport controls should track direct drag panning state.");
        Assert(viewportHtml.Contains(".cfx-topology-wrapper[data-cfx-viewport-controls='true'] .cfx-topology-viewport>svg", StringComparison.Ordinal), "Viewport transforms should target only the embedded topology SVG, not toolbar icon SVGs.");
        Assert(viewportHtml.Contains("const zoomBy = (factor, origin) =>", StringComparison.Ordinal), "Topology viewport controls should zoom around the pointer or active control.");
        Assert(viewportHtml.Contains("button.addEventListener('click', () => zoomBy(attr(button, 'data-cfx-topology-zoom') === 'in' ? 1.2 : 0.8333333333))", StringComparison.Ordinal), "Topology zoom buttons should zoom around the viewport center instead of the overlaid toolbar button.");
        Assert(viewportHtml.Contains("const topologySvg = () =>", StringComparison.Ordinal) && viewportHtml.Contains("[data-cfx-role=\"topology\"]", StringComparison.Ordinal), "Topology viewport controls should target the real topology SVG instead of toolbar icon SVGs.");
        Assert(viewportHtml.Contains("cfx-topology-set-viewport", StringComparison.Ordinal), "Topology viewport controls should allow hosts to drive viewport state.");
        Assert(viewportHtml.Contains("cfx-topology-reset-viewport", StringComparison.Ordinal), "Topology viewport controls should allow hosts to reset viewport state.");
        Assert(viewportHtml.Contains("cfx-topology-viewport", StringComparison.Ordinal), "Topology viewport controls should dispatch host-friendly viewport events.");
        Assert(viewportHtml.Contains("addEventListener('wheel'", StringComparison.Ordinal), "Topology viewport controls should support wheel zoom.");
        Assert(viewportHtml.Contains("data-cfx-topology-fit", StringComparison.Ordinal) && viewportHtml.Contains("cfx-topology-fit", StringComparison.Ordinal), "Topology viewport controls should support host-visible fit events.");

        var staticViewportHtml = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { EnableHtmlInteractions = false, EnableHtmlViewportControls = true });
        Assert(staticViewportHtml.Contains("data-cfx-viewport-controls=\"false\"", StringComparison.Ordinal), "Static topology HTML pages should not render viewport controls.");
        Assert(!staticViewportHtml.Contains("data-cfx-topology-zoom=\"in\"", StringComparison.Ordinal), "Static topology HTML pages should omit viewport control markup.");

        var exportHtml = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { IncludeLegend = false, EnableHtmlInteractions = true, EnableHtmlExportControls = true });
        Assert(exportHtml.Contains("data-cfx-export-controls=\"true\"", StringComparison.Ordinal), "Topology HTML pages should expose opt-in export controls.");
        Assert(exportHtml.Contains("data-cfx-topology-export=\"svg\"", StringComparison.Ordinal) && exportHtml.Contains("data-cfx-topology-export=\"png\"", StringComparison.Ordinal), "Topology export controls should include SVG and PNG actions.");
        Assert(exportHtml.Contains("new CustomEvent('cfx-topology-export'", StringComparison.Ordinal), "Topology export controls should dispatch host-friendly export events.");
        Assert(exportHtml.Contains("new XMLSerializer().serializeToString(clone)", StringComparison.Ordinal), "Topology SVG export should serialize the prepared embedded SVG clone.");
        Assert(exportHtml.Contains("canvas.toBlob", StringComparison.Ordinal), "Topology PNG export should rasterize the embedded SVG in-browser.");

        var fragmentHtml = CreateSampleTopologyChart().ToHtmlFragment(new TopologyRenderOptions { IncludeLegend = false, EnableHtmlInteractions = true, EnableHtmlViewportControls = true, EnableHtmlExportControls = true });
        Assert(fragmentHtml.Contains("data-cfx-topology-assets=\"true\"", StringComparison.Ordinal), "Interactive topology fragments should include scoped topology CSS assets for embedders.");
        Assert(fragmentHtml.Contains("--cfx-topology-control-bg", StringComparison.Ordinal), "Topology fragment controls should be themeable through CSS variables.");
        Assert(fragmentHtml.Contains("<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M12 5v14M5 12h14\"", StringComparison.Ordinal), "Topology fragment controls should render icon-style buttons instead of raw text controls.");
        Assert(fragmentHtml.Contains("cfx-topology-select", StringComparison.Ordinal), "Interactive topology fragments should include the topology runtime script.");
        Assert(fragmentHtml.Contains("data-cfx-runtime-bound", StringComparison.Ordinal), "The topology runtime should guard wrappers against duplicate event binding when several fragments are embedded.");

        var staticExportHtml = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { EnableHtmlInteractions = false, EnableHtmlExportControls = true });
        Assert(staticExportHtml.Contains("data-cfx-export-controls=\"false\"", StringComparison.Ordinal), "Static topology HTML pages should not render export controls.");
        Assert(!staticExportHtml.Contains("data-cfx-topology-export=\"svg\"", StringComparison.Ordinal), "Static topology HTML pages should omit export control markup.");

        var syncHtml = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { IncludeLegend = false, EnableHtmlInteractions = true, EnableHtmlSynchronizedState = true, HtmlSyncGroupName = "directory-topology" });
        Assert(syncHtml.Contains("data-cfx-sync-enabled=\"true\"", StringComparison.Ordinal), "Topology HTML pages should expose opt-in synchronization state.");
        Assert(syncHtml.Contains("data-cfx-sync-group=\"directory-topology\"", StringComparison.Ordinal), "Topology HTML pages should expose the synchronization group name.");
        Assert(syncHtml.Contains("new CustomEvent('cfx-topology-sync'", StringComparison.Ordinal), "Topology HTML pages should dispatch host-friendly synchronization events.");
        Assert(syncHtml.Contains("cfx-topology-apply-sync", StringComparison.Ordinal), "Topology HTML pages should accept synchronized state from peer topology wrappers.");
        Assert(syncHtml.Contains("emitSync('selection'", StringComparison.Ordinal) && syncHtml.Contains("emitSync('viewport'", StringComparison.Ordinal), "Topology synchronization should cover selection and viewport state.");

        var syncWithoutGroupHtml = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { EnableHtmlInteractions = true, EnableHtmlSynchronizedState = true });
        Assert(syncWithoutGroupHtml.Contains("data-cfx-sync-enabled=\"false\"", StringComparison.Ordinal), "Topology synchronization should require an explicit group name.");

        var staticSyncHtml = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { EnableHtmlInteractions = false, EnableHtmlSynchronizedState = true, HtmlSyncGroupName = "directory-topology" });
        Assert(staticSyncHtml.Contains("data-cfx-sync-enabled=\"false\"", StringComparison.Ordinal), "Static topology HTML pages should not enable synchronization.");
        Assert(!staticSyncHtml.Contains("new CustomEvent('cfx-topology-sync'", StringComparison.Ordinal), "Static topology HTML pages should omit synchronization hooks.");

        var prefixedHtml = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { EnableHtmlInteractions = true, CssClassPrefix = "report-topology" });
        Assert(prefixedHtml.Contains("class=\"report-topology-wrapper\"", StringComparison.Ordinal), "Topology HTML wrappers should honor CssClassPrefix.");
        Assert(prefixedHtml.Contains(".report-topology-wrapper[data-cfx-interactive=\"true\"]", StringComparison.Ordinal), "Topology interaction selectors should honor CssClassPrefix.");
        Assert(prefixedHtml.Contains("report-topology-html-selected", StringComparison.Ordinal), "Topology interaction state classes should honor CssClassPrefix.");
        Assert(!prefixedHtml.Contains("class=\"cfx-topology-wrapper\"", StringComparison.Ordinal), "Prefixed topology HTML should not keep hardcoded wrapper class names.");

        var sanitizedPrefixHtml = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { EnableHtmlInteractions = true, CssClassPrefix = "123 report topology" });
        Assert(sanitizedPrefixHtml.Contains("class=\"cfx-123-report-topology-wrapper\"", StringComparison.Ordinal), "Topology HTML should sanitize wrapper CSS prefixes.");
        Assert(sanitizedPrefixHtml.Contains("class=\"cfx-123-report-topology\"", StringComparison.Ordinal), "Topology embedded SVG should use the same sanitized CSS prefix as the HTML wrapper.");
        Assert(!sanitizedPrefixHtml.Contains("123 report topology", StringComparison.Ordinal), "Topology HTML should not emit unsanitized CSS prefixes.");
    }
}
