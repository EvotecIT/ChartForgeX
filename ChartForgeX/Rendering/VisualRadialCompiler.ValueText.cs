using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static partial class VisualRadialCompiler {
    private static string FormatPercentage(double fraction) => fraction.ToString("0.#%", CultureInfo.InvariantCulture);
    private static string FormatOverflowCenter(bool tight) => tight ? ">1E308" : "> 1.79E308";
    /// <summary>Uses the same percentage/overflow caption owners as native legend, slices and center fitting.</summary>
    private static IReadOnlyList<(string Owner, string Text)> RadialValueTextRepertoire() {
        var observations = new List<(string Owner, string Text)>();
        for (var digit = 0; digit <= 10; digit++) observations.Add(("radial-percent", FormatPercentage(digit / 10d)));
        observations.Add(("radial-percent", FormatPercentage(.12345)));
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
            yield return ("native-radial-label-value-context", FormatLabelContent(chart.Options.PieSliceLabelContent, OtherSliceLabel, FormatPercentage(0), value));
    }
}
