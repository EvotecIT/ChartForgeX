using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Pictorial symbols, values and artwork. Configure it through <see cref="ChartOptions.Pictorial"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartPictorialOptions {
    internal ChartPictorialOptions Clone() => (ChartPictorialOptions)MemberwiseClone();
}
