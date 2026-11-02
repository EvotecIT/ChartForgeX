using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static partial class VisualRadialCompiler {
    private static string FormatPercentage(Chart chart, double fraction) => PercentageFormat(chart).Format(fraction);
    private static ChartValueFormat PercentageFormat(Chart chart) => ChartValueFormat.Percentages(chart.Options.ValueFormat,
        Enumerable.Range(0, chart.Series.Count).SelectMany(index => {
            var slices = GetSlices(chart, chart.Options.Theme.Resolve(chart.Options.ThemeMode), index);
            var total = Total(chart, slices, index);
            return slices.Select(slice => Fraction(chart, slice, total));
        }));
    private static string FormatOverflowCenter(bool tight) => tight ? ">1E308" : "> 1.79E308";
    /// <summary>Uses the same percentage/overflow caption owners as native legend, slices and center fitting.</summary>
    private static IReadOnlyList<(string Owner, string Text)> RadialValueTextRepertoire() {
        var observations = new List<(string Owner, string Text)>();
        foreach (var sample in new[] { 0d, .12345 }) {
            var format = ChartValueFormat.Percentages(ChartValueFormat.ExistingValue, new[] { sample });
            for (var digit = 0; digit <= 10; digit++) observations.Add(("radial-percent", format.Format(digit / 10d)));
            observations.Add(("radial-percent", format.Format(.12345)));
        }
        observations.Add(("radial-overflow", OverflowValueText));
        observations.Add(("radial-center-overflow", FormatOverflowCenter(false)));
        observations.Add(("radial-center-overflow", FormatOverflowCenter(true)));
        return observations.AsReadOnly();
    }
    private static IEnumerable<(string Owner, string Text)> RadialValueTextRepertoire(Chart chart, VisualRenderContext context, VisualSceneTextCapture capture) {
        foreach (var row in RadialValueTextRepertoire()) yield return row;
        if (!capture.HasContextCaseRequests || chart.Options.PieSliceLabelContent != ChartPieSliceLabelContent.LabelAndValue
            || !chart.Options.ValueFormat.TryGetNativeTextRepertoire(out _, out var values)) yield break;
        // The admitted LabelAndValue branch ignores percentage; the captured native label and actual formatter result compose the text.
        for (var series = 0; series < chart.Series.Count; series++)
        foreach (var slice in context.CaptureNativeValueSource ? GetSourceSlices(chart, context.Theme.Resolve(context.ThemeMode), series) : GetSlices(chart, context.Theme.Resolve(context.ThemeMode), series)) foreach (var value in values)
            yield return ("native-radial-label-value-context", FormatLabel(chart, slice, 0, value));
        if (context.CaptureNativeValueSource) foreach (var value in values)
            yield return ("native-radial-label-value-context", FormatLabelContent(chart.Options.PieSliceLabelContent, OtherSliceLabel, FormatPercentage(chart, 0), value));
    }
}
