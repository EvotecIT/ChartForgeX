using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Topology;

/// <summary>
/// Represents one topology validation issue.
/// </summary>
public sealed class TopologyValidationIssue {
    /// <summary>Gets or sets the issue code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets a human-readable message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional item id.</summary>
    public string? ItemId { get; set; }
}

/// <summary>
/// Represents topology validation output.
/// </summary>
public sealed class TopologyValidationResult {
    /// <summary>Gets validation errors.</summary>
    public List<TopologyValidationIssue> Errors { get; } = new();

    /// <summary>Gets validation warnings.</summary>
    public List<TopologyValidationIssue> Warnings { get; } = new();

    /// <summary>Gets whether validation succeeded.</summary>
    public bool IsValid => Errors.Count == 0;
}

/// <summary>
/// Exception thrown when topology rendering receives invalid data.
/// </summary>
public sealed class TopologyValidationException : Exception {
    /// <summary>Initializes a new instance of the <see cref="TopologyValidationException"/> class.</summary>
    /// <param name="result">The validation result.</param>
    public TopologyValidationException(TopologyValidationResult result)
        : base(BuildMessage(result)) {
        Result = result;
    }

    /// <summary>Gets the validation result.</summary>
    public TopologyValidationResult Result { get; }

    private static string BuildMessage(TopologyValidationResult result) {
        if (result == null) return "Topology validation failed.";
        return "Topology validation failed: " + string.Join("; ", result.Errors.Select(error => error.Code + ": " + error.Message));
    }
}

/// <summary>
/// Validates topology chart models before rendering.
/// </summary>
public sealed class TopologyChartValidator {
    /// <summary>
    /// Validates a topology chart.
    /// </summary>
    /// <param name="chart">The chart to validate.</param>
    /// <returns>Structured validation output.</returns>
    public TopologyValidationResult Validate(TopologyChart chart) => Validate(chart, true);

