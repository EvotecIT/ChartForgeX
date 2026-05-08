using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>
    /// Gets x-axis tick label highlight colors keyed by axis value.
    /// </summary>
    public Dictionary<double, ChartColor> XAxisLabelHighlights { get; } = new();
}
