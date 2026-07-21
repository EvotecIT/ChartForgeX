using System;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void GraphExplorerEditingStateAndBoxSelectionStayOptIn() {
        var readOnly = SampleGraphScene().ToGraphExplorerHtmlFragment();
        Assert(readOnly.Contains("data-cfx-graph-action=\"box-select\"", StringComparison.Ordinal), "Explorer scenes should include modern box selection by default.");
        Assert(!readOnly.Contains("<aside class=\"cfx-graph-editor\"", StringComparison.Ordinal), "Graph editing should remain absent unless manipulation is explicitly enabled.");

        var scene = SampleGraphScene();
        scene.Options.Enable(GraphSceneFeatures.IncrementalUpdates | GraphSceneFeatures.Manipulation | GraphSceneFeatures.History);
        scene.Options.Manipulation.EnableEditing();
        scene.Options.Manipulation.MaximumHistoryEntries = 25;
        var html = scene.ToGraphExplorerHtmlFragment(options => {
            options.PersistInteractionState = true;
            options.InteractionStateStorageKey = "reviewed-service-map";
        });

        Assert(html.Contains("data-cfx-graph-manipulation=\"true\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-manipulation-capabilities=\"addNodes,editNodes,deleteNodes,addEdges,editEdges,deleteEdges,dragGroups,persistPositions\"", StringComparison.Ordinal), "Editable scenes should publish their exact allowed capabilities.");
        Assert(html.Contains("data-cfx-graph-history-limit=\"25\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-action=\"undo\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-action=\"redo\"", StringComparison.Ordinal), "Editable scenes should expose bounded undo and redo controls.");
        Assert(html.Contains("data-cfx-graph-state-persist=\"true\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-state-key=\"reviewed-service-map\"", StringComparison.Ordinal), "Hosts should opt into namespaced interaction-state persistence.");
        Assert(html.Contains("data-cfx-role=\"graph-editor\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-editor-form=\"node\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-editor-form=\"edge\"", StringComparison.Ordinal), "Editable scenes should render a typed, accessible node and edge editor.");
        Assert(html.Contains("data-cfx-role=\"graph-selection-box\"", StringComparison.Ordinal) && html.Contains("cfxgraphboxselect", StringComparison.Ordinal), "Graph explorers should expose a cross-renderer box-selection surface and host event.");
        Assert(html.Contains("const requestGraphChange", StringComparison.Ordinal) && html.Contains("cfxgraphbeforechange", StringComparison.Ordinal) && html.Contains("cfxgraphchange", StringComparison.Ordinal), "Graph mutations should pass through a cancelable, validated host contract.");
        Assert(html.Contains("const captureGraphInteractionState", StringComparison.Ordinal) && html.Contains("const applyGraphInteractionState", StringComparison.Ordinal) && html.Contains("localStorage.setItem", StringComparison.Ordinal), "Graph interaction snapshots should support explicit capture, replay, and opt-in local persistence.");
        Assert(html.Contains("const restoredInteractionState = initializeGraphInteractionState(root);", StringComparison.Ordinal) && html.Contains("if (!restoredInteractionState && hasFeature(root, 'RuntimePhysics')", StringComparison.Ordinal), "A successfully restored interaction state should preserve its saved coordinates instead of immediately restarting initial stabilization.");
        Assert(html.Contains("applyGraphRuntimePatch(root, graphSnapshotPatch(root, snapshot), { reheat: false, reason: 'state-restore' })", StringComparison.Ordinal) && html.Contains("options?.reheat === false", StringComparison.Ordinal), "Editable snapshot document restoration should suppress patch reheating as well as initial stabilization.");
        Assert(html.Contains("hidden: attr(node.el, 'data-node-hidden') === 'true'", StringComparison.Ordinal) && html.Contains("hidden: attr(edge, 'data-edge-hidden') === 'true'", StringComparison.Ordinal), "Graph snapshots should persist only intrinsic hidden state, not transient cluster, hierarchy, or filter visibility.");
        Assert(html.Contains("const optionalGraphCoordinate", StringComparison.Ordinal) && html.Contains("x: x ?? existing?.x ?? Number(root.dataset.cfxGraphLastPointerX", StringComparison.Ordinal) && html.Contains("y: y ?? existing?.y ?? Number(root.dataset.cfxGraphLastPointerY", StringComparison.Ordinal), "Blank editor coordinates should preserve an edited node's current position, use the pointer or scene-center fallback for additions, and preserve an explicit zero.");
        Assert(html.Contains("change: (target, patch", StringComparison.Ordinal) && html.Contains("captureState:", StringComparison.Ordinal) && html.Contains("applyState:", StringComparison.Ordinal) && html.Contains("undo: target", StringComparison.Ordinal) && html.Contains("redo: target", StringComparison.Ordinal), "The dependency-free host API should expose safe mutation, state, and history operations.");
        Assert(html.Contains("Move cluster", StringComparison.Ordinal) && html.Contains("graphCapability(root, 'dragGroups')", StringComparison.Ordinal), "Collapsed clusters should support capability-gated group movement.");
        var style = HtmlGraphExplorerRenderer.BuildFragmentStyle();
        Assert(style.Contains(".cfx-graph-editor", StringComparison.Ordinal) && style.Contains(".cfx-graph-selection-box", StringComparison.Ordinal), "Graph editing and box selection should share the premium self-contained visual system.");

        var invalid = SampleGraphScene();
        invalid.Options.Enable(GraphSceneFeatures.Manipulation);
        AssertThrows<InvalidOperationException>(() => invalid.Validate(), "Manipulation should require the atomic incremental-update contract.");
        var invalidBoxSelection = SampleGraphScene();
        invalidBoxSelection.Options.Disable(GraphSceneFeatures.Selection);
        AssertThrows<InvalidOperationException>(() => invalidBoxSelection.Validate(), "Box selection should require the shared selection contract.");
        Assert(html.Contains("hasFeature(root, 'BoxSelection') && hasFeature(root, 'Selection')", StringComparison.Ordinal) && html.Contains("if (!hasFeature(root, 'Selection')) return []", StringComparison.Ordinal), "Box-selection controls and gestures should remain defensively gated by the selection feature in the browser runtime.");
        AssertThrows<ArgumentOutOfRangeException>(() => scene.Options.Manipulation.MaximumHistoryEntries = 0, "Graph history should reject an empty retention budget.");
    }
}
