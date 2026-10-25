using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualScalarProgressCompiler {
    private static void Progress(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, VisualThemeColors colors) {
        var series = chart.Series[0];
        if (series.RenderPoints.Count == 0) { builder.AddDiagnostic(new VisualDiagnostic("progress.no-data", "The progress chart has no rows.")); return; }
        if (!series.IsTypedProgress && series.RenderPoints.Any(point => point.Y < 0)) throw new InvalidOperationException("Progress rows require non-negative values.");
        builder.CaptureValue(new ProgressPlan(chart, context, plot, colors));
    }
}
