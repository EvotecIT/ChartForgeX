using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Topology;

/// <summary>
/// Names the arrow and endpoint markers of a rendered topology by where an edge takes its colour from, not by the colour
/// itself: <c>muted</c>, the status (<c>healthy</c>, <c>warning</c>, <c>critical</c>, <c>disabled</c>, <c>unknown</c>), or
/// <c>color-N</c> for the N-th distinct explicit edge colour string in edge order. It follows the branches of
/// <see cref="TopologyRenderPrimitives.EdgeColor"/>, so edges with one key always share one colour. Light and dark renders
/// of one chart therefore use the same marker ids, and with SVG colour variables one SVG draws arrows in either theme.
/// </summary>
internal sealed class TopologySvgMarkerKeys {
    private readonly Dictionary<string, int> _explicitColors = new(StringComparer.Ordinal);

    /// <summary>Numbers the explicit edge colours of <paramref name="chart"/> in edge order.</summary>
    public TopologySvgMarkerKeys(TopologyChart chart) {
        foreach (var edge in chart.Edges) {
            if (edge.IsMuted || string.IsNullOrWhiteSpace(edge.Color)) continue;
            var color = edge.Color!.Trim();
            if (!_explicitColors.ContainsKey(color)) _explicitColors[color] = _explicitColors.Count + 1;
        }
    }

    /// <summary>Returns the marker key of an edge of the chart the keys were built from.</summary>
    public string Key(TopologyEdge edge) {
        if (edge.IsMuted) return "muted";
        if (string.IsNullOrWhiteSpace(edge.Color)) return TopologyRenderPrimitives.StatusMarkerToken(edge.Status);
        return "color-" + _explicitColors[edge.Color!.Trim()].ToString(CultureInfo.InvariantCulture);
    }
}
