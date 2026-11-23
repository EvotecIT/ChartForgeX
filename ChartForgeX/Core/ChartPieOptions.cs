using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Pie and donut slices, labels and centre text. Configure it through <see cref="ChartOptions.Pie"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartPieOptions {
    internal ChartPieOptions Clone() => (ChartPieOptions)MemberwiseClone();
}
