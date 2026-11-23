using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Bar layout, stacking and styling. Configure it through <see cref="ChartOptions.Bars"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartBarOptions {
    internal ChartBarOptions Clone() => (ChartBarOptions)MemberwiseClone();
}
