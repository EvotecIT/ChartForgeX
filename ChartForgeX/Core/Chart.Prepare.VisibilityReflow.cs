using System;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    private PreparedVisual PrepareReflowVisibility(VisualRenderContext context, ChartVisibilityView visibility, ChartCartesianViewport? viewport) {
        if (viewport != null) throw new NotSupportedException("Native Reflow does not combine an explicit Cartesian window with changed membership.");
        var source = new ChartVisibilitySource(this, context); source.ValidateView(visibility);
        var complete = source.CreateNativeView(null).PrepareCore(context, null);
        var filtered = source.CreateNativeView(visibility); filtered.UseVisibilitySourceLegend(source);
        return VisualSeriesVisibilityCapture.PrepareReflow(complete, filtered.PrepareCore(context, null), visibility, source, context);
    }
    private (PreparedVisual First, PreparedVisual Second, PreparedVisual? FullFirst, PreparedVisual? FullSecond) PrepareAlignedReflowVisibility(
        VisualRenderContext first, VisualRenderContext second, ChartVisibilityView visibility, ChartCartesianViewport? viewport, bool retainFullDrawings) {
        if (viewport != null) throw new NotSupportedException("Native Reflow does not combine explicit Cartesian windows with changed membership.");
        var source = new ChartVisibilitySource(this, first); source.ValidateView(visibility);
        var complete = source.CreateNativeView(null).PrepareAligned(first, second);
        var filtered = source.CreateNativeView(visibility); filtered.UseVisibilitySourceLegend(source);
        var rows = filtered.PrepareAligned(first, second, retainFullDrawings: retainFullDrawings);
        var a = VisualSeriesVisibilityCapture.PrepareReflow(complete.First, rows.First, visibility, source, first);
        var b = VisualSeriesVisibilityCapture.PrepareReflow(complete.Second, rows.Second, visibility, source, second);
        return (a, b, retainFullDrawings ? a : null, retainFullDrawings ? b : null);
    }
}
