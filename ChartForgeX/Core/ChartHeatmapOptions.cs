using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Heatmap scales, cells and labels. Configure it through <see cref="ChartOptions.Heatmap"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartHeatmapOptions {
    internal ChartHeatmapOptions Clone() => (ChartHeatmapOptions)MemberwiseClone();
}
