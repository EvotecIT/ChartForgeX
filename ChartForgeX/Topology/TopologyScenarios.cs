using System;
using System.Collections.Generic;

namespace ChartForgeX.Topology;

/// <summary>
/// Describes an interactive path or story through a topology chart.
/// </summary>
public sealed class TopologyScenario {
    private string _id = string.Empty;
    private string _label = string.Empty;

    /// <summary>Gets or sets the stable scenario identifier.</summary>
    public string Id { get => _id; set => _id = RequiredToken(value, nameof(value), "Topology scenario ids"); }

    /// <summary>Gets or sets the scenario label used by host controls.</summary>
    public string Label { get => _label; set => _label = RequiredText(value, nameof(value), "Topology scenario labels"); }

    /// <summary>Gets or sets an optional scenario description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets an optional scenario accent color.</summary>
    public string? Color { get; set; }

    /// <summary>Gets ordered node and edge references that participate in the scenario.</summary>
    public List<TopologyScenarioStep> Steps { get; } = new();

    /// <summary>Gets arbitrary scenario metadata for host adapters.</summary>
    public Dictionary<string, string> Metadata { get; } = new();

    private static string RequiredText(string? value, string parameterName, string displayName) {
        if (value == null) throw new ArgumentNullException(parameterName);
        var trimmed = value.Trim();
        if (trimmed.Length == 0) throw new ArgumentException(displayName + " must not be empty.", parameterName);
        return trimmed;
    }

    private static string RequiredToken(string? value, string parameterName, string displayName) {
        var trimmed = RequiredText(value, parameterName, displayName);
        foreach (var ch in trimmed) {
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.') continue;
            throw new ArgumentException(displayName + " may contain only letters, digits, dots, underscores, and hyphens.", parameterName);
        }

        return trimmed;
    }
}

/// <summary>
/// Describes one node or edge reference inside a topology scenario.
/// </summary>
public sealed class TopologyScenarioStep {
    private string _id = string.Empty;
    private TopologyScenarioStepKind _kind;

    /// <summary>Gets or sets the referenced node or edge id.</summary>
    public string Id { get => _id; set => _id = RequiredText(value, nameof(value), "Topology scenario step ids"); }

    /// <summary>Gets or sets whether this step references a node or an edge.</summary>
    public TopologyScenarioStepKind Kind {
        get => _kind;
        set {
            if (!Enum.IsDefined(typeof(TopologyScenarioStepKind), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Topology scenario step kind must be defined.");
            _kind = value;
        }
    }

    /// <summary>Gets or sets an optional step label.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets an optional step description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets arbitrary step metadata for route inspectors and host adapters.</summary>
    public Dictionary<string, string> Metadata { get; } = new();

    private static string RequiredText(string? value, string parameterName, string displayName) {
        if (value == null) throw new ArgumentNullException(parameterName);
        var trimmed = value.Trim();
        if (trimmed.Length == 0) throw new ArgumentException(displayName + " must not be empty.", parameterName);
        return trimmed;
    }
}
