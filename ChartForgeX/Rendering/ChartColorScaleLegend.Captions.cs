using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static partial class ChartColorScaleLegend {
    /// <summary>One detached caption fact; descriptive legacy labels survive an explicit caption at the same value.</summary>
    internal sealed class CaptionFact {
        internal CaptionFact(double value, string label, string formatted, string origin, bool inDomain, double ratio) {
            Value = value; Label = label; Formatted = formatted; Origin = origin; InDomain = inDomain; Ratio = ratio;
            Text = formatted.Length == 0 ? label : label + " · " + formatted;
        }
        internal double Value { get; }
        internal string Label { get; }
        internal string Formatted { get; }
        internal string Text { get; }
        internal string Origin { get; }
        internal bool InDomain { get; }
        internal double Ratio { get; }
        internal bool Candidate { get; set; }
    }

    /// <summary>Captures full text and numeric positions without clamping outside captions or inventing a missing domain.</summary>
    internal static IReadOnlyList<CaptionFact> CaptionFacts(ChartColorScale scale, ChartLabels labels, double min, double max,
        bool hasDomain, Func<IReadOnlyList<double>, string[]> formatValues, ChartColorLegendStyle legendStyle) {
        var source = new List<(double Value, string Label, string Origin)>();
        var low = scale.EffectiveMinimum(min); var high = scale.EffectiveMaximum(max);
        if (hasDomain) {
            source.Add((low, scale.LowLabel ?? labels.Less, "low"));
            if (scale.MidpointLabel != null) source.Add((scale.EffectiveMidpoint(min, max), scale.MidpointLabel, "midpoint"));
            source.Add((high, scale.HighLabel ?? labels.More, "high"));
        }
        source.AddRange(scale.Captions.Select(caption => (caption.Value, caption.Text, "authored")));
        var formatted = formatValues(source.Select(item => item.Value).ToArray());
        var result = source.Select((item, index) => {
            var inside = hasDomain && item.Value >= low && item.Value <= high;
            return new CaptionFact(item.Value, item.Label, formatted[index], item.Origin, inside,
                inside ? CaptionRatio(scale, min, max, item.Value, legendStyle) : 0);
        }).ToArray();
        foreach (var group in result.Where(item => item.InDomain).GroupBy(item => item.Value))
            (group.FirstOrDefault(item => item.Origin == "authored") ?? group.First()).Candidate = true;
        return Array.AsReadOnly(result);
    }

    /// <summary>Projects numeric values through the actual sampled source values, rather than color-stop ordinals.</summary>
    private static double CaptionRatio(ChartColorScale scale, double min, double max, double value, ChartColorLegendStyle legendStyle) {
        var low = scale.EffectiveMinimum(min); var high = scale.EffectiveMaximum(max);
        if (low == high) return .5;
        if (legendStyle == ChartColorLegendStyle.Gradient) return ChartMath.Normalize(value, low, high);
        var steps = Steps(scale, min, max);
        if (value <= steps[0]) return .5 / steps.Length;
        for (var index = 1; index < steps.Length; index++) if (value <= steps[index]) {
            var fraction = steps[index] == steps[index - 1] ? 0 : ChartMath.Normalize(value, steps[index - 1], steps[index]);
            return (index - .5 + fraction) / steps.Length;
        }
        return (steps.Length - .5) / steps.Length;
    }
}
