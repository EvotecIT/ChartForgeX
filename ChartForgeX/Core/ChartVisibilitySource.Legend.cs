using System;
using System.Collections.Generic;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

internal sealed partial class ChartVisibilitySource {
    internal IReadOnlyList<VisualLegendEntry> LegendEntries(VisualRenderContext context) {
        var complete = CreateNativeView(null);
        ChartTimeSpacingPreparation.Capture(complete);
        var labels = new ChartAxisValueFormatter.Cache();
        return VisualChartCompiler.LegendEntries(VisualChartCompiler.Family(complete), complete,
            context.Theme.Resolve(context.ThemeMode), labels, null, new ChartColorPresentationSet(complete));
    }
}
