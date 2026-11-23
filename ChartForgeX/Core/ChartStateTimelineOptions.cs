using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>State timeline categories and summary. Configure it through <see cref="ChartOptions.StateTimeline"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartStateTimelineOptions {
    internal ChartStateTimelineOptions Clone() => (ChartStateTimelineOptions)MemberwiseClone();
}
