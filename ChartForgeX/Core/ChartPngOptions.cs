using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>PNG fonts, hinting and scale. Configure it through <see cref="ChartOptions.Png"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartPngOptions {
    internal ChartPngOptions Clone() => (ChartPngOptions)MemberwiseClone();
}
