using System;
using PowerForge.Fluent;

namespace ChartForgeX.Topology;

/// <summary>Interactive HTML page controls, panels and content security settings. Configure it through <see cref="TopologyRenderOptions.Html"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class TopologyHtmlOptions {
    internal TopologyHtmlOptions Clone() => (TopologyHtmlOptions)MemberwiseClone();
}
