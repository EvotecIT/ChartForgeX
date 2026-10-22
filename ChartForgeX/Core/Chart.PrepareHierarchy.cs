using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    internal ChartHierarchyPreparation? HierarchyPreparation { get; private set; }

    /// <summary>Prepares a detached Treemap or Sunburst subtree without replacing source observations.</summary>
    /// <param name="context">The logical size, frame, theme and typography.</param>
    /// <param name="view">The existing item to use as the visible root, or null for the full hierarchy.</param>
    /// <remarks>Source identities, authored parents, values, colors and states are retained. Sunburst geometry rebases depth and preserves authored remainders.
    /// Missing roots and unsupported families are rejected. A selected zero-valued item remains a no-data view.</remarks>
    public PreparedVisual PrepareHierarchy(VisualRenderContext context, ChartHierarchyView? view = null) =>
        PrepareCore(context, null, hierarchyView: view, requireHierarchy: true);
}
