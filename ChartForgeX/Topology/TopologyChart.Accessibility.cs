using System;
using ChartForgeX.Accessibility;

namespace ChartForgeX.Topology;

public sealed partial class TopologyChart {
    /// <summary>Gets accessibility metadata shared by capable renderers and hosts.</summary>
    public VisualAccessibility Accessibility { get; } = new();

    /// <summary>Configures the topology text alternative and language metadata.</summary>
    public TopologyChart WithAccessibility(Action<VisualAccessibility> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(Accessibility);
        return this;
    }

    /// <summary>Gets the words the renderers write on their own: the name of an untitled diagram and its automatic description.</summary>
    public TopologyLabels Labels { get; internal set; } = new();

    /// <summary>Configures the words the renderers write on their own (see <see cref="TopologyLabels"/>).</summary>
    public TopologyChart WithLabels(Action<TopologyLabels> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(Labels);
        return this;
    }

    /// <summary>Marks the topology as decorative for capable renderers.</summary>
    public TopologyChart AsDecorative(bool decorative = true) {
        Accessibility.IsDecorative = decorative;
        return this;
    }
}
