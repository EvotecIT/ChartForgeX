using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

public sealed partial class TopologyChartValidator {
    internal TopologyValidationResult ValidateAlignmentInputs(TopologyChart chart) {
        CheckAlignmentInputWork(chart);
        return Validate(chart, validateScenarioReferences: false, allowAutomaticGroupDimensions: true, validateAlignmentGeometry: false);
    }

    private static void CheckAlignmentInputWork(TopologyChart chart) {
        if (chart.Alignments.Count == 0) return;
        var id = chart.Alignments[0]?.Id ?? string.Empty;
        Bound(chart.Nodes.Count, ChartForgeX.VisualArtifacts.VisualArtifactInterchangeValidation.MaximumNodes, "Interchange.MaximumNodes");
        Bound(chart.Groups.Count, ChartForgeX.VisualArtifacts.VisualArtifactInterchangeValidation.MaximumGroups, "Interchange.MaximumGroups");
        Bound(chart.Edges.Count, ChartForgeX.VisualArtifacts.VisualArtifactInterchangeValidation.MaximumEdges, "Interchange.MaximumEdges");
        Bound(chart.Alignments.Count, ChartForgeX.VisualArtifacts.VisualArtifactInterchangeValidation.MaximumAnnotations, "Interchange.MaximumAnnotations");
        var budget = new TopologyAlignmentBudget(id);
        budget.Work((long)chart.Nodes.Count + chart.Groups.Count + chart.Edges.Count + chart.Alignments.Count);
        foreach (var alignment in chart.Alignments) if (alignment != null) {
            Bound(alignment.NodeIds.Count, ChartForgeX.VisualArtifacts.VisualArtifactInterchangeValidation.MaximumNodes, "Interchange.MaximumNodes");
            budget.Work(alignment.NodeIds.Count);
        }
        void Bound(int count, int maximum, string name) {
            if (count > maximum) throw new TopologyAlignmentLimitException(name, maximum, count, id);
        }
    }

    private static void ValidateAlignments(TopologyChart chart, TopologyValidationResult result, bool validateGeometry) {
        if (chart.Alignments.Count == 0) return;
        CheckAlignmentInputWork(chart);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var nodes = new HashSet<string>(chart.Nodes.Select(node => node.Id), StringComparer.Ordinal);
        foreach (var alignment in chart.Alignments) {
            if (alignment == null) { Add(result, "alignment-null", "Alignment declarations must not be null.", null); continue; }
            if (string.IsNullOrWhiteSpace(alignment.Id)) Add(result, "alignment-id", "Alignment ID is required.", alignment.Id);
            if (!ids.Add(alignment.Id)) Add(result, "alignment-duplicate-id", "Alignment IDs must be unique.", alignment.Id);
            if (!Enum.IsDefined(typeof(TopologyAlignmentKind), alignment.Kind)) Add(result, "alignment-kind", "Alignment kind must be Row or Column.", alignment.Id);
            if (alignment.NodeIds.Count < 2) Add(result, "alignment-members", "An alignment requires at least two distinct nodes.", alignment.Id);
            var members = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in alignment.NodeIds) {
                if (string.IsNullOrWhiteSpace(id) || !nodes.Contains(id)) Add(result, "alignment-reference", "Alignment member must be an existing node.", alignment.Id);
                if (!members.Add(id)) Add(result, "alignment-duplicate-member", "Alignment must not repeat a member.", alignment.Id);
            }
        }
        if (validateGeometry && result.IsValid && chart.LayoutMode == TopologyLayoutMode.Manual) TopologyAlignmentLayout.ValidateManual(chart, result);
    }
}
