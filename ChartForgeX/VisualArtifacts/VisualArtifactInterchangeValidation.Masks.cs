using System;
using System.Collections.Generic;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    internal static long Base64Length(long byteLength) => checked((byteLength + 2) / 3 * 4);

}
