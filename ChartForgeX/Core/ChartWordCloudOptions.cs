using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Word cloud sizing, angles and density. Configure it through <see cref="ChartOptions.WordCloud"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartWordCloudOptions {
    internal ChartWordCloudOptions Clone() => (ChartWordCloudOptions)MemberwiseClone();
}
