using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualColorScaleLegendScene {
    /// <summary>Draws a bounded continuous key and its complete passive caption catalog from one measured plan.</summary>
    internal static void DrawCaptioned(VisualSceneBuilder builder, ChartColorScale scale, ChartLabels labels, TextStyle style,
        double gap, ChartRect bounds, double min, double max, string prefix, bool missing, ChartColorBlend missingBlend,
        Func<IReadOnlyList<double>, string[]> formatValues, string diagnosticCode, bool hasDomain, ChartRect? missingBounds,
        string? heading, string? valueName, ChartColorLegendStyle legendStyle, bool vertical = false) {
        var row = builder.MeasureText("Mg", style).Height;
        var headingHeight = Math.Min(bounds.Height, HeadingHeight(builder, style, gap, heading));
        var body = new ChartRect(bounds.Left, bounds.Top + headingHeight, bounds.Width, Math.Max(0, bounds.Height - headingHeight));
        var missingHeight = (missing || !hasDomain) && !missingBounds.HasValue ? Math.Min(row + gap, body.Height) : 0;
        var available = Math.Max(0, body.Height - gap - missingHeight);
        var facts = ChartColorScaleLegend.CaptionFacts(scale, labels, min, max, hasDomain, formatValues, legendStyle);
        var stripHeight = hasDomain ? vertical ? available : Math.Min(12, available) : 0;
        var strip = new ChartRect(body.Left, body.Top + gap, vertical ? Math.Min(row, body.Width * .2) : body.Width, stripHeight);
        var captionArea = vertical
            ? new ChartRect(strip.Right + gap, strip.Top, Math.Max(0, body.Right - strip.Right - gap), strip.Height)
            : new ChartRect(body.Left, strip.Bottom + gap / 2, body.Width, Math.Max(0, body.Bottom - missingHeight - strip.Bottom - gap / 2));
        var plan = PlanCaptions(builder, facts, style, gap, captionArea.Width, hasDomain ? captionArea.Height : 0, vertical);
        var metadata = new Dictionary<string, string> {
            ["data-cfx-scale-mode"] = scale.Mode.ToString().ToLowerInvariant(),
            ["data-cfx-caption-count"] = VisualStateSceneTools.Number(scale.Captions.Count),
            ["data-cfx-has-domain"] = hasDomain ? "true" : "false"
        };
        if (legendStyle != ChartColorLegendStyle.Auto) metadata["data-cfx-legend-style"] = legendStyle.ToString().ToLowerInvariant();
        if (hasDomain) {
            metadata["data-cfx-min-value"] = VisualStateSceneTools.Number(scale.EffectiveMinimum(min));
            metadata["data-cfx-max-value"] = VisualStateSceneTools.Number(scale.EffectiveMaximum(max));
            metadata["data-cfx-midpoint-value"] = VisualStateSceneTools.Number(scale.EffectiveMidpoint(min, max));
        }
        var description = string.Join("; ", facts.Select(fact => fact.Text));
        if (!hasDomain) description = labels.NoData + (description.Length > 0 ? "; " + description : "");
        builder.AddRegion(new VisualSemanticRegion(prefix, prefix, bounds, description));
        using (builder.PushGroup(prefix, prefix, metadata)) {
            Heading(builder, bounds, style, prefix, heading);
            PositionedStops(builder, scale, bounds, prefix, formatValues, valueName);
            using (builder.PushClip(body)) {
                if (hasDomain && strip.Width > 0 && strip.Height > 0) {
                    if (legendStyle == ChartColorLegendStyle.Gradient) Gradient(builder, scale, strip, min, max, prefix + "-gradient", vertical);
                    else CaptionedSwatches(builder, scale, strip, min, max, prefix, formatValues, valueName, vertical);
                }
                PaintCaptionPlan(builder, plan, captionArea, strip, prefix, gap, vertical);
                if (missingHeight > 0) CaptionedMissing(builder, labels, style,
                    new ChartRect(body.Left, body.Bottom - Math.Min(row, missingHeight), body.Width, Math.Min(row, missingHeight)), prefix, missingBlend, scale.NoDataColor == null);
            }
            if (missing && missingBounds.HasValue) using (builder.PushClip(missingBounds.Value))
                CaptionedMissing(builder, labels, style, missingBounds.Value, prefix, missingBlend, scale.NoDataColor == null);
        }
        if (plan.Omitted) builder.AddDiagnostic(new VisualDiagnostic("color-scale.caption-overflow",
            "Some numeric color captions do not fit at their value positions; complete captions remain in descriptive regions."));
        if (hasDomain && stripHeight + .000001 < 12 && !vertical) builder.AddDiagnostic(new VisualDiagnostic(diagnosticCode,
            "The numeric color key does not fit its measured viewport; complete values and captions remain in descriptive regions."));
    }

    private static void CaptionedSwatches(VisualSceneBuilder builder, ChartColorScale scale, ChartRect strip, double min,
        double max, string prefix, Func<IReadOnlyList<double>, string[]> formatValues, string? valueName, bool vertical) {
        var steps = ChartColorScaleLegend.Steps(scale, min, max); var formatted = formatValues(steps);
        for (var index = 0; index < steps.Length; index++) {
            var source = new Dictionary<string, string> { ["data-cfx-value"] = VisualStateSceneTools.Number(steps[index]),
                ["data-cfx-formatted-value"] = formatted[index] };
            if (valueName != null) source["data-cfx-label-value"] = valueName;
            var box = vertical ? new ChartRect(strip.Left, strip.Top + strip.Height * (steps.Length - 1 - index) / steps.Length, strip.Width, strip.Height / steps.Length)
                : new ChartRect(strip.Left + strip.Width * index / steps.Length, strip.Top, strip.Width / steps.Length, strip.Height);
            var blend = scale.BlendFor(steps[index], min, max);
            using (VisualStateSceneTools.Mark(builder, prefix + "-" + index, prefix + "-step", box, formatted[index], source))
                builder.Rect(box, blend.Color, radius: 1, role: prefix + "-swatch", paint: VisualChartPaint.Fill(blend.Paint));
        }
    }

    private static void PaintCaptionPlan(VisualSceneBuilder builder, CaptionPlan plan, ChartRect area, ChartRect strip,
        string prefix, double gap, bool vertical) {
        for (var index = 0; index < plan.Facts.Count; index++) {
            var fact = plan.Facts[index];
            var visible = plan.Placements.TryGetValue(index, out var label);
            var metadata = new Dictionary<string, string> {
                ["data-cfx-caption-value"] = VisualStateSceneTools.Number(fact.Value), ["data-cfx-caption-label"] = fact.Label,
                ["data-cfx-caption-formatted-value"] = fact.Formatted, ["data-cfx-caption-origin"] = fact.Origin,
                ["data-cfx-caption-in-domain"] = fact.InDomain ? "true" : "false", ["data-cfx-caption-visible"] = visible ? "true" : "false",
                ["aria-label"] = fact.Text
            };
            if (fact.InDomain) metadata["data-cfx-caption-ratio"] = VisualStateSceneTools.Number(fact.Ratio);
            var box = visible ? new ChartRect(area.Left + label!.Bounds.Left, area.Top + label.Bounds.Top, label.Bounds.Width, label.Bounds.Height)
                : new ChartRect(area.Left, area.Top, 0, 0);
            builder.AddRegion(new VisualSemanticRegion(prefix + "-caption-" + index, prefix + "-caption", box, fact.Text));
            using (builder.PushGroup(null, prefix + "-caption-source", metadata)) {
                if (!visible) continue;
                var painted = label!;
                var paint = VisualChartPaint.Text(painted.Request.Style);
                if (vertical) {
                    var y = strip.Top + strip.Height * (1 - fact.Ratio);
                    builder.Line(strip.Right, y, Math.Min(area.Left, strip.Right + gap / 2), y, painted.Request.Style.Color, .75,
                        role: prefix + "-caption-tick", paint: VisualChartPaint.Stroke(paint));
                } else {
                    var x = strip.Left + strip.Width * fact.Ratio;
                    builder.Line(x, strip.Bottom, x, Math.Min(area.Top, strip.Bottom + gap / 2), painted.Request.Style.Color, .75,
                        role: prefix + "-caption-tick", paint: VisualChartPaint.Stroke(paint));
                }
                VisualAxisText.Draw(builder, new PlacedLabel(painted.Request, painted.Text, box, false, painted.CandidateIndex), prefix + "-label");
            }
        }
    }

    private static void CaptionedMissing(VisualSceneBuilder builder, ChartLabels labels, TextStyle style, ChartRect area,
        string prefix, ChartColorBlend blend, bool hatch) {
        var row = builder.MeasureText("Mg", style).Height;
        var size = Math.Min(row * .7, Math.Min(area.Width, area.Height));
        var box = new ChartRect(area.Left, area.Top + Math.Max(0, (area.Height - size) / 2), size, size);
        using (VisualStateSceneTools.Mark(builder, prefix + "-empty", prefix + "-empty", box, labels.NoData,
            new Dictionary<string, string> { ["data-cfx-empty"] = "true" })) {
            builder.Rect(box, blend.Color, radius: 1, role: prefix + "-missing-swatch", paint: VisualChartPaint.Fill(blend.Paint));
            if (hatch) ChartMissingData.Hatch(builder, ChartPathBuilder.RoundedRectangle(box, 1), style.Color, prefix + "-missing-swatch");
        }
        VisualStateSceneTools.Text(builder, labels.NoData, new ChartRect(area.Left + row, area.Top, Math.Max(0, area.Width - row), area.Height),
            style, prefix + "-label", prefix + "-label-missing");
    }
}
