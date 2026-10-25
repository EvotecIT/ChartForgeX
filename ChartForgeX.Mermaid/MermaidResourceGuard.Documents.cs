using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidResourceGuard {
    /// <summary>Counts every parsed family explicitly without reflection, before native conversion can expand it.</summary>
    internal static void ValidateDocument(MermaidDocument document) {
        Checkpoint();
        if (Current.Value is not { } context) return;
        long elements = 0, edges = 0;
        switch (document) {
            case MermaidZenUmlDocument zen:
                if (!zen.ContainsSyntaxErrors) MermaidZenUmlModel.Validate(zen, AddElements, AddEdges);
                break;
            case MermaidRailroadDocument railroad:
                Items(railroad.Rules);
                var expressions = new Stack<(MermaidRailroadExpression Expression, int Depth)>();
                foreach (var rule in railroad.Rules) expressions.Push((rule.Definition, 1));
                while (expressions.Count > 0) {
                    var current = expressions.Pop(); AddElements(1, current.Expression.Span); CheckDepth(current.Depth, current.Expression.Span);
                    AddEdges(current.Expression.Children.Count, current.Expression.Span);
                    foreach (var child in current.Expression.Children) expressions.Push((child, current.Depth + 1));
                }
                break;
            case MermaidFlowchartDocument flow:
                Items(flow.Nodes); Items(flow.Subgraphs); Items(flow.ClassDefinitions); Items(flow.LinkStyles); Links(flow.Edges);
                foreach (var definition in flow.ClassDefinitions) AddElements(definition.Styles.Count, definition.Span);
                foreach (var node in flow.Nodes) AddElements(node.Classes.Count + node.Styles.Count + node.Properties.Count, node.Span);
                foreach (var group in flow.Subgraphs) AddElements(group.Classes.Count + group.Styles.Count, group.Span);
                if (flow is MermaidUseCaseDocument useCase) Items(useCase.Notes);
                foreach (var style in flow.LinkStyles) AddElements(style.Styles.Count, style.Span);
                foreach (var edge in flow.Edges) {
                    AddElements(edge.Styles.Count + edge.Classes.Count, edge.Span);
                    if (edge.Id != null) CheckInterchangeFactLength(edge.Id.Length, edge.IdentitySpan ?? edge.Span);
                }
                ParentDepths(flow.Subgraphs.Select(group => (group.Id, group.ParentSubgraphId, group.Span)), stopAtRepeatedParent: true);
                break;
            case MermaidSequenceDocument sequence:
                Items(sequence.Participants); Items(sequence.Notes); Items(sequence.Blocks); Items(sequence.Activations);
                Items(sequence.Lifetimes); Items(sequence.ParticipantBoxes); Items(sequence.Links); Items(sequence.PropertyStatements); Items(sequence.AutonumberStatements); Links(sequence.Messages);
                foreach (var block in sequence.Blocks) CheckDepth(block.Depth + 1, block.Span);
                break;
            case MermaidClassDocument classes:
                Items(classes.Classes); Items(classes.Notes); Items(classes.StyleStatements); Items(classes.ClassDefinitions); Items(classes.Namespaces); Links(classes.Relationships);
                ParentDepths(classes.Namespaces.Select(group => (group.Id, group.ParentId, group.Span)));
                foreach (var definition in classes.ClassDefinitions) AddElements(definition.Styles.Count, definition.Span);
                var namespaces = new HashSet<string>(classes.Namespaces.Select(group => group.Id), StringComparer.Ordinal);
                foreach (var item in classes.Classes) {
                    Items(item.Members); AddElements(item.Classes.Count + item.Styles.Count, item.Span);
                    if (item.Namespace == null) continue;
                    string? parent = null;
                    var depth = 0;
                    foreach (var part in item.Namespace.Split('.')) {
                        CheckDepth(++depth, item.Span);
                        var id = parent == null ? part : parent + "." + part;
                        if (namespaces.Add(id)) AddElements(1, item.Span);
                        parent = id;
                    }
                }
                break;
            case MermaidStateDocument state:
                Items(state.States); Items(state.Notes); Items(state.Regions); Items(state.ClassDefinitions); Items(state.StyleStatements); Links(state.Transitions);
                ParentDepths(state.States.Select(node => (node.Id, node.ParentId, node.Span)));
                foreach (var definition in state.ClassDefinitions) AddElements(definition.Styles.Count, definition.Span);
                foreach (var node in state.States) AddElements(node.Classes.Count + node.Styles.Count, node.Span);
                break;
            case MermaidEntityRelationshipDocument entity:
                Items(entity.Entities); Items(entity.Groups); Items(entity.ClassDefinitions); Items(entity.StyleStatements); Links(entity.Relationships);
                ParentDepths(entity.Groups.Select(group => (group.Id, group.ParentId, group.Span)));
                foreach (var definition in entity.ClassDefinitions) AddElements(definition.Styles.Count, definition.Span);
                foreach (var group in entity.Groups) AddElements(group.Classes.Count + group.Styles.Count, group.Span);
                foreach (var node in entity.Entities) { Items(node.Attributes); AddElements(node.Classes.Count + node.Styles.Count, node.Span); }
                break;
            case MermaidAgentflowDocument agentflow:
                Items(agentflow.Nodes); Items(agentflow.Flows); Links(agentflow.Edges);
                Items(agentflow.ClassDefinitions);
                foreach (var definition in agentflow.ClassDefinitions) AddElements(definition.Styles.Count, definition.Span);
                ParentDepths(agentflow.Flows.Select(flow => (flow.Id, flow.ParentId, flow.Span))
                    .Concat(agentflow.Nodes.Select(node => (node.Id, node.IsGlobal ? null : node.ParentFlowId, node.Span))));
                foreach (var node in agentflow.Nodes) { Metadata(node.Metadata); AddElements(node.Classes.Count + node.Styles.Count, node.Span); }
                foreach (var flow in agentflow.Flows) { Metadata(flow.Metadata); AddElements(flow.Classes.Count + flow.Styles.Count, flow.Span); }
                foreach (var edge in agentflow.Edges) { Metadata(edge.Metadata); AddElements(edge.Classes.Count + edge.Styles.Count, edge.Span); }
                break;
            case MermaidPieDocument pie: Spans(pie.Slices.Select(item => item.Span)); break;
            case MermaidJourneyDocument journey:
                Items(journey.Sections); Items(journey.Tasks);
                foreach (var task in journey.Tasks) AddElements(task.Actors.Count, task.Span);
                break;
            case MermaidGitGraphDocument git:
                Spans(git.Branches.Select(item => item.Span)); Spans(git.Commits.Select(item => item.Span));
                foreach (var commit in git.Commits) AddEdges(commit.ParentIds.Count, commit.Span);
                break;
            case MermaidGanttDocument gantt:
                Items(gantt.Sections); Items(gantt.Tasks);
                foreach (var task in gantt.Tasks) AddEdges(task.DependencyIds.Count, task.Span);
                break;
            case MermaidMindMapDocument mind:
                Items(mind.Nodes);
                foreach (var node in mind.Nodes) {
                    CheckDepth(node.Level + 1, node.Span);
                    AddElements(node.Classes.Count + node.Icons.Count, node.Span);
                    if (node.ParentId != null) AddEdges(1, node.Span);
                }
                break;
            case MermaidTimelineDocument timeline:
                Items(timeline.Sections); Items(timeline.Periods);
                foreach (var period in timeline.Periods) Items(period.Events);
                break;
            case MermaidRequirementDocument requirement:
                Items(requirement.Requirements); Items(requirement.Elements); Items(requirement.StyleStatements); Items(requirement.ClassDefinitions); Links(requirement.Relationships);
                foreach (var definition in requirement.ClassDefinitions) AddElements(definition.Styles.Count, definition.Span);
                foreach (var item in requirement.Requirements) AddElements(item.Classes.Count + item.Styles.Count, item.Span);
                foreach (var item in requirement.Elements) AddElements(item.Classes.Count + item.Styles.Count, item.Span);
                break;
            case MermaidQuadrantDocument quadrant:
                Items(quadrant.Points); Items(quadrant.ClassDefinitions);
                foreach (var definition in quadrant.ClassDefinitions) AddElements(definition.Styles.Count, definition.Span);
                foreach (var point in quadrant.Points) AddElements(point.Styles.Count + (point.ClassName == null ? 0 : 1), point.Span);
                break;
            case MermaidXYChartDocument xy:
                Items(xy.Series); AddElements(xy.XAxis.Labels.Count, xy.HeaderSpan);
                foreach (var series in xy.Series) {
                    AddElements(series.Values.Count, series.Span);
                    Items(series.PointLabels);
                }
                break;
            case MermaidSankeyDocument sankey:
                Links(sankey.Links);
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var link in sankey.Links) {
                    if (names.Add(link.Source)) AddElements(1, link.Span);
                    if (names.Add(link.Target)) AddElements(1, link.Span);
                }
                break;
            case MermaidRadarDocument radar:
                Items(radar.Axes); Items(radar.Curves);
                foreach (var curve in radar.Curves) AddElements(curve.OrderedValues.Count + curve.ValuesByAxisId.Count, curve.Span);
                break;
            case MermaidTreemapDocument treemap:
                Items(treemap.Nodes); Items(treemap.ClassDefinitions);
                foreach (var definition in treemap.ClassDefinitions) AddElements(definition.Styles.Count, definition.Span);
                foreach (var node in treemap.Nodes) {
                    var depth = 0;
                    for (var current = node; current != null; current = current.Parent) CheckDepth(++depth, node.Span);
                    if (node.Parent != null) AddEdges(1, node.Span);
                }
                break;
            case MermaidKanbanDocument kanban:
                Items(kanban.Columns);
                foreach (var column in kanban.Columns) {
                    AddElements(column.Metadata.Count, column.Span); Items(column.Tasks);
                    foreach (var task in column.Tasks) AddElements(task.Metadata.Count, task.Span);
                }
                break;
            case MermaidArchitectureDocument architecture:
                Items(architecture.Groups); Items(architecture.Services); Items(architecture.Junctions); Links(architecture.Edges);
                Items(architecture.Alignments); foreach (var alignment in architecture.Alignments) {
                    MermaidArchitectureAlignment.CheckMemberCount(alignment.Members.Count, alignment.Span);
                    AddElements(alignment.Members.Count, alignment.Span);
                    foreach (var id in alignment.Members) if (id != null && id.Length > ChartForgeX.VisualArtifacts.VisualArtifactInterchangeValidation.MaximumIdCharacters)
                        throw new MermaidResourceLimitException("Interchange.MaximumIdCharacters", ChartForgeX.VisualArtifacts.VisualArtifactInterchangeValidation.MaximumIdCharacters, id.Length, alignment.Span);
                }
                ParentDepths(architecture.Groups.Select(group => (group.Id, group.ParentId, group.Span)));
                break;
            case MermaidC4Document c4:
                Items(c4.Boundaries); Items(c4.Elements); Items(c4.ElementStyles); Items(c4.RelationshipStyles); Links(c4.Relationships);
                ParentDepths(c4.Boundaries.Select(group => (group.Id, group.ParentId, group.Span)));
                break;
            case MermaidBlockDocument block:
                Spans(block.Items.Select(item => item.Span)); Items(block.ClassDefinitions); Items(block.StyleStatements);
                ParentDepths(block.Items.Select(item => (item.Id, item.ParentId, item.Span)));
                foreach (var definition in block.ClassDefinitions) AddElements(definition.Styles.Count, definition.Span);
                foreach (var item in block.Items) AddElements(item.Classes.Count + item.Styles.Count, item.Span);
                foreach (var edge in block.Edges) AddEdges(1, edge.Span);
                break;
            case MermaidPacketDocument packet: Spans(packet.Fields.Select(item => item.Span)); break;
            case MermaidVennDocument venn:
                Spans(venn.Sets.Select(item => item.Span)); Spans(venn.Intersections.Select(item => item.Span)); Spans(venn.TextNodes.Select(item => item.Span));
                break;
            case MermaidIshikawaDocument fishbone:
                var pending = new Stack<MermaidIshikawaNode>();
                if (fishbone.Root != null) pending.Push(fishbone.Root);
                while (pending.Count > 0) {
                    var node = pending.Pop(); AddElements(1, node.Span); CheckDepth(node.Level + 1, node.Span);
                    foreach (var child in node.Children) { pending.Push(child); AddEdges(1, child.Span); }
                }
                break;
            case MermaidWardleyDocument wardley:
                if (wardley.AnnotationsBox != null) Spans(new[] { wardley.AnnotationsBox.Span });
                Spans(wardley.Nodes.Select(item => item.Span)); Spans(wardley.Notes.Select(item => item.Span));
                Spans(wardley.Annotations.Select(item => item.Span)); Spans(wardley.Markers.Select(item => item.Span)); Spans(wardley.Pipelines.Select(item => item.Span));
                foreach (var link in wardley.Links) AddEdges(1, link.Span);
                foreach (var evolution in wardley.Evolutions) AddEdges(1, evolution.Span);
                foreach (var pipeline in wardley.Pipelines) Spans(pipeline.Components.Select(item => item.Span));
                break;
            case MermaidTreeViewDocument tree:
                Items(tree.Nodes);
                foreach (var node in tree.Nodes) { CheckDepth(node.Level + 1, node.Span); if (node.Parent != null) AddEdges(1, node.Span); }
                break;
            case MermaidEventModelingDocument events: Items(events.Frames); Items(events.DataBlocks); Links(events.Relations); break;
            case MermaidCynefinDocument cynefin: Items(cynefin.RawStatements); Links(cynefin.Transitions); break;
            default: Items(document.RawStatements); break;
        }
        Checkpoint();
        // A fresh conversion scope starts with the retained source model. Later
        // named/default style expansion must fit alongside those existing records.
        context.ReservedElements = Math.Max(context.ReservedElements, elements);

        void Items(IEnumerable<MermaidAstNode> items) { foreach (var item in items) AddElements(1, item.Span); }
        void Spans(IEnumerable<MermaidSourceSpan> spans) { foreach (var span in spans) AddElements(1, span); }
        void Links(IEnumerable<MermaidAstNode> items) { foreach (var item in items) AddEdges(1, item.Span); }
        void AddElements(long count, MermaidSourceSpan span) {
            Checkpoint(); elements += count;
            Check(nameof(MermaidResourceLimits.MaximumElements), context.Limits.MaximumElements, elements, span);
        }
        void AddEdges(long count, MermaidSourceSpan span) {
            Checkpoint(); edges += count;
            Check(nameof(MermaidResourceLimits.MaximumEdges), context.Limits.MaximumEdges, edges, span);
        }
        void Metadata(Dictionary<string, MermaidMetadataValue> metadata) {
            var pending = new Stack<(MermaidMetadataValue Value, int Depth)>();
            foreach (var pair in metadata) { AddElements(1, pair.Value.Span); pending.Push((pair.Value, 1)); }
            while (pending.Count > 0) {
                var current = pending.Pop();
                AddElements(1, current.Value.Span); CheckDepth(current.Depth, current.Value.Span);
                foreach (var pair in current.Value.Properties) {
                    AddElements(1, pair.Value.Span); pending.Push((pair.Value, current.Depth + 1));
                }
                foreach (var value in current.Value.Items) pending.Push((value, current.Depth + 1));
            }
        }
        void ParentDepths(IEnumerable<(string Id, string? ParentId, MermaidSourceSpan Span)> groups, bool stopAtRepeatedParent = false) {
            var parents = new Dictionary<string, (string? ParentId, MermaidSourceSpan Span)>(StringComparer.Ordinal);
            foreach (var group in groups) if (!parents.ContainsKey(group.Id)) parents.Add(group.Id, (group.ParentId, group.Span));
            foreach (var group in parents) {
                var parent = group.Value.ParentId; var depth = 1;
                var visited = stopAtRepeatedParent ? new HashSet<string>(StringComparer.Ordinal) { group.Key } : null;
                CheckDepth(depth, group.Value.Span);
                while (parent != null && parents.TryGetValue(parent, out var next)) {
                    // Flowchart finalization reports genuine parent cycles as located invalid source.
                    if (visited != null && !visited.Add(parent)) break;
                    CheckDepth(++depth, group.Value.Span); parent = next.ParentId;
                }
            }
        }
    }
}
