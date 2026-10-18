using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    /// <summary>Plans named X captions before the first gutter measurement and after each plot-width change.</summary>
    /// <remarks>Full captions stay in the axis cache. Only ordinary multiword category displays may wrap or shorten;
    /// numeric/calendar scales, single-token captions and explicit presentation retain their existing policy.</remarks>
    private static void RefreshCategoryDisplay(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect track, ChartRange range, VisualThemeColors colors, AxisLabelCache labels) {
        var axis = chart.Options.XAxis;
        labels.SetCategoryDisplay(axis, null);
        if (!chart.Options.ShowAxes || !axis.Visible || track.Width <= 0 || chart.Series.Count == 0
            || axis.Scale != ChartScaleKind.Linear || !labels.HasAuthoredTicks(axis) || VisualAxisText.Angle(axis.LabelAngle) != 0
            || axis.LabelDensity is ChartLabelDensity.All or ChartLabelDensity.Relaxed
            || ChartCartesianOrientation.IsHorizontal(chart)
            || chart.Series.Any(series => !ChartCartesianCategoryLabels.IsCategoricalKind(series.Kind)
                || series.HistogramBinLayout != null || series.TimeBucketInterval != null)) return;
        var style = chart.Options.TickLabelStyle.Resolve(new TextStyle {
            Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = colors.MutedForeground
        });
        // Keep the existing oversized-font owner authoritative; its fitted style is chosen later.
        if (style.EffectiveFontSize > VisualTextFitting.MaximumOrdinaryFontSize) return;
        var ticks = labels.Ticks(axis, range.MinX, range.MaxX);
        var sources = new Dictionary<double, string>();
        foreach (var tick in ticks) sources[tick] = labels.Format(axis, tick, null, ticks);
        if (!sources.Values.Any(CanWrapCategory)) return;
        var positions = ticks.Select(tick => ChartScaleTransform.Normalize(tick, range.MinX, range.MaxX, axis) * track.Width).ToArray();
        var gap = AxisLabelGap(axis, context.Theme.Spacing);
        var displays = new Dictionary<double, (string Text, bool Shortened)>();
        for (var index = 0; index < ticks.Count; index++) {
            var source = sources[ticks[index]];
            if (!CanWrapCategory(source)) continue;
            var pitch = track.Width;
            if (index > 0) pitch = Math.Min(pitch, Math.Abs(positions[index] - positions[index - 1]));
            if (index + 1 < positions.Length) pitch = Math.Min(pitch, Math.Abs(positions[index + 1] - positions[index]));
            var width = Math.Max(0, pitch - gap);
            if (width <= 0) continue;
            var display = VisualAxisText.FitWrappedCategory(builder, source, style, width, out var shortened);
            // Empty or ellipsis-only captions cannot identify a category. Keep their complete
            // source display so the existing regular-stride owner can select useful captions.
            if (display.Replace("…", string.Empty).Trim().Length == 0) continue;
            displays[ticks[index]] = (display, shortened);
        }
        // Do not turn different complete names into the same shortened prefix merely to
        // paint more captions. The existing stride remains the fallback for that group.
        var ambiguous = ticks.GroupBy(tick => displays.TryGetValue(tick, out var display) ? display.Text : sources[tick], StringComparer.Ordinal)
            .Where(group => group.Select(tick => sources[tick]).Distinct(StringComparer.Ordinal).Skip(1).Any())
            .SelectMany(group => group).ToArray();
        foreach (var tick in ambiguous) displays.Remove(tick);
        if (displays.Count > 0) labels.SetCategoryDisplay(axis, displays);
    }

    private static bool CanWrapCategory(string source) => source.IndexOf('\n') < 0 && source.IndexOf('\r') < 0
        && source.Trim().IndexOf(' ') > 0;
}
