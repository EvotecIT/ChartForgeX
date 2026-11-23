using ChartForgeX.Core;
using PowerForge.Fluent;
using System.Collections.Generic;
using System;

namespace ChartForgeX.VisualBlocks;

/// <summary>Hand-written fluent methods of <see cref="WardleyMapBlock"/> that do more than set one property.</summary>
public static partial class WardleyMapBlockFluent {
    /// <summary>Sets detached, strictly increasing stage right edges in (0,1], or clears them with null.</summary>
    public static global::ChartForgeX.VisualBlocks.WardleyMapBlock StageBoundaries(this global::ChartForgeX.VisualBlocks.WardleyMapBlock wardleyMapBlock, global::System.Collections.Generic.IEnumerable<double>? boundaries)
        => (wardleyMapBlock ?? throw new System.ArgumentNullException(nameof(wardleyMapBlock))).SetStageBoundaries(boundaries);

    /// <summary>Places the annotation legend at normalized visibility/evolution coordinates; its measured box stays inside the plot.</summary>
    public static global::ChartForgeX.VisualBlocks.WardleyMapBlock AnnotationsBox(this global::ChartForgeX.VisualBlocks.WardleyMapBlock wardleyMapBlock, double visibility, double evolution)
        => (wardleyMapBlock ?? throw new System.ArgumentNullException(nameof(wardleyMapBlock))).SetAnnotationsBox(visibility, evolution);
}
