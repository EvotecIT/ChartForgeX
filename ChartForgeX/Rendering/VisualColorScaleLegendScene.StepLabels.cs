using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualColorScaleLegendScene {
    /// <summary>Fits intermediate numeric captions between complete endpoints and any named midpoint.</summary>
    internal static void StepLabels(VisualSceneBuilder builder, IReadOnlyList<double> positions, IReadOnlyList<string> captions,
        TextStyle style, ChartRect area, double leftReserved, double rightReserved, double gap, string prefix, ChartRect? obstacle = null) {
        var left = area.Left + leftReserved + gap;
        var right = area.Right - rightReserved - gap;
        for (var index = 1; index < positions.Count - 1; index++) {
            var text = captions[index]; var size = builder.MeasureText(text, style); var center = positions[index];
            var first = center - size.Width / 2; var last = center + size.Width / 2;
            if (first < left || last > right || size.Height > area.Height) continue;
            if (obstacle.HasValue && first < obstacle.Value.Right + gap && last > obstacle.Value.Left - gap) continue;
            VisualStateSceneTools.Text(builder, text, new ChartRect(first, area.Top, size.Width, area.Height), style,
                prefix + "-label", prefix + "-label-step-" + index, TextAlignment.Center, paint: VisualChartPaint.Text(style));
            left = last + gap;
        }
    }
}
