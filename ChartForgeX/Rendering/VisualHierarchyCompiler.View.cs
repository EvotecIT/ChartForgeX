using System.Collections.Generic;

namespace ChartForgeX.Rendering;

internal static partial class VisualHierarchyCompiler {
    private static void ApplyHierarchyViewMetadata(Dictionary<string, string> metadata, ChartHierarchyPreparation? view, int index) {
        if (view?.Request == null) return;
        metadata["data-cfx-view-root"] = view.Request.RootId;
        if (view.Contains(index)) metadata["data-cfx-view-depth"] = N(view.Depth(index));
        else {
            metadata["data-cfx-geometry-status"] = "outside-view";
            metadata["data-cfx-visible"] = "false";
            metadata["data-cfx-passive"] = "true";
        }
    }
}
