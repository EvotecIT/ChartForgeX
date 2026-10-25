using System;
using System.Collections.Generic;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    /// <summary>Fill membership refers to surviving authored observations, never derived alignment vertices.</summary>
    private static IDisposable? AlignedAreaSegment(VisualSceneBuilder builder, IReadOnlyList<ChartStackAreaVertex> vertices, int start, int count) {
        var first = -1; var last = -1;
        for (var index = start; index < start + count; index++) {
            if (!vertices[index].SourcePoint.HasValue) continue;
            if (first < 0) first = vertices[index].SourcePoint!.Value;
            last = vertices[index].SourcePoint!.Value;
        }
        return first < 0 ? null : FillSegment(builder, first, last - first + 1);
    }
}
