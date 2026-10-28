using System;
using System.Collections.Generic;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal enum SvgRasterFilterOperation { Flood, Color, Blur, Morphology, Offset, Blend, Composite, Arithmetic, Merge }

/// <summary>Parsed source-order primitive data; indices can refer only to a built-in or an earlier primitive.</summary>
internal sealed class SvgRasterFilterStage {
    internal SvgRasterFilterOperation Operation;
    internal int[] Inputs = Array.Empty<int>();
    internal bool LinearRgb;
    internal RasterEffectRegion? Clip;
    internal SvgRasterFilterSubregion Subregion;
    internal RasterColorTransform? Color;
    internal RasterEffectPixel16 Flood;
    internal int X, Y;
    internal bool Maximize;
    internal VisualBlendMode Blend;
    internal VisualCompositeOperator Composite;
    internal double K1, K2, K3, K4;
}

internal sealed partial class SvgRasterFilterPlan {
    internal const int SourceGraphicInput = -1, SourceAlphaInput = -2;
    internal SvgRasterFilterStage[]? Stages { get; }
    internal bool[]? NeededStages { get; }
    internal int[]? StageReadCounts { get; }
    internal bool RequiresStageSource { get; }

    private SvgRasterFilterPlan(RasterEffectRegion frame, SvgRasterFilterStage[] stages, CancellationToken cancellation) {
        FilterRegion = CaptureRegion = frame; Stages = stages;
        var needed = NeededStages = new bool[stages.Length]; var reads = StageReadCounts = new int[stages.Length + 2];
        needed[stages.Length - 1] = true; reads[stages.Length + 1] = 1; // Final publication holds the last result once.
        for (var index = stages.Length - 1; index >= 0; index--) {
            cancellation.ThrowIfCancellationRequested();
            if (!needed[index]) continue;
            foreach (var input in stages[index].Inputs) {
                cancellation.ThrowIfCancellationRequested();
                reads[input + 2] = checked(reads[input + 2] + 1);
                if (input >= 0) needed[input] = true;
            }
        }
        if (reads[0] > 0) reads[1] = checked(reads[1] + 1); // One lazy SourceAlpha derivation reads SourceGraphic once.
        RequiresStageSource = reads[1] > 0;
    }

    private static bool TryOrderedStages(SvgRasterElement filter, SvgRasterMatrix matrix, SvgRasterDefinitions definitions,
        out SvgRasterFilterPlan? plan, out string reason) {
        plan = null;
        reason = "Finite ordered primitives require an explicit aligned user-space filter frame and represented inputs, operations and subregions.";
        if (!TryStageFrame(filter, matrix, definitions, out var frame)) return false;
        try {
            var stages = new List<SvgRasterFilterStage>(); var names = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var primitive in filter.Children) {
                definitions.Filters!.Cancellation.ThrowIfCancellationRequested();
                if (primitive.Name == "title" || primitive.Name == "desc" || primitive.Name == "style") continue;
                if (!TryStagePrimitive(primitive, matrix, definitions, stages.Count, names, out var stage, out reason)) return false;
                if (!TryStageClip(primitive, matrix, frame, stages, stage.Inputs, out stage.Subregion, out stage.Clip)) {
                    reason = "Primitive subregion lengths must be finite numeric user-space values aligned to device pixels."; return false;
                }
                if (stage.Operation == SvgRasterFilterOperation.Blur)
                    _ = RasterEffectPlan.SurfaceBlur(new RasterEffectRegion(0, 0, frame.Width, frame.Height), stage.X, stage.Y, stage.LinearRgb);
                var result = primitive.Get("result");
                if (result != null) {
                    if (!ResultName(result)) { reason = "A result name must be one non-reserved token."; return false; }
                    names[result] = stages.Count; // A reference binds to the closest preceding producer.
                }
                stages.Add(stage);
            }
            if (stages.Count == 0) return false;
            plan = new SvgRasterFilterPlan(frame, stages.ToArray(), definitions.Filters!.Cancellation); reason = string.Empty; return true;
        } catch (Exception ex) when (ex is ArgumentException || ex is OverflowException) {
            reason = "The ordered filter's frame, support or scalar parameters exceed finite native admission."; return false;
        }
    }

    private static bool TryStageInput(string? value, int previous, Dictionary<string, int> names, out int input) {
        input = previous;
        if (string.IsNullOrWhiteSpace(value)) return true;
        var name = value!.Trim();
        switch (name) {
            case "SourceGraphic": input = SourceGraphicInput; return true;
            case "SourceAlpha": input = SourceAlphaInput; return true;
            case "BackgroundImage": case "BackgroundAlpha": case "FillPaint": case "StrokePaint": return false;
            default:
                // Missing and forward names have the same input as an omitted attribute, per Filter Effects.
                if (names.TryGetValue(name, out var earlier)) input = earlier;
                return true;
        }
    }
}
