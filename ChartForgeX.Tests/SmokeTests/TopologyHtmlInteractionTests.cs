using System;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyHtmlPagesExposeSelectionInteractions() {
        var html = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { IncludeLegend = false });
        Assert(html.Contains("data-cfx-interactive=\"true\"", StringComparison.Ordinal), "Topology HTML pages should mark interactive wrappers.");
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
        Assert(!staticHtml.Contains("cfx-topology-select", StringComparison.Ordinal), "Static topology HTML pages should omit the interaction script.");
        Assert(!staticHtml.Contains("cfx-topology-hover", StringComparison.Ordinal), "Static topology HTML pages should omit hover interaction hooks.");
        Assert(!staticHtml.Contains("cfx-topology-navigate", StringComparison.Ordinal), "Static topology HTML pages should omit keyboard navigation hooks.");

        var viewportHtml = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { IncludeLegend = false, EnableHtmlViewportControls = true });
        Assert(viewportHtml.Contains("data-cfx-viewport-controls=\"true\"", StringComparison.Ordinal), "Topology HTML pages should expose opt-in viewport controls.");
        Assert(viewportHtml.Contains("data-cfx-topology-zoom=\"in\"", StringComparison.Ordinal) && viewportHtml.Contains("data-cfx-topology-zoom=\"out\"", StringComparison.Ordinal), "Topology viewport controls should include zoom actions.");
        Assert(viewportHtml.Contains("data-cfx-topology-mode=\"pan\"", StringComparison.Ordinal), "Topology viewport controls should include a pan mode.");
        Assert(viewportHtml.Contains("cfx-topology-set-viewport", StringComparison.Ordinal), "Topology viewport controls should allow hosts to drive viewport state.");
        Assert(viewportHtml.Contains("cfx-topology-reset-viewport", StringComparison.Ordinal), "Topology viewport controls should allow hosts to reset viewport state.");
        Assert(viewportHtml.Contains("cfx-topology-viewport", StringComparison.Ordinal), "Topology viewport controls should dispatch host-friendly viewport events.");
        Assert(viewportHtml.Contains("addEventListener('wheel'", StringComparison.Ordinal), "Topology viewport controls should support wheel zoom.");

        var staticViewportHtml = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { EnableHtmlInteractions = false, EnableHtmlViewportControls = true });
        Assert(staticViewportHtml.Contains("data-cfx-viewport-controls=\"false\"", StringComparison.Ordinal), "Static topology HTML pages should not render viewport controls.");
        Assert(!staticViewportHtml.Contains("data-cfx-topology-zoom=\"in\"", StringComparison.Ordinal), "Static topology HTML pages should omit viewport control markup.");

        var exportHtml = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { IncludeLegend = false, EnableHtmlExportControls = true });
        Assert(exportHtml.Contains("data-cfx-export-controls=\"true\"", StringComparison.Ordinal), "Topology HTML pages should expose opt-in export controls.");
        Assert(exportHtml.Contains("data-cfx-topology-export=\"svg\"", StringComparison.Ordinal) && exportHtml.Contains("data-cfx-topology-export=\"png\"", StringComparison.Ordinal), "Topology export controls should include SVG and PNG actions.");
        Assert(exportHtml.Contains("new CustomEvent('cfx-topology-export'", StringComparison.Ordinal), "Topology export controls should dispatch host-friendly export events.");
        Assert(exportHtml.Contains("new XMLSerializer().serializeToString(svg)", StringComparison.Ordinal), "Topology SVG export should serialize the embedded SVG.");
        Assert(exportHtml.Contains("canvas.toBlob", StringComparison.Ordinal), "Topology PNG export should rasterize the embedded SVG in-browser.");

        var staticExportHtml = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { EnableHtmlInteractions = false, EnableHtmlExportControls = true });
        Assert(staticExportHtml.Contains("data-cfx-export-controls=\"false\"", StringComparison.Ordinal), "Static topology HTML pages should not render export controls.");
        Assert(!staticExportHtml.Contains("data-cfx-topology-export=\"svg\"", StringComparison.Ordinal), "Static topology HTML pages should omit export control markup.");

        var syncHtml = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { IncludeLegend = false, EnableHtmlSynchronizedState = true, HtmlSyncGroupName = "directory-topology" });
        Assert(syncHtml.Contains("data-cfx-sync-enabled=\"true\"", StringComparison.Ordinal), "Topology HTML pages should expose opt-in synchronization state.");
        Assert(syncHtml.Contains("data-cfx-sync-group=\"directory-topology\"", StringComparison.Ordinal), "Topology HTML pages should expose the synchronization group name.");
        Assert(syncHtml.Contains("new CustomEvent('cfx-topology-sync'", StringComparison.Ordinal), "Topology HTML pages should dispatch host-friendly synchronization events.");
        Assert(syncHtml.Contains("cfx-topology-apply-sync", StringComparison.Ordinal), "Topology HTML pages should accept synchronized state from peer topology wrappers.");
        Assert(syncHtml.Contains("emitSync('selection'", StringComparison.Ordinal) && syncHtml.Contains("emitSync('viewport'", StringComparison.Ordinal), "Topology synchronization should cover selection and viewport state.");

        var syncWithoutGroupHtml = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { EnableHtmlSynchronizedState = true });
        Assert(syncWithoutGroupHtml.Contains("data-cfx-sync-enabled=\"false\"", StringComparison.Ordinal), "Topology synchronization should require an explicit group name.");

        var staticSyncHtml = CreateSampleTopologyChart().ToHtmlPage(new TopologyRenderOptions { EnableHtmlInteractions = false, EnableHtmlSynchronizedState = true, HtmlSyncGroupName = "directory-topology" });
        Assert(staticSyncHtml.Contains("data-cfx-sync-enabled=\"false\"", StringComparison.Ordinal), "Static topology HTML pages should not enable synchronization.");
        Assert(!staticSyncHtml.Contains("new CustomEvent('cfx-topology-sync'", StringComparison.Ordinal), "Static topology HTML pages should omit synchronization hooks.");
    }
}
