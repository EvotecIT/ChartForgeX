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
        ChartRect track, ChartRect frame, ChartRange range, VisualThemeColors colors, AxisLabelCache labels) {
        var axis = chart.Options.XAxis;
        labels.SetCategoryDisplay(axis, null);
        if (!chart.Options.ShowAxes || !axis.Visible || track.Width <= 0 || chart.Series.Count == 0
            || axis.Scale != ChartScaleKind.Linear || !labels.HasAuthoredTicks(axis) || VisualAxisText.Angle(axis.LabelAngle) != 0
            || axis.LabelDensity is ChartLabelDensity.All or ChartLabelDensity.Relaxed
            || ChartCartesianOrientation.IsHorizontal(chart)
            || chart.Series.Any(series => series.HistogramBinLayout != null || series.TimeBucketInterval != null)
            || chart.Series.Any(series => !ChartCartesianCategoryLabels.IsCategoricalKind(series.Kind))
                && (axis.Labels.Count == 0 || chart.Series.SelectMany(ChartCartesianOrientation.Categories)
                    .Any(value => ChartAxisValueFormatter.FindExplicitLabel(axis.Labels, value) == null))) return;
        var style = chart.Options.TickLabelStyle.Resolve(new TextStyle {
            Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = colors.MutedForeground
        });
        // Keep the existing oversized-font owner authoritative; its fitted style is chosen later.
        if (style.EffectiveFontSize > VisualTextFitting.MaximumOrdinaryFontSize) return;
        var ticks = labels.Ticks(axis, range.MinX, range.MaxX);
        var sources = new Dictionary<double, string>();
        foreach (var tick in ticks) sources[tick] = labels.Format(axis, tick, null, ticks);
        if (!sources.Values.Any(CanWrapCategory)) return;
        var named = sources.Values.Where(source => source.Length > 0).ToArray();
        var exceedsFrame = named.Length > 0 && named.All(source => builder.MeasureText(source, style).Width > frame.Width);
        // Point and area catalogs retain their complete-caption stride unless none of
        // their full names can fit the frame. Ordinary sparse labels must not acquire
        // a second line that competes with an explicitly styled axis title.
        if (chart.Series.Any(series => !ChartCartesianCategoryLabels.IsCategoricalKind(series.Kind)) && !exceedsFrame) return;
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
        // A complete explicit category catalog can name point/area observations too. If every
        // nonempty caption is wider than the entire frame, reserving its full width cannot
        // produce any ink. Plan a coarse regular rhythm before the monotonic frame is reserved.
        if (displays.Count == 0 && exceedsFrame) {
            for (var stride = 1; stride <= ticks.Count; stride++) {
                var selected = ticks.Where((_, index) => index % stride == 0).ToArray();
                var candidates = new Dictionary<double, (string Text, bool Shortened)>();
                for (var index = 0; index < selected.Length; index++) {
                    var pitch = track.Width;
                    if (index > 0) pitch = Math.Min(pitch, Math.Abs(Position(selected[index]) - Position(selected[index - 1])));
                    if (index + 1 < selected.Length) pitch = Math.Min(pitch, Math.Abs(Position(selected[index + 1]) - Position(selected[index])));
                    var text = VisualAxisText.FitWrappedCategory(builder, sources[selected[index]], style, Math.Max(0, pitch - gap), out var shortened);
                    candidates[selected[index]] = (text, shortened);
                }
                if (candidates.Values.Any(item => item.Text.Replace("…", string.Empty).Trim().Length == 0)
                    || candidates.Values.Select(item => item.Text).Distinct(StringComparer.Ordinal).Count() != candidates.Count
                    || !AxisLabelSequenceFits(builder, axis, selected, Position, true, style, gap, null, labels, tick => candidates[tick].Text)) continue;
                // The first source tick already needs its endpoint gutter. Do not select a
                // second caption that requires the opposite gutter and then loses its place
                // when that reservation shortens the track. A complete coarse caption is
                // preferable to several shortened common prefixes; tiny frames retain one.
                if (selected.Length > 1 && (candidates.Values.Any(item => item.Shortened)
                    || selected.Any(tick => {
                        var half = builder.MeasureText(candidates[tick].Text, style).Width / 2;
                        return half > (axis.Reversed ? Position(tick) : track.Width - Position(tick));
                    }))) continue;
                displays = candidates;
                labels.PlanCategoryTicks(axis, selected, track.Width);
                break;
            }
        }
        if (displays.Count > 0) labels.SetCategoryDisplay(axis, displays);
        double Position(double tick) => ChartScaleTransform.Normalize(tick, range.MinX, range.MaxX, axis) * track.Width;
    }

    /// <summary>Reserves the actual planned edge ink rather than applying the widest caption to both endpoints.</summary>
    private static (double Left, double Right)? CategoryCaptionOverhang(VisualSceneBuilder builder, ChartAxis axis,
        double minimum, double maximum, TextStyle style, AxisLabelCache labels) {
        var ticks = labels.PlannedCategoryTicks(axis);
        if (ticks == null) return null;
        var length = labels.PlannedCategoryTrack(axis); var left = 0d; var right = 0d;
        foreach (var tick in ticks) {
            var text = labels.CategoryDisplay(axis, tick, labels.Format(axis, tick, null, ticks));
            var half = builder.MeasureText(text, style).Width / 2;
            var position = ChartScaleTransform.Normalize(tick, minimum, maximum, axis) * length;
            left = Math.Max(left, half - position); right = Math.Max(right, half - (length - position));
        }
        return (Math.Max(0, left), Math.Max(0, right));
    }

    private static bool CanWrapCategory(string source) => source.IndexOf('\n') < 0 && source.IndexOf('\r') < 0
        && source.Trim().IndexOf(' ') > 0;
}
