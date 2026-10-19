using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualColorScaleLegendScene {
    /// <summary>Measured anchored placements shared by horizontal keys and inverted right-hand map keys.</summary>
    private sealed class CaptionPlan {
        internal CaptionPlan(IReadOnlyList<ChartColorScaleLegend.CaptionFact> facts, IReadOnlyDictionary<int, PlacedLabel> placements,
            double height) { Facts = facts; Placements = placements; Height = height; }
        internal IReadOnlyList<ChartColorScaleLegend.CaptionFact> Facts { get; }
        internal IReadOnlyDictionary<int, PlacedLabel> Placements { get; }
        internal double Height { get; }
        internal bool Omitted => Facts.Select((fact, index) => fact.Candidate && !Placements.ContainsKey(index)).Any(value => value);
    }

    private static CaptionPlan PlanCaptions(VisualSceneBuilder builder, IReadOnlyList<ChartColorScaleLegend.CaptionFact> facts,
        TextStyle style, double gap, double width, double maximumHeight, bool vertical = false) {
        width = Math.Max(0, width); maximumHeight = Math.Max(0, maximumHeight);
        var requests = new List<LabelPlacementRequest>(); var indices = new List<int>();
        var row = builder.MeasureText("Mg", style).Height;
        for (var index = 0; index < facts.Count; index++) {
            var fact = facts[index];
            if (!fact.Candidate) continue;
            var anchor = vertical ? 0 : width * fact.Ratio;
            var lane = vertical ? (new ChartRect(0, 0, width, 0), TextAlignment.Left)
                : MidpointCaptionLane(builder, fact.Text, style, new ChartRect(0, 0, width, 0), anchor);
            var fitted = FitCaptionStyle(builder, fact.Text, style, lane.Item1.Width);
            var metrics = builder.MeasureText(fact.Text, fitted);
            row = Math.Max(row, metrics.Height);
            var fraction = lane.Item2 == TextAlignment.Left ? 0 : lane.Item2 == TextAlignment.Right ? 1 : .5;
            requests.Add(new LabelPlacementRequest(fact.Text, new ChartPoint(anchor, vertical ? maximumHeight * (1 - fact.Ratio) : 0),
                fitted, new[] { new LabelCandidate(0, 0, fraction, 0) }, priority: fact.Origin == "authored" ? 3 : fact.Origin == "midpoint" ? 1 : 2) {
                Fallback = LabelFallbackRule.Drop, MeasuredSize = metrics
            });
            indices.Add(index);
        }
        var height = vertical ? maximumHeight : Math.Min(maximumHeight, row * Math.Min(2, requests.Count) + gap * Math.Min(1, Math.Max(0, requests.Count - 1)));
        for (var index = 0; index < requests.Count; index++) {
            var request = requests[index]; var metrics = request.MeasuredSize!.Value;
            IReadOnlyList<LabelCandidate> candidates;
            if (vertical) {
                var anchor = request.Anchor.Y;
                var fraction = anchor - metrics.Height / 2 < 0 ? 0 : anchor + metrics.Height / 2 > height ? 1 : .5;
                candidates = new[] { new LabelCandidate(0, 0, 0, fraction) };
            } else candidates = new[] { request.Candidates[0], new LabelCandidate(0, row + gap, request.Candidates[0].HorizontalAlignment, 0) };
            requests[index] = new LabelPlacementRequest(request.Text, request.Anchor, request.Style, candidates, request.Priority) {
                Fallback = LabelFallbackRule.Drop, MeasuredSize = metrics
            };
        }
        var placed = new LabelPlacementService().Place(requests, new ChartRect(0, 0, width, height), null, gap, builder.MeasureText);
        var result = new Dictionary<int, PlacedLabel>();
        for (var index = 0; index < placed.Count; index++) if (!placed[index].IsDropped) result.Add(indices[index], placed[index]);
        return new CaptionPlan(facts, result, result.Count == 0 ? 0 : result.Values.Max(label => label.Bounds.Bottom));
    }

    private static double CaptionedHeight(VisualSceneBuilder builder, ChartColorScale scale, ChartLabels labels, TextStyle style,
        double gap, double width, double maximum, bool missing, Func<IReadOnlyList<double>, string[]> formatValues, bool hasDomain,
        double min, double max, bool inlineMissing, string? heading, ChartColorLegendStyle legendStyle) {
        var row = builder.MeasureText("Mg", style).Height;
        var facts = ChartColorScaleLegend.CaptionFacts(scale, labels, min, max, hasDomain, formatValues, legendStyle);
        var captions = hasDomain ? PlanCaptions(builder, facts, style, gap, width, double.MaxValue).Height : 0;
        var missingHeight = (missing || !hasDomain) && !inlineMissing ? row + gap : 0;
        var height = HeadingHeight(builder, style, gap, heading) + gap + (hasDomain ? 12 + gap / 2 + captions : 0) + missingHeight;
        return Math.Max(0, Math.Min(maximum, height));
    }

    /// <summary>Reserves the same measured full-caption column that the vertical caption plan can use.</summary>
    internal static double CaptionedVerticalWidth(VisualSceneBuilder builder, ChartColorScale scale, ChartLabels labels,
        TextStyle style, double gap, double maximum, double min, double max, bool hasDomain,
        Func<IReadOnlyList<double>, string[]> formatValues, string heading, bool missing, ChartColorLegendStyle legendStyle) {
        var row = builder.MeasureText("Mg", style).Height;
        var facts = ChartColorScaleLegend.CaptionFacts(scale, labels, min, max, hasDomain, formatValues, legendStyle);
        var text = facts.Where(fact => fact.Candidate).Select(fact => builder.MeasureText(fact.Text, style).Width).DefaultIfEmpty(0).Max();
        var title = builder.MeasureText(heading, style).Width;
        if (missing || !hasDomain) title = Math.Max(title, builder.MeasureText(labels.NoData, style).Width + row);
        return Math.Min(Math.Max(0, maximum), Math.Max(title, text + row + gap) + gap);
    }
}
