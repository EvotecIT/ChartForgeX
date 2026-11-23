using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Gantt markers, ticks and open ends. Configure it through <see cref="ChartOptions.Gantt"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartGanttOptions {
    internal ChartGanttOptions Clone() => (ChartGanttOptions)MemberwiseClone();
}
