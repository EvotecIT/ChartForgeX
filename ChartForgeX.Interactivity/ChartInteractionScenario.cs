using System.Collections.Generic;

namespace ChartForgeX.Interactivity;

/// <summary>
/// Describes a named interactive path, flow, or alternate analytical state for a chart adapter.
/// </summary>
public sealed class ChartInteractionScenario {
    private string _id = string.Empty;
    private string _label = string.Empty;

    /// <summary>Gets or sets the stable scenario identifier.</summary>
    public string Id { get => _id; set => _id = ChartInteractionText.RequiredToken(value, nameof(value), "Interaction scenario ids"); }

    /// <summary>Gets or sets the scenario label shown by host controls.</summary>
    public string Label { get => _label; set => _label = ChartInteractionText.RequiredText(value, nameof(value), "Interaction scenario labels"); }

    /// <summary>Gets or sets an optional scenario description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets an optional scenario accent color.</summary>
    public string? Color { get; set; }

    /// <summary>Gets ordered target references that participate in the scenario.</summary>
    public List<ChartInteractionScenarioStep> Steps { get; } = new();

    /// <summary>Gets arbitrary scenario metadata for host adapters.</summary>
    public Dictionary<string, string> Metadata { get; } = new();
}

/// <summary>
/// Describes one target reference inside a host-neutral interaction scenario.
/// </summary>
public sealed class ChartInteractionScenarioStep {
    private string _targetKind = string.Empty;
    private string _targetId = string.Empty;

    /// <summary>Gets or sets the adapter-defined target kind, such as series, point, node, edge, or annotation.</summary>
    public string TargetKind { get => _targetKind; set => _targetKind = ChartInteractionText.RequiredToken(value, nameof(value), "Interaction scenario target kinds"); }

    /// <summary>Gets or sets the adapter-defined target identifier.</summary>
    public string TargetId { get => _targetId; set => _targetId = ChartInteractionText.RequiredText(value, nameof(value), "Interaction scenario target ids"); }

    /// <summary>Gets or sets an optional step label.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets an optional step description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets arbitrary step metadata for route inspectors and host adapters.</summary>
    public Dictionary<string, string> Metadata { get; } = new();
}
