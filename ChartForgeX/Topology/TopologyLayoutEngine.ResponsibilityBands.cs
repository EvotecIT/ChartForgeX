using System.Collections.Generic;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    /// <summary>Cross-axis slots for selected weighted root responsibilities; ordinary layered/grid placement stays unchanged.</summary>
    private sealed class LayeredResponsibilityBands {
        private readonly Dictionary<TopologyNode, (double Offset, int Index)> _offsets = new();
        internal LayeredResponsibilityBands(TopologyChart local, TopologyRenderOptions options) {
            var horizontal = local.LayoutDirection is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft;
            var spacing = options.LayeredNodeSpacing ?? (horizontal ? 24 : 28);
            var next = 0d;
            // Local items are already ordered by the single Swimlane responsibility owner, with real ungrouped siblings first.
            // Full measured proxy dimensions retain nested shells/headers; each main-axis rank shares these same slots.
            foreach (var node in local.Nodes) {
                _offsets.Add(node, (next, _offsets.Count));
                next += (horizontal ? LayeredNodeHeight(local, node) : LayeredNodeWidth(local, node)) + spacing;
            }
        }
        internal double Offset(TopologyNode node) => _offsets[node].Offset;
        internal int Index(TopologyNode node) => _offsets[node].Index;
    }
}
