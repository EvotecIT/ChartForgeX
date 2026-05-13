using System;
using System.Collections.Generic;

namespace ChartForgeX.Topology;

internal sealed class TopologyHighlightState {
    private readonly HashSet<TopologyHealthStatus> _statuses;
    private readonly HashSet<string> _groupIds;
    private readonly HashSet<string> _nodeIds;
    private readonly HashSet<string> _edgeIds;
    private readonly TopologyScenarioSelection? _scenario;

    private TopologyHighlightState(TopologyChart chart, TopologyRenderOptions options) {
        _statuses = new HashSet<TopologyHealthStatus>(options.HighlightStatuses);
        _groupIds = new HashSet<string>(options.HighlightGroupIds, StringComparer.Ordinal);
        _nodeIds = new HashSet<string>(options.HighlightNodeIds, StringComparer.Ordinal);
        _edgeIds = new HashSet<string>(options.HighlightEdgeIds, StringComparer.Ordinal);
        foreach (var node in chart.Nodes) {
            if (!string.IsNullOrWhiteSpace(node.GroupId) && _groupIds.Contains(node.GroupId!)) _nodeIds.Add(node.Id);
        }

        _scenario = TopologyScenarioSelection.From(chart, options.ActiveScenarioId);
        HighlightConnectedEdges = options.HighlightConnectedEdges;
        DimmedOpacity = Math.Max(0.05, Math.Min(1, options.DimmedOpacity));
    }

    public bool IsActive => _statuses.Count > 0 || _groupIds.Count > 0 || _nodeIds.Count > 0 || _edgeIds.Count > 0 || _scenario != null;

    public string? ActiveScenarioId => _scenario?.Id;

    public bool HighlightConnectedEdges { get; }

    public double DimmedOpacity { get; }

    public static TopologyHighlightState From(TopologyChart chart, TopologyRenderOptions options) => new(chart, options);

    public bool IsGroupHighlighted(TopologyGroup group) {
        if (!IsActive) return true;
        return _groupIds.Contains(group.Id)
            || _scenario?.GroupIds.Contains(group.Id) == true
            || _statuses.Contains(group.Status);
    }

    public bool IsNodeHighlighted(TopologyNode node) {
        if (!IsActive) return true;
        return _nodeIds.Contains(node.Id)
            || _scenario?.NodeIds.Contains(node.Id) == true
            || (!string.IsNullOrWhiteSpace(node.GroupId) && _groupIds.Contains(node.GroupId!))
            || _statuses.Contains(node.Status);
    }

    public bool IsEdgeHighlighted(TopologyEdge edge) {
        if (!IsActive) return true;
        if (_edgeIds.Contains(edge.Id) || _scenario?.EdgeIds.Contains(edge.Id) == true || _statuses.Contains(edge.Status)) return true;
        if (!HighlightConnectedEdges) return false;
        return _nodeIds.Contains(edge.SourceNodeId) || _nodeIds.Contains(edge.TargetNodeId);
    }

    public string SvgOpacity(bool isHighlighted) => IsActive && !isHighlighted ? " opacity=\"" + TopologyRenderPrimitives.F(DimmedOpacity) + "\"" : string.Empty;

    public string CssClass(string prefix, bool isHighlighted) {
        if (!IsActive) return string.Empty;
        return isHighlighted ? " " + prefix + "--highlighted" : " " + prefix + "--dimmed";
    }
}
