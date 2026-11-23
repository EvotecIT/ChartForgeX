using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Data label visibility, placement and leader-line connectors. Configure it through <see cref="ChartOptions.DataLabels"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartDataLabelOptions {
    internal ChartDataLabelOptions Clone() => (ChartDataLabelOptions)MemberwiseClone();
}
