using System;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

public sealed partial class TopologyChart {
    internal TopologyHierarchyPreparation? HierarchyPreparation { get; set; }

    /// <summary>Prepares an explicit layered hierarchy view while retaining its complete raw source and authored relationships.</summary>
    /// <param name="context">The existing common frame, theme, fonts and logical size.</param>
    /// <param name="view">Exact collapsed IDs, sibling tie order and native direction; null restores the complete hierarchy.</param>
    /// <param name="options">Existing topology presentation options. The caller's options are not changed.</param>
    /// <returns>A detached native scene with source contributor facts and the matching immutable view.</returns>
    public PreparedVisual PrepareHierarchy(VisualRenderContext context, TopologyHierarchyView? view = null, TopologyRenderOptions? options = null) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        var captured = ResolveRenderOptions(options).Clone(); captured.HierarchyView = view ?? new TopologyHierarchyView();
        return Prepare(context, captured);
    }
}
