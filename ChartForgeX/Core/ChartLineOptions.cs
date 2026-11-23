using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Line styling, markers and orientation. Configure it through <see cref="ChartOptions.Lines"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartLineOptions {
    internal ChartLineOptions Clone() => (ChartLineOptions)MemberwiseClone();
}
