using System;
using ChartForgeX.Accessibility;
using ChartForgeX.Core;

namespace ChartForgeX.Topology;

public sealed partial class TopologyChart {
    /// <summary>Gets accessibility metadata shared by capable renderers and hosts.</summary>
    public VisualAccessibility Accessibility { get; private set; } = new();

    /// <summary>Installs an independent accessibility snapshot after successful configuration.</summary>
    /// <remarks>Callback exceptions preserve the owner. Previous values and retained callback values remain independent of the installed result.</remarks>
    public TopologyChart ConfigureAccessibility(Action<VisualAccessibility> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        Accessibility = ChartOptionConfiguration.Configure(Accessibility, configure, options => options.Clone());
        return this;
    }

    /// <summary>Gets the words the renderers write on their own: the name of an untitled diagram and its automatic description.</summary>
    public TopologyLabels Labels { get; internal set; } = new();

    /// <summary>Installs an independent label snapshot after successful configuration (see <see cref="TopologyLabels"/>).</summary>
    /// <remarks>Callback exceptions preserve the owner. Previous values and retained callback values remain independent of the installed result.</remarks>
    public TopologyChart ConfigureLabels(Action<TopologyLabels> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        Labels = ChartOptionConfiguration.Configure(Labels, configure, options => options.Clone());
        return this;
    }

    /// <summary>Marks the topology as decorative for capable renderers.</summary>
    public TopologyChart AsDecorative(bool decorative = true) {
        Accessibility.IsDecorative = decorative;
        return this;
    }
}
