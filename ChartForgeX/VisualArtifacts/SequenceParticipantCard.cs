using ChartForgeX.Primitives;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

/// <summary>
/// The card look of a sequence participant: a topology icon on the left, the label with a subtitle below it, and an
/// optional status dot, so a sequence diagram reads like the topology it was projected from.
/// </summary>
public sealed class SequenceParticipantCard {
    /// <summary>Gets or sets the optional second line under the label.</summary>
    public string? Subtitle { get; set; }
    /// <summary>Gets or sets the built-in topology icon shape; <see cref="TopologyIconShape.Auto"/> derives it from <see cref="IconKind"/>.</summary>
    public TopologyIconShape IconShape { get; set; } = TopologyIconShape.Auto;
    /// <summary>Gets or sets the topology node kind the icon represents.</summary>
    public TopologyNodeKind IconKind { get; set; } = TopologyNodeKind.Generic;
    /// <summary>Gets or sets the icon color; null uses the participant border.</summary>
    public ChartColor? IconColor { get; set; }
    /// <summary>Gets or sets the status dot color; null draws no dot.</summary>
    public ChartColor? StatusColor { get; set; }
}