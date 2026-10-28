using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    private static double MapCalloutInset(Chart chart, ChartRect plot) => chart.Options.MapCallouts == null ? 0
        : Math.Min(chart.Options.MapCallouts.MaximumDistance, Math.Min(plot.Width, plot.Height) / 5);

    // Each candidate's nearest box edge is no farther than its anchor offset. Diagonals use unit directions,
    // so MaximumDistance bounds the straight leader in logical units, independently of text dimensions.
    private static List<LabelCandidate> MapCalloutCandidates(ChartDataLabelPlacement placement, double clearance, double maximum) {
        var candidates = new List<LabelCandidate>();
        var first = Math.Min(clearance, maximum);
        if (placement is ChartDataLabelPlacement.Center or ChartDataLabelPlacement.Inside)
            candidates.Add(new LabelCandidate(0, 0, .5, .5));
        for (var band = 0; band < 4; band++) {
            var distance = first + (maximum - first) * band / 3;
            if (placement == ChartDataLabelPlacement.Left) candidates.Add(new LabelCandidate(-distance, 0, 1, .5));
            else if (placement is ChartDataLabelPlacement.Right or ChartDataLabelPlacement.Outside) candidates.Add(new LabelCandidate(distance, 0, 0, .5));
            else if (placement == ChartDataLabelPlacement.Below) candidates.Add(new LabelCandidate(0, distance, .5, 0));
            candidates.Add(new LabelCandidate(0, -distance, .5, 1));
            candidates.Add(new LabelCandidate(distance, 0, 0, .5)); candidates.Add(new LabelCandidate(-distance, 0, 1, .5));
            candidates.Add(new LabelCandidate(0, distance, .5, 0));
            var diagonal = distance / Math.Sqrt(2);
            candidates.Add(new LabelCandidate(diagonal, diagonal, 0, 0)); candidates.Add(new LabelCandidate(-diagonal, diagonal, 1, 0));
            candidates.Add(new LabelCandidate(diagonal, -diagonal, 0, 1)); candidates.Add(new LabelCandidate(-diagonal, -diagonal, 1, 1));
            if (first == maximum) break;
        }
        return candidates;
    }
}
