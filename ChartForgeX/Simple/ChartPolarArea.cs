using System.Collections.Generic;

namespace ChartForgeX.Simple;

/// <summary>Polar area chart definition.</summary>
public sealed class ChartPolarArea : ChartDefinition {
    /// <summary>Segment values.</summary>
    public IList<double> Value { get; }

    /// <summary>Optional segment labels.</summary>
    public IList<string>? Labels { get; }

    /// <summary>Create a polar area chart definition.</summary>
    /// <param name="name">Series name.</param>
    /// <param name="value">Segment values.</param>
    /// <param name="labels">Optional segment labels.</param>
    public ChartPolarArea(string name, IList<double> value, IList<string>? labels = null) : base(name) {
        Value = value;
        Labels = labels;
    }
}
