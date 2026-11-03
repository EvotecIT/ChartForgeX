using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualSpecialtyCompiler {
    private sealed class PyramidCaption {
        internal PyramidCaption(ChartRect bounds, string text, bool inside = false) { Bounds = bounds; Text = text; Inside = inside; }
        internal ChartRect Bounds { get; }
        internal bool Inside { get; }
        internal string Text { get; }
    }

    /// <summary>Shares the rail among complete readable captions while keeping each leader attached to its own partition.</summary>
    private static IReadOnlyDictionary<int, PyramidCaption> PyramidCaptions(Chart chart, VisualRenderContext context,
        VisualSceneBuilder builder, ChartPyramidLayout layout, ChartDataLabelPlacement placement, VisualThemeColors colors) {
        var result = new Dictionary<int, PyramidCaption>();
        if (layout.Stages.Count == 0) return result;
        var vertical = layout.LabelSide is ChartDataLabelPlacement.Left or ChartDataLabelPlacement.Right;
        var left = layout.Stages.Min(stage => stage.OutsideLabel.Left);
        var top = layout.Stages.Min(stage => stage.OutsideLabel.Top);
        var right = layout.Stages.Max(stage => stage.OutsideLabel.Right);
        var bottom = layout.Stages.Max(stage => stage.OutsideLabel.Bottom);
        var rail = new ChartRect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
        var occupiedEnd = double.NegativeInfinity;
        var gap = context.Theme.Spacing / 4;
        bool FitsInside(ChartPyramidStageLayout stage) {
            var metrics = builder.MeasureText(PyramidLabel(chart, stage.SourceIndex), Style(chart, context, stage.SourceIndex, colors.Foreground));
            return metrics.Width <= stage.InsideLabel.Width && metrics.Height <= stage.InsideLabel.Height;
        }
        // Automatic placement keeps one caption treatment: when any stage needs the rail, every stage uses it,
        // so the chart never mixes a leader with inside captions. Explicit inside placement keeps its per-stage fit.
        var insideAllowed = placement is ChartDataLabelPlacement.Inside or ChartDataLabelPlacement.Center
            || placement == ChartDataLabelPlacement.Auto && layout.Stages.Where(stage => stage.HasGeometry).All(FitsInside);
        foreach (var stage in layout.Stages.OrderBy(stage => vertical ? stage.OutsideLabel.Top : stage.OutsideLabel.Left)) {
            var index = stage.SourceIndex;
            var source = PyramidLabel(chart, index);
            var style = Style(chart, context, index, colors.Foreground);
            var metrics = builder.MeasureText(source, style);
            if (insideAllowed && metrics.Width <= stage.InsideLabel.Width && metrics.Height <= stage.InsideLabel.Height) {
                result[index] = new PyramidCaption(stage.InsideLabel, source, inside: true);
                continue;
            }
            // A stage's geometric slot may be shorter than one text line. Keep its
            // complete name and value on a measured rail, then omit crowded neighbours.
            // No per-stage shrink or prefix-only caption is needed to make them fit.
            var displayed = vertical ? string.Join("\n", ChartLabelWrapping.BalancedTwoLine(source,
                style.EffectiveFontSize, rail.Width, (value, _) => builder.MeasureText(value, style).Width)) : source;
            metrics = builder.MeasureText(displayed, style);
            if (metrics.Width > rail.Width || metrics.Height > rail.Height) continue;
            var center = vertical ? stage.OutsideLabel.Top + stage.OutsideLabel.Height / 2
                : stage.OutsideLabel.Left + stage.OutsideLabel.Width / 2;
            var extent = vertical ? metrics.Height : metrics.Width;
            var minimum = vertical ? rail.Top : rail.Left;
            var maximum = vertical ? rail.Bottom : rail.Right;
            var start = Math.Max(minimum, Math.Min(maximum - extent, center - extent / 2));
            if (start < occupiedEnd + gap) continue;
            var bounds = vertical ? new ChartRect(rail.Left, start, rail.Width, extent)
                : new ChartRect(start, rail.Top, extent, rail.Height);
            result[index] = new PyramidCaption(bounds, displayed);
            occupiedEnd = start + extent;
        }
        return result;
    }
}
