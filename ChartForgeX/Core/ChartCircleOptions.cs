using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Circle and status-ring presentation. Configure it through <see cref="ChartOptions.Circle"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartCircleOptions {
    internal ChartCircleOptions Clone() => (ChartCircleOptions)MemberwiseClone();
}
