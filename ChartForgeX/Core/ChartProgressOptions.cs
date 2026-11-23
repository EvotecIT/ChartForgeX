using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Progress bar and ring presentation. Configure it through <see cref="ChartOptions.Progress"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartProgressOptions {
    internal ChartProgressOptions Clone() => (ChartProgressOptions)MemberwiseClone();
}
