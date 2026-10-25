using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    /// <summary>Reuses the incumbent node painter at its captured native units; observations retain the final graph.</summary>
    private bool TryBuildCapturedNode(TopologyNode node) {
        var basis = TopologyPreparedNodeBasis.Resolve(node);
        if (basis?.UsesOriginalPresentation != true) return false;
        AtOriginalNodeUnits(node, basis, BuildNode);
        return true;
    }

    /// <summary>Actor figures retain direct-factor geometry; their caption fitting uses its original numeric domain.</summary>
    private bool TryBuildCapturedDiagramCaption(TopologyNode node, bool active) {
        var basis = TopologyPreparedNodeBasis.Resolve(node);
        if (basis == null || !TopologyPreparedNodeBasis.IsActor(node.Shape)
            || basis.DisplayMode is not (TopologyNodeDisplayMode.Card or TopologyNodeDisplayMode.CompactCard)) return false;
        AtOriginalNodeUnits(node, basis, raw => {
            using var policy = UseTextPolicy(raw.TextStyle);
            BuildDiagramCaption(raw, active);
        });
        return true;
    }

    /// <summary>Runs the incumbent painter against one detached original node, restoring projection in every exit path.</summary>
    private void AtOriginalNodeUnits(TopologyNode node, TopologyPreparedNodeBasis basis, Action<TopologyNode> paint) {
        var raw = basis.OriginalNode(node); // Detached one-node value; no producer callback or retained raw graph.
        var priorScale = _scale; var priorX = _offsetX; var priorY = _offsetY;
        var anchor = Point(new ChartPoint(node.X, node.Y));
        try {
            _scale = priorScale * basis.Factor;
            _offsetX = anchor.X - basis.OriginalBody.X * _scale;
            _offsetY = anchor.Y - basis.OriginalBody.Y * _scale;
            // Raw has no captured basis. Existing BuildNode/UseTextPolicy owns exactly the same surfaces,
            // glyph parser, fitting, paint order, metadata, resolved font callbacks, marks and final-coordinate region.
            paint(raw);
        } finally {
            _scale = priorScale; _offsetX = priorX; _offsetY = priorY;
        }
    }
}
