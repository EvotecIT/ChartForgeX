using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static readonly string[] BoxPlotStatisticNames = { "minimum", "q1", "median", "q3", "maximum" };

    private static (ResolvedPointLabel Observation, string?[] Captions, string[] Numeric) ResolveBoxPlotLabels(Chart chart,
        VisualRenderContext context, ChartSeries series, int item, VisualThemeColors colors, string category,
        ChartBoxPlot summary, ChartBoxPlotOptions options) {
        var numeric = new string[5]; var captions = new string?[5];
        var values = new[] { summary.Minimum, summary.Q1, summary.Median, summary.Q3, summary.Maximum };
        for (var member = 0; member < values.Length; member++) {
            numeric[member] = Value(chart, values[member]);
            var text = options.StatisticLabelFormatter == null ? numeric[member]
                : options.StatisticLabelFormatter(new ChartBoxPlotStatisticLabelContext(series.Name, item, category,
                    summary, (ChartBoxPlotStatistic)member, numeric[member]));
            captions[member] = string.IsNullOrWhiteSpace(text) ? null : text;
        }
        var observation = ResolveObservationLabel(chart, context, series, item, colors, _ => numeric[2]);
        return (observation, captions, numeric);
    }

    private static void AddBoxPlotStatisticLabels(Chart chart, VisualRenderContext context, ChartSeries series, int index,
        int item, ChartRect bounds, bool horizontal, Func<double, ChartPoint> project, ChartBoxPlot summary,
        ResolvedPointLabel observation, string?[] captions, ChartBoxPlotOptions options, List<LabelPlacementRequest> labels) {
        var components = new[] { options.MinimumLabel, options.Q1Label, options.MedianLabel, options.Q3Label, options.MaximumLabel };
        var values = new[] { summary.Minimum, summary.Q1, summary.Median, summary.Q3, summary.Maximum };
        var positions = new double[5];
        for (var member = 0; member < positions.Length; member++) {
            var point = project(values[member]); positions[member] = horizontal ? point.X : point.Y;
        }
        for (var member = 0; member < captions.Length; member++) {
            var text = captions[member]; if (text == null) continue;
            var point = project(values[member]);
            // Adjacent statistic midpoints bound each Inside/Center lane, including reversed value axes.
            // The complete box/whisker remains the single collision obstacle and source association.
            var before = member == 0 ? positions[0] : positions[member - 1] / 2 + positions[member] / 2;
            var after = member == 4 ? positions[4] : positions[member] / 2 + positions[member + 1] / 2;
            var low = Math.Min(before, after); var length = Math.Abs(after - before);
            var slot = horizontal ? new ChartRect(low, bounds.Top, length, bounds.Height)
                : new ChartRect(bounds.Left, low, bounds.Width, length);
            var label = new ResolvedPointLabel(text, TextCaseTransformer.Apply(text, observation.Style.TextCase, CultureInfo.InvariantCulture), observation.Style,
                fallback: options.StatisticLabelFormatter == null ? LabelFallbackRule.Drop : LabelFallbackRule.EllipsisThenDrop);
            AddLabel(chart, context, series, index, item, point, slot, label, labels, values[member], horizontal: horizontal,
                labelId: PointId(index, item) + "-" + BoxPlotStatisticNames[member], valueLabel: components[member]);
        }
    }
}
