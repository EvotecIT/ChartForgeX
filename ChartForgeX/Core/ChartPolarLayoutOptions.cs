using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Polar axis and grid layout. Configure it through <see cref="ChartOptions.Polar"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartPolarLayoutOptions {
    internal ChartPolarLayoutOptions Clone() => (ChartPolarLayoutOptions)MemberwiseClone();
}
