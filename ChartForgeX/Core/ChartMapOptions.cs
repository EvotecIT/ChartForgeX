using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Map viewport, layers, labels and colour scales. Configure it through <see cref="ChartOptions.Map"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartMapOptions {
    internal ChartMapOptions Clone() => (ChartMapOptions)MemberwiseClone();
}
