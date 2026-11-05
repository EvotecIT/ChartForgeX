using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Topology;

public sealed partial class TopologyChart {
    /// <summary>
    /// Gets or sets whether an edge between two cards whose fixed ports face each other (right to left, bottom to
    /// top) also places those cards in that order on one row or column. Diagram languages whose sides describe
    /// placement, such as Mermaid architecture and C4 directional relationships, set it; ordinary charts keep
    /// fixed ports as attachment sides only, so a deliberate loop still leaves and returns as authored.
    /// </summary>
    internal bool PlacesFacingPorts { get; set; }
}

internal static partial class TopologyLayoutEngine {
    private const string FacingPortAlignmentPrefix = "layout-facing-ports-";

    private static bool HasFacingPorts(TopologyEdge edge) => (edge.SourcePort, edge.TargetPort) is
        (TopologyEdgePort.Right, TopologyEdgePort.Left) or (TopologyEdgePort.Left, TopologyEdgePort.Right)
        or (TopologyEdgePort.Bottom, TopologyEdgePort.Top) or (TopologyEdgePort.Top, TopologyEdgePort.Bottom);

    // Facing ports become ordered rows and columns inside the detached layout copy only, so portable output
    // keeps the authored declarations. A copy the alignment solver cannot place falls back to the ordinary layout.
    private static void AddFacingPortAlignments(TopologyChart copy) {
        if (copy.LayoutMode is not (TopologyLayoutMode.Layered or TopologyLayoutMode.DenseGrouped)) return;
        if (copy.Nodes.Any(node => node.HasPositionOverride || Math.Abs(node.X) >= 0.0001 || Math.Abs(node.Y) >= 0.0001 || node.Metadata.ContainsKey("layer"))
            || copy.Groups.Any(group => group.HasPositionOverride || group.X != 0 || group.Y != 0 || group.Width > 0 || group.Height > 0)
            || copy.Edges.Any(edge => edge.MinimumRankSpan > 1)) return;
        var nodes = new HashSet<string>(copy.Nodes.Select(node => node.Id), StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var added = 0;
        foreach (var edge in copy.Edges) {
            if (!nodes.Contains(edge.SourceNodeId) || !nodes.Contains(edge.TargetNodeId) || string.Equals(edge.SourceNodeId, edge.TargetNodeId, StringComparison.Ordinal)) continue;
            TopologyAlignmentKind kind; string first, second;
            switch ((edge.SourcePort, edge.TargetPort)) {
                case (TopologyEdgePort.Right, TopologyEdgePort.Left): kind = TopologyAlignmentKind.Row; first = edge.SourceNodeId; second = edge.TargetNodeId; break;
                case (TopologyEdgePort.Left, TopologyEdgePort.Right): kind = TopologyAlignmentKind.Row; first = edge.TargetNodeId; second = edge.SourceNodeId; break;
                case (TopologyEdgePort.Bottom, TopologyEdgePort.Top): kind = TopologyAlignmentKind.Column; first = edge.SourceNodeId; second = edge.TargetNodeId; break;
                case (TopologyEdgePort.Top, TopologyEdgePort.Bottom): kind = TopologyAlignmentKind.Column; first = edge.TargetNodeId; second = edge.SourceNodeId; break;
                default: continue;
            }
            if (!seen.Add(kind + "\n" + first + "\n" + second)) continue;
            copy.Alignments.Add(new TopologyAlignment(FacingPortAlignmentPrefix + added.ToString(CultureInfo.InvariantCulture), kind, new[] { first, second }) { IsLayoutDerived = true });
            added++;
        }
        if (added > 0) copy.LayoutMode = TopologyLayoutMode.Layered;
    }
}