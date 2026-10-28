using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    internal static VisualArtifactInterchangeTopologyHierarchy MapTopologyHierarchy(TopologyHierarchyPreparation preparation) {
        var raw = preparation.CopySource();
        // Reuse the existing typed graph mapping; this branch never prepares or stamps raw positions as resolved geometry.
        var source = FromPreparedTopology(raw, new TopologyRenderOptions());
        var result = new VisualArtifactInterchangeTopologyHierarchy { Source = source, SourceId = raw.Id,
            SourcePadding = raw.Viewport.Padding, LayoutDirection = preparation.View.LayoutDirection };
        result.CollapsedNodeIds.AddRange(preparation.View.CollapsedNodeIds); result.NodeOrder.AddRange(preparation.View.NodeOrder);
        for (var index = 0; index < raw.Nodes.Count; index++) {
            var node = raw.Nodes[index]; var mapped = source.Nodes[index];
            var state = new VisualArtifactInterchangeTopologySourceNode { Id = mapped.Id, SourceId = node.Id,
                CssClass = node.CssClass, HasPositionOverride = node.HasPositionOverride, PreserveDisplayModeSize = node.PreserveDisplayModeSize,
                DisplayMode = node.DisplayMode, ShowStatusBadge = node.ShowStatusBadge };
            state.PortIds.AddRange(node.Ports.Select(port => port.Id)); result.SourceNodes.Add(state);
            CopyRawMetadata(node.Metadata, mapped.Extensions);
            for (var port = 0; port < node.Ports.Count; port++) CopyRawMetadata(node.Ports[port].Metadata, mapped.Ports[port].Extensions);
            for (var detail = 0; detail < node.Details.Count; detail++) CopyRawMetadata(node.Details[detail].Metadata, mapped.Details[detail].Extensions);
        }
        for (var index = 0; index < raw.Groups.Count; index++) {
            var group = raw.Groups[index]; var mapped = source.Groups[index];
            result.SourceGroups.Add(new VisualArtifactInterchangeTopologySourceGroup { Id = mapped.Id, SourceId = group.Id,
                CssClass = group.CssClass, HasPositionOverride = group.HasPositionOverride }); CopyRawMetadata(group.Metadata, mapped.Extensions);
        }
        for (var index = 0; index < raw.Edges.Count; index++) {
            var edge = raw.Edges[index]; var mapped = source.Edges[index];
            result.SourceEdges.Add(SourceIdentity(mapped.Id, edge.Id, edge.CssClass)); CopyRawMetadata(edge.Metadata, mapped.Extensions);
        }
        var nodeIds = result.SourceNodes.ToDictionary(state => state.SourceId, state => state.Id, StringComparer.Ordinal);
        var edgeIds = result.SourceEdges.GroupBy(state => state.SourceId, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First().Id, StringComparer.Ordinal);
        for (var index = 0; index < raw.Scenarios.Count; index++) {
            var scenario = raw.Scenarios[index]; var mapped = source.Scenarios[index];
            result.SourceScenarios.Add(SourceIdentity(mapped.Id, scenario.Id)); CopyRawMetadata(scenario.Metadata, mapped.Extensions);
            // A raw step references its authored edge identity once, including when that identity has multiple occurrences.
            mapped.Steps.Clear();
            foreach (var step in scenario.Steps) {
                var retained = new VisualArtifactInterchangeScenarioStep { TargetId = step.Kind == TopologyScenarioStepKind.Node ? nodeIds[step.Id] : edgeIds[step.Id],
                    Kind = step.Kind, Label = step.Label, Description = step.Description, DurationMilliseconds = step.DurationMilliseconds };
                CopyRawMetadata(step.Metadata, retained.Extensions); mapped.Steps.Add(retained);
            }
        }
        for (var index = 0; index < raw.Annotations.Count; index++) result.SourceAnnotations.Add(SourceIdentity(source.Annotations[index].Id, raw.Annotations[index].Id));
        for (var index = 0; index < raw.Alignments.Count; index++)
            result.SourceAlignments.Add(SourceIdentity(source.Topology!.Alignments[index].Id, raw.Alignments[index].Id));
        return result;
    }

    /// <summary>Restores the complete raw organization graph retained by a hierarchy projection.</summary>
    /// <remarks>This restores the authored layout mode and automatic-position/size flags, rather than the visible prepared bounds.
    /// The collapsed request remains separately on the envelope. Native palette callbacks and host rendering options are not portable.
    /// Details, embedded artwork, scenarios, annotations, IDs and typed raw bounds are detached. Ordinary ToTopologyChart remains Manual.</remarks>
    public static TopologyChart ToTopologyHierarchySource(this VisualArtifactInterchangeEnvelope envelope) {
        if (envelope == null) throw new ArgumentNullException(nameof(envelope)); envelope.Validate();
        RequireEditableSourcePresentation(envelope);
        var hierarchy = envelope.Topology?.Hierarchy ?? throw new NotSupportedException("The envelope does not retain a raw topology hierarchy source.");
        var source = hierarchy.Source; var chart = TopologyChart.Create();
        chart.Id = hierarchy.SourceId; chart.Title = source.Title; chart.Subtitle = source.Subtitle;
        chart.LayoutMode = source.Topology!.LayoutMode; chart.LayoutDirection = source.Topology.LayoutDirection;
        if (source.Width.HasValue) chart.Viewport.Width = source.Width.Value;
        if (source.Height.HasValue) chart.Viewport.Height = source.Height.Value;
        chart.Viewport.Padding = hierarchy.SourcePadding;
        chart.Accessibility.Name = source.AccessibleName; chart.Accessibility.Description = source.AccessibleDescription;
        chart.Accessibility.Language = source.Language; chart.Accessibility.IsDecorative = source.IsDecorative;
        RestoreTopologyPresentation(chart, source.Presentation);
        var nodes = hierarchy.SourceNodes.ToDictionary(state => state.Id, state => state.SourceId, StringComparer.Ordinal);
        var groups = hierarchy.SourceGroups.ToDictionary(state => state.Id, state => state.SourceId, StringComparer.Ordinal);
        var edges = hierarchy.SourceEdges.ToDictionary(state => state.Id, state => state.SourceId, StringComparer.Ordinal);
        var endpoints = nodes.Concat(groups).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var ports = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        for (var index = 0; index < source.Groups.Count; index++) {
            var group = RestoreTopologyGroup(source.Groups[index]); var state = hierarchy.SourceGroups[index];
            group.Id = state.SourceId; group.CssClass = state.CssClass; group.HasPositionOverride = state.HasPositionOverride;
            if (group.ParentGroupId != null) group.ParentGroupId = groups[group.ParentGroupId]; chart.Groups.Add(group);
        }
        for (var index = 0; index < source.Nodes.Count; index++) {
            var node = RestoreTopologyNode(source.Nodes[index]); var state = hierarchy.SourceNodes[index];
            var portIds = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var port = 0; port < node.Ports.Count; port++) { portIds.Add(node.Ports[port].Id, state.PortIds[port]); node.Ports[port].Id = state.PortIds[port]; }
            ports.Add(state.Id, portIds); node.Id = state.SourceId; node.CssClass = state.CssClass;
            node.HasPositionOverride = state.HasPositionOverride; node.PreserveDisplayModeSize = state.PreserveDisplayModeSize;
            node.DisplayMode = state.DisplayMode; node.ShowStatusBadge = state.ShowStatusBadge;
            if (node.GroupId != null) node.GroupId = groups[node.GroupId]; chart.Nodes.Add(node);
        }
        for (var index = 0; index < source.Edges.Count; index++) {
            var edge = RestoreTopologyEdge(source.Edges[index]); var state = hierarchy.SourceEdges[index];
            if (edge.SourcePortId != null) edge.SourcePortId = ports[edge.SourceNodeId][edge.SourcePortId];
            if (edge.TargetPortId != null) edge.TargetPortId = ports[edge.TargetNodeId][edge.TargetPortId];
            edge.Id = state.SourceId; edge.CssClass = state.CssClass;
            edge.SourceNodeId = endpoints[edge.SourceNodeId]; edge.TargetNodeId = endpoints[edge.TargetNodeId];
            if (edge.LabelAnchorNodeId != null) edge.LabelAnchorNodeId = nodes[edge.LabelAnchorNodeId]; chart.Edges.Add(edge);
        }
        for (var index = 0; index < source.Scenarios.Count; index++) {
            var scenario = RestoreTopologyScenario(source.Scenarios[index]); scenario.Id = hierarchy.SourceScenarios[index].SourceId;
            foreach (var step in scenario.Steps) step.Id = step.Kind == TopologyScenarioStepKind.Node ? nodes[step.Id] : edges[step.Id]; chart.Scenarios.Add(scenario);
        }
        for (var index = 0; index < source.Annotations.Count; index++) {
            var note = RestoreTopologyAnnotation(source.Annotations[index]); chart.Annotations.Add(new TopologyAnnotation(hierarchy.SourceAnnotations[index].SourceId,
                note.Text, note.TargetId == null ? null : endpoints[note.TargetId], note.Side));
        }
        RestoreTopologyAlignments(chart, source.Topology!.Alignments, nodes, hierarchy.SourceAlignments);
        chart.CrossingPaint = source.Topology.CrossingPaint;
        chart.PathDecorations.AddRange(source.Topology.PathDecorations);
        var validation = new TopologyChartValidator().ValidateAuthoredHierarchySource(chart); if (!validation.IsValid) throw new TopologyValidationException(validation);
        return chart;
    }

    private static VisualArtifactInterchangeTopologySourceIdentity SourceIdentity(string id, string sourceId, string? cssClass = null) => new() { Id = id, SourceId = sourceId, CssClass = cssClass };
    private static void CopyRawMetadata(IReadOnlyDictionary<string, string> source, IDictionary<string, string> target) {
        target.Clear(); foreach (var pair in source) target.Add(pair.Key, pair.Value);
    }
}
