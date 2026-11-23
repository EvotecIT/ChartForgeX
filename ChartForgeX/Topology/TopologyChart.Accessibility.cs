using System;
using ChartForgeX.Accessibility;
using ChartForgeX.Core;

namespace ChartForgeX.Topology;

public sealed partial class TopologyChart {
    /// <summary>Gets accessibility metadata shared by capable renderers and hosts.</summary>
    public VisualAccessibility Accessibility {
        get => _accessibility;
        set => _accessibility = value ?? throw new ArgumentNullException(nameof(value));
    }
    private VisualAccessibility _accessibility = new();

    /// <summary>Gets the words the renderers write on their own: the name of an untitled diagram and its automatic description.</summary>
    public TopologyLabels Labels {
        get => _labels;
        set => _labels = value ?? throw new ArgumentNullException(nameof(value));
    }
    private TopologyLabels _labels = new();
}
