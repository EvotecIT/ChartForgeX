using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static (ResolvedPointLabel Observation, string? Start, string? End) ResolveRangeBarLabels(Chart chart,
        VisualRenderContext context, ChartSeries series, int item, VisualThemeColors colors, string category,
        ChartInterval interval, bool endpoints, Func<ChartRangeBarEndpointLabelContext, string?>? formatter) {
        // Preserve the existing lazy formatter and custom-point-label precedence for combined captions.
        if (!endpoints) return (ResolveObservationLabel(chart, context, series, item, colors,
            () => Value(chart, Math.Min(interval.Start, interval.End)) + "–" + Value(chart, Math.Max(interval.Start, interval.End))), null, null);

        // Format the pair once in the existing numeric order, then reuse it for heading and endpoint contexts.
        var low = Value(chart, Math.Min(interval.Start, interval.End));
        var high = Value(chart, Math.Max(interval.Start, interval.End));
        var startValue = interval.Start <= interval.End ? low : high;
        var endValue = interval.Start <= interval.End ? high : low;
        var observation = ResolveObservationLabel(chart, context, series, item, colors, () => low + "–" + high);
        var start = Format(ChartRangeBarEndpoint.Start, startValue);
        var end = Format(ChartRangeBarEndpoint.End, endValue);
        return (observation, start, end);

        string? Format(ChartRangeBarEndpoint endpoint, string value) {
            var text = formatter == null ? value : formatter(new ChartRangeBarEndpointLabelContext(series.Name, item, category, interval, endpoint, value));
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
    }

    private static void AddRangeBarEndpointLabels(Chart chart, VisualRenderContext context, ChartSeries series,
        int index, int item, ChartRect body, double category, double start, double end, bool horizontal,
        ResolvedPointLabel observation, string? startText, string? endText, List<LabelPlacementRequest> labels) {
        Add(startText, start, start <= end, "start");
        Add(endText, end, end < start, "end");

        void Add(string? text, double endpoint, bool leading, string suffix) {
            if (text == null) return;
            // Each caption owns its half of the interval for Inside/Center and physical side placements.
            // Both still reference the complete observation, whose actual paint remains one shared obstacle.
            var half = horizontal ? new ChartRect(body.Left + (leading ? 0 : body.Width / 2), body.Top, body.Width / 2, body.Height)
                : new ChartRect(body.Left, body.Top + (leading ? 0 : body.Height / 2), body.Width, body.Height / 2);
            var anchor = horizontal ? new ChartPoint(endpoint, category) : new ChartPoint(category, endpoint);
            var direction = horizontal ? leading ? -1 : 1 : leading ? 1 : -1;
            var label = new ResolvedPointLabel(text, TextCaseTransformer.Apply(text, observation.Style.TextCase, CultureInfo.InvariantCulture), observation.Style);
            AddLabel(chart, context, series, index, item, anchor, half, label, labels,
                barDirection: direction, horizontal: horizontal, labelId: PointId(index, item) + "-" + suffix);
        }
    }
}