    internal TopologyValidationResult Validate(TopologyChart chart, bool validateScenarioReferences) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var result = new TopologyValidationResult();
        ValidateViewport(chart, result);
        ValidateGroups(chart, result);
        ValidateNodes(chart, result);
        ValidateEdges(chart, result);
        ValidateScenarios(chart, result, validateScenarioReferences);
        return result;
    }

    internal TopologyValidationResult ValidateScenarioReferences(TopologyChart chart) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var result = new TopologyValidationResult();
        ValidateScenarios(chart, result, validateScenarioReferences: true);
        return result;
    }

    private static void ValidateViewport(TopologyChart chart, TopologyValidationResult result) {
        if (!IsPositive(chart.Viewport.Width)) Add(result, "viewport-width", "Viewport width must be a positive finite number.", null);
        if (!IsPositive(chart.Viewport.Height)) Add(result, "viewport-height", "Viewport height must be a positive finite number.", null);
        if (!IsFinite(chart.Viewport.Padding) || chart.Viewport.Padding < 0) Add(result, "viewport-padding", "Viewport padding must be a non-negative finite number.", null);
    }

    private static void ValidateGroups(TopologyChart chart, TopologyValidationResult result) {
        foreach (var group in chart.Groups) {
            if (string.IsNullOrWhiteSpace(group.Id)) Add(result, "group-id-empty", "Group id is required.", null);
            if (string.IsNullOrWhiteSpace(group.Label)) Add(result, "group-label-empty", "Group label is required.", group.Id);
            if (!IsPositive(group.Width)) Add(result, "group-width", "Group '" + group.Id + "' width must be positive.", group.Id);
            if (!IsPositive(group.Height)) Add(result, "group-height", "Group '" + group.Id + "' height must be positive.", group.Id);
            if (!IsFinite(group.X) || !IsFinite(group.Y)) Add(result, "group-coordinate", "Group '" + group.Id + "' coordinates must be finite.", group.Id);
            ValidateCoordinatePair(result, "group", group.Id, group.Longitude, group.Latitude);
        }

        foreach (var duplicate in chart.Groups.Where(group => !string.IsNullOrWhiteSpace(group.Id)).GroupBy(group => group.Id, StringComparer.Ordinal).Where(group => group.Count() > 1)) {
            Add(result, "duplicate-group-id", "Duplicate group id '" + duplicate.Key + "'.", duplicate.Key);
        }
    }

    private static void ValidateNodes(TopologyChart chart, TopologyValidationResult result) {
        var groupIds = new HashSet<string>(chart.Groups.Select(group => group.Id), StringComparer.Ordinal);
        foreach (var node in chart.Nodes) {
            if (string.IsNullOrWhiteSpace(node.Id)) Add(result, "node-id-empty", "Node id is required.", null);
            if (string.IsNullOrWhiteSpace(node.Label)) Add(result, "node-label-empty", "Node label is required.", node.Id);
            if (!IsPositive(node.Width)) Add(result, "node-width", "Node '" + node.Id + "' width must be positive.", node.Id);
            if (!IsPositive(node.Height)) Add(result, "node-height", "Node '" + node.Id + "' height must be positive.", node.Id);
            if (!IsFinite(node.X) || !IsFinite(node.Y)) Add(result, "node-coordinate", "Node '" + node.Id + "' coordinates must be finite.", node.Id);
            ValidateCoordinatePair(result, "node", node.Id, node.Longitude, node.Latitude);
            if (!string.IsNullOrWhiteSpace(node.GroupId) && !groupIds.Contains(node.GroupId!)) {
                Add(result, "missing-node-group", "Node '" + node.Id + "' references missing group '" + node.GroupId + "'.", node.Id);
            }
        }

        foreach (var duplicate in chart.Nodes.Where(node => !string.IsNullOrWhiteSpace(node.Id)).GroupBy(node => node.Id, StringComparer.Ordinal).Where(group => group.Count() > 1)) {
            Add(result, "duplicate-node-id", "Duplicate node id '" + duplicate.Key + "'.", duplicate.Key);
        }
    }

    private static void ValidateEdges(TopologyChart chart, TopologyValidationResult result) {
        var nodeIds = new HashSet<string>(chart.Nodes.Select(node => node.Id), StringComparer.Ordinal);
        foreach (var edge in chart.Edges) {
            if (string.IsNullOrWhiteSpace(edge.Id)) Add(result, "edge-id-empty", "Edge id is required.", null);
            if (string.IsNullOrWhiteSpace(edge.SourceNodeId)) Add(result, "edge-source-empty", "Edge '" + edge.Id + "' source node id is required.", edge.Id);
            if (string.IsNullOrWhiteSpace(edge.TargetNodeId)) Add(result, "edge-target-empty", "Edge '" + edge.Id + "' target node id is required.", edge.Id);
            if (!string.IsNullOrWhiteSpace(edge.SourceNodeId) && !nodeIds.Contains(edge.SourceNodeId)) {
                Add(result, "missing-edge-source", "Edge '" + edge.Id + "' references missing source node '" + edge.SourceNodeId + "'.", edge.Id);
            }

            if (!string.IsNullOrWhiteSpace(edge.TargetNodeId) && !nodeIds.Contains(edge.TargetNodeId)) {
                Add(result, "missing-edge-target", "Edge '" + edge.Id + "' references missing target node '" + edge.TargetNodeId + "'.", edge.Id);
            }
        }
    }

    private static void ValidateScenarios(TopologyChart chart, TopologyValidationResult result, bool validateScenarioReferences) {
        var nodeIds = new HashSet<string>(chart.Nodes.Where(node => !string.IsNullOrWhiteSpace(node.Id)).Select(node => node.Id), StringComparer.Ordinal);
        var edgeIds = new HashSet<string>(chart.Edges.Where(edge => !string.IsNullOrWhiteSpace(edge.Id)).Select(edge => edge.Id), StringComparer.Ordinal);
        foreach (var scenario in chart.Scenarios) {
            if (string.IsNullOrWhiteSpace(scenario.Id)) Add(result, "scenario-id-empty", "Scenario id is required.", null);
            else if (!IsScenarioToken(scenario.Id)) Add(result, "scenario-id-token", "Scenario id '" + scenario.Id + "' may contain only letters, digits, dots, underscores, and hyphens.", scenario.Id);
            if (string.IsNullOrWhiteSpace(scenario.Label)) Add(result, "scenario-label-empty", "Scenario label is required.", scenario.Id);
            if (scenario.Steps.Count == 0) Add(result, "scenario-empty", "Scenario '" + scenario.Id + "' must reference at least one node or edge.", scenario.Id);
            foreach (var step in scenario.Steps) {
                if (string.IsNullOrWhiteSpace(step.Id)) {
                    Add(result, "scenario-step-id-empty", "Scenario '" + scenario.Id + "' contains an empty step id.", scenario.Id);
                    continue;
                }

                if (!Enum.IsDefined(typeof(TopologyScenarioStepKind), step.Kind)) {
                    Add(result, "scenario-step-kind", "Scenario '" + scenario.Id + "' contains an undefined step kind.", scenario.Id);
                    continue;
                }

                if (!validateScenarioReferences) continue;
                if (step.Kind == TopologyScenarioStepKind.Node && !nodeIds.Contains(step.Id)) {
                    Add(result, "missing-scenario-node", "Scenario '" + scenario.Id + "' references missing node '" + step.Id + "'.", scenario.Id);
                } else if (step.Kind == TopologyScenarioStepKind.Edge && !edgeIds.Contains(step.Id)) {
                    Add(result, "missing-scenario-edge", "Scenario '" + scenario.Id + "' references missing edge '" + step.Id + "'.", scenario.Id);
                }
            }
        }

        foreach (var duplicate in chart.Scenarios.Where(scenario => !string.IsNullOrWhiteSpace(scenario.Id)).GroupBy(scenario => scenario.Id, StringComparer.Ordinal).Where(group => group.Count() > 1)) {
            Add(result, "duplicate-scenario-id", "Duplicate scenario id '" + duplicate.Key + "'.", duplicate.Key);
        }
    }

    private static bool IsScenarioToken(string value) {
        foreach (var ch in value.Trim()) {
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.') continue;
            return false;
        }

        return true;
    }

    private static bool IsPositive(double value) => IsFinite(value) && value > 0;

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static void ValidateCoordinatePair(TopologyValidationResult result, string itemKind, string itemId, double? longitude, double? latitude) {
        if (longitude.HasValue != latitude.HasValue) {
            Add(result, itemKind + "-geo-coordinate-pair", CultureInfo.InvariantCulture.TextInfo.ToTitleCase(itemKind) + " '" + itemId + "' must set both longitude and latitude.", itemId);
            return;
        }

        if (!longitude.HasValue || !latitude.HasValue) return;
        if (!IsFinite(longitude.Value) || longitude.Value < -180 || longitude.Value > 180) Add(result, itemKind + "-longitude", CultureInfo.InvariantCulture.TextInfo.ToTitleCase(itemKind) + " '" + itemId + "' longitude must be between -180 and 180 degrees.", itemId);
        if (!IsFinite(latitude.Value) || latitude.Value < -90 || latitude.Value > 90) Add(result, itemKind + "-latitude", CultureInfo.InvariantCulture.TextInfo.ToTitleCase(itemKind) + " '" + itemId + "' latitude must be between -90 and 90 degrees.", itemId);
    }

    private static void Add(TopologyValidationResult result, string code, string message, string? itemId) {
        result.Errors.Add(new TopologyValidationIssue {
            Code = code,
            Message = message,
            ItemId = string.IsNullOrWhiteSpace(itemId) ? null : itemId
        });
    }
}
