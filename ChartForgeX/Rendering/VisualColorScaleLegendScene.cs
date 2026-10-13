using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>One measured numeric key scene for matrix and Cartesian color dimensions.</summary>
internal static partial class VisualColorScaleLegendScene {
    /// <summary>Measures the optional heading lane shared by modern and legacy numeric key owners.</summary>
    internal static double HeadingHeight(VisualSceneBuilder builder, TextStyle style, double gap, string? heading) =>
        heading == null ? 0 : builder.MeasureText("Mg", style).Height + gap;

    /// <summary>Draws a heading within the caller's measured key bounds, preserving its complete semantic caption.</summary>
    internal static void Heading(VisualSceneBuilder builder, ChartRect bounds, TextStyle style, string prefix, string? heading) {
        if (heading == null) return;
        VisualStateSceneTools.Text(builder, heading,
            new ChartRect(bounds.Left, bounds.Top, bounds.Width, Math.Min(builder.MeasureText("Mg", style).Height, bounds.Height)),
            style, prefix + "-title", prefix + "-title");
    }

    private static int Columns(VisualSceneBuilder builder, ChartColorScale scale, ChartLabels labels, TextStyle style, double width, double gap, Func<IReadOnlyList<double>, string[]> formatValues) {
        if (scale.Mode != ChartColorScaleMode.Discrete) return 1;
        var widest = ChartColorScaleLegend.BandCaptions(labels, scale, formatValues).Max(text => builder.MeasureText(text, style).Width);
        return Math.Max(1, Math.Min(scale.Bands.Count, (int)Math.Floor(width / Math.Max(1, widest + gap))));
    }

    /// <summary>Measures the same caption rows that Draw emits, using the caller's numeric formatting policy.</summary>
    internal static double Height(VisualSceneBuilder builder, ChartColorScale scale, ChartLabels labels, TextStyle style,
        double gap, double width, double maximum, bool missing, Func<IReadOnlyList<double>, string[]> formatValues, bool hasDomain = true,
        bool compact = false, double min = 0, double max = 0, bool inlineMissing = false, string? heading = null,
        ChartColorLegendStyle legendStyle = ChartColorLegendStyle.Auto) {
        var row = builder.MeasureText("Mg", style).Height;
        var headingHeight = Math.Max(0, Math.Min(maximum, HeadingHeight(builder, style, gap, heading)));
        maximum = Math.Max(0, maximum - headingHeight);
        if (!hasDomain) return headingHeight + (inlineMissing ? 0 : Math.Max(0, Math.Min(maximum, row + gap)));
        var columns = Columns(builder, scale, labels, style, width, gap, formatValues);
        var rows = scale.Mode == ChartColorScaleMode.Discrete ? (int)Math.Ceiling(scale.Bands.Count / (double)columns) : 1;
        var inlineMidpoint = compact && InlineMidpoint(builder, scale, labels, style, gap, width, min, max, formatValues, legendStyle);
        var extra = (scale.MidpointLabel != null && !inlineMidpoint ? row + gap : 0) + (missing && !inlineMissing ? row + gap : 0);
        return headingHeight + Math.Max(0, Math.Min(maximum, rows * (12 + row + gap) + gap + extra));
    }

    /// <summary>Draws shared canonical stops or bands, retaining full captions and domains independently of viewport fit.</summary>
    internal static void Draw(VisualSceneBuilder builder, ChartColorScale scale, ChartLabels labels, TextStyle style, double gap,
        ChartRect bounds, double min, double max, string prefix, bool missing, ChartColorBlend missingBlend,
        Func<IReadOnlyList<double>, string[]> formatValues, string diagnosticCode = "matrix.scale-overflow", bool hasDomain = true,
        bool compact = false, ChartRect? missingBounds = null, string? heading = null, string? valueName = null,
        ChartColorLegendStyle legendStyle = ChartColorLegendStyle.Auto) {
        var row = builder.MeasureText("Mg", style).Height;
        var fullBounds = bounds;
        var headingHeight = Math.Max(0, Math.Min(bounds.Height, HeadingHeight(builder, style, gap, heading)));
        if (heading != null) bounds = new ChartRect(bounds.Left, bounds.Top + headingHeight, bounds.Width, Math.Max(0, bounds.Height - headingHeight));
        var columns = Columns(builder, scale, labels, style, bounds.Width, gap, formatValues);
        var captions = scale.Mode == ChartColorScaleMode.Discrete ? ChartColorScaleLegend.BandCaptions(labels, scale, formatValues) : null;
        var inlineMidpoint = hasDomain && compact && InlineMidpoint(builder, scale, labels, style, gap, bounds.Width, min, max, formatValues, legendStyle);
        var extra = (scale.MidpointLabel != null && !inlineMidpoint ? row + gap : 0) + (missing && !missingBounds.HasValue ? row + gap : 0);
        var available = Math.Max(0, bounds.Height - gap - extra);
        var rows = captions != null ? (int)Math.Ceiling(captions.Length / (double)columns) : 1;
        if (hasDomain && rows * (12 + row + gap) > available + .000001) {
            // Fit more bands in each row before sacrificing caption height. Complete intervals remain in regions.
            if (captions != null) {
                var capacity = Math.Max(1, (int)Math.Floor(available / (12 + row + gap)));
                columns = Math.Max(columns, (int)Math.Ceiling(captions.Length / (double)capacity));
                rows = (int)Math.Ceiling(captions.Length / (double)columns);
            }
            if (rows * (12 + row + gap) > available + .000001)
                builder.AddDiagnostic(new VisualDiagnostic(diagnosticCode, "The numeric color key does not fit the viewport; complete scale values and captions remain in descriptive regions."));
        }
        var pitchY = available / rows; var swatchHeight = Math.Min(12, Math.Max(0, pitchY - row - gap));
        var y = bounds.Top + gap; var midpointX = bounds.Left + bounds.Width / 2;
        var metadata = new Dictionary<string, string> { ["data-cfx-scale-mode"] = scale.Mode.ToString().ToLowerInvariant() };
        if (legendStyle != ChartColorLegendStyle.Auto) metadata["data-cfx-legend-style"] = legendStyle.ToString().ToLowerInvariant();
        if (captions == null && hasDomain) {
            metadata["data-cfx-min-value"] = VisualStateSceneTools.Number(scale.EffectiveMinimum(min));
            metadata["data-cfx-max-value"] = VisualStateSceneTools.Number(scale.EffectiveMaximum(max));
            metadata["data-cfx-midpoint-value"] = VisualStateSceneTools.Number(scale.EffectiveMidpoint(min, max));
        }
        builder.AddRegion(new VisualSemanticRegion(prefix, prefix, fullBounds, !hasDomain ? labels.NoData : captions != null ? string.Join("; ", captions)
            : string.Join(" – ", formatValues(new[] { scale.EffectiveMinimum(min), scale.EffectiveMaximum(max) }))));
        using (builder.PushGroup(prefix, prefix, metadata)) {
            Heading(builder, fullBounds, style, prefix, heading);
            PositionedStops(builder, scale, fullBounds, prefix, formatValues, valueName);
            if (missing && missingBounds.HasValue) using (builder.PushClip(missingBounds.Value)) Missing(missingBounds.Value.Top, missingBounds);
            using (builder.PushClip(bounds)) {
                if (!hasDomain) {
                    if (!missingBounds.HasValue) Missing(bounds.Top);
                    return;
                }
                if (captions != null) {
                    var width = bounds.Width / columns;
                    for (var index = 0; index < captions.Length; index++) {
                        var band = scale.Bands[index]; var left = bounds.Left + index % columns * width; var top = y + index / columns * pitchY;
                        var source = new Dictionary<string, string> {
                            ["data-cfx-band"] = index.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            ["data-cfx-label"] = captions[index],
                            ["data-cfx-lower-bound"] = index == 0 ? "" : VisualStateSceneTools.Number(scale.Bands[index - 1].UpperBound!.Value),
                            ["data-cfx-upper-bound"] = band.UpperBound.HasValue ? VisualStateSceneTools.Number(band.UpperBound.Value) : "",
                            ["data-cfx-lower-inclusive"] = "true", ["data-cfx-upper-exclusive"] = "true"
                        };
                        if (band.Label != null) source["data-cfx-band-label"] = band.Label;
                        Tile(index, new ChartRect(left, top, Math.Max(0, width - gap / 2), swatchHeight),
                            ChartColorBlend.Solid(band.Color, SvgColorRole.Ramp), captions[index], source);
                        Caption(captions[index], new ChartRect(left, top + swatchHeight + gap / 2, Math.Max(0, width - gap / 2), row), "band-" + index, TextAlignment.Center);
                    }
                } else {
                    var steps = ChartColorScaleLegend.Steps(scale, min, max); var width = bounds.Width / steps.Length;
                    if (scale.Mode == ChartColorScaleMode.Diverging && scale.EffectiveMaximum(max) > scale.EffectiveMinimum(min))
                        midpointX = legendStyle == ChartColorLegendStyle.Gradient ? bounds.Left + bounds.Width * ChartColorScaleLegend.MidpointRatio(scale, min, max)
                            : bounds.Left + (ChartColorScaleLegend.MidpointStep(scale, min, max, steps.Length) + .5) * width;
                    if (legendStyle == ChartColorLegendStyle.Gradient)
                        Gradient(builder, scale, new ChartRect(bounds.Left, y, bounds.Width, swatchHeight), min, max, prefix + "-gradient");
                    else {
                        var stepLabels = formatValues(steps);
                        for (var index = 0; index < steps.Length; index++)
                            Tile(index, new ChartRect(bounds.Left + index * width, y, width, swatchHeight), scale.BlendFor(steps[index], min, max), stepLabels[index],
                                new Dictionary<string, string> { ["data-cfx-value"] = VisualStateSceneTools.Number(steps[index]) });
                    }
                    var endpoints = formatValues(new[] { scale.EffectiveMinimum(min), scale.EffectiveMaximum(max) });
                    Caption((scale.LowLabel ?? labels.Less) + " · " + endpoints[0],
                        new ChartRect(bounds.Left, y + swatchHeight + gap / 2, bounds.Width / 2, row), "low", TextAlignment.Left);
                    Caption((scale.HighLabel ?? labels.More) + " · " + endpoints[1],
                        new ChartRect(bounds.Left + bounds.Width / 2, y + swatchHeight + gap / 2, bounds.Width / 2, row), "high", TextAlignment.Right);
                    if (inlineMidpoint) Midpoint(y + swatchHeight + gap / 2);
                }
                y += rows * pitchY;
                if (scale.MidpointLabel != null && !inlineMidpoint) {
                    Midpoint(y);
                    y += row + gap;
                }
                if (missing && !missingBounds.HasValue) Missing(y);
            }
        }

        void Midpoint(double top) {
            var text = scale.MidpointLabel + " · " + formatValues(new[] { scale.EffectiveMidpoint(min, max) })[0];
            var lane = MidpointCaptionLane(builder, text, style, new ChartRect(bounds.Left, top, bounds.Width, row), midpointX,
                inwardEdges: legendStyle == ChartColorLegendStyle.Gradient);
            Caption(text, lane.Bounds, "midpoint", lane.Alignment);
        }

        void Missing(double top, ChartRect? target = null) {
            var blend = missingBlend;
            var lane = target ?? bounds;
            var box = new ChartRect(lane.Left, top + row * .15, row * .7, row * .7);
            using (VisualStateSceneTools.Mark(builder, prefix + "-empty", prefix + "-empty", box, labels.NoData,
                new Dictionary<string, string> { ["data-cfx-empty"] = "true" }))
                builder.Rect(box, blend.Color, radius: 1, role: prefix + "-missing-swatch", paint: VisualChartPaint.Fill(blend.Paint));
            Caption(labels.NoData, new ChartRect(lane.Left + row, top, Math.Max(0, lane.Width - row), row), "missing", TextAlignment.Left);
        }

        void Tile(int index, ChartRect box, ChartColorBlend blend, string label, Dictionary<string, string> source) {
            if (valueName != null && source.ContainsKey("data-cfx-value")) {
                source["data-cfx-formatted-value"] = label;
                source["data-cfx-label-value"] = valueName;
            }
            using (VisualStateSceneTools.Mark(builder, prefix + "-" + index, prefix + "-step", box, label, source))
                builder.Rect(box, blend.Color, radius: 1, role: prefix + "-swatch", paint: VisualChartPaint.Fill(blend.Paint));
        }
        void Caption(string text, ChartRect box, string id, TextAlignment alignment) {
            var captionStyle = captions != null || id == "midpoint" ? FitCaptionStyle(builder, text, style, box.Width) : style;
            VisualStateSceneTools.Text(builder, text, box, captionStyle, prefix + "-label", prefix + "-label-" + id, alignment);
        }
    }

    /// <summary>Keeps the numeric anchor centered when readable; edge captions face inward within the measured key.</summary>
    internal static (ChartRect Bounds, TextAlignment Alignment) MidpointCaptionLane(VisualSceneBuilder builder,
        string text, TextStyle style, ChartRect lane, double anchor, bool inwardEdges = true) {
        anchor = Math.Max(lane.Left, Math.Min(lane.Right, anchor));
        var symmetric = Math.Max(0, 2 * Math.Min(anchor - lane.Left, lane.Right - anchor));
        if (!inwardEdges || symmetric > 0 && builder.MeasureText(text, FitCaptionStyle(builder, text, style, symmetric)).Width <= symmetric)
            return (new ChartRect(anchor - symmetric / 2, lane.Top, symmetric, lane.Height), TextAlignment.Center);
        return anchor <= lane.Left + lane.Width / 2
            ? (new ChartRect(anchor, lane.Top, Math.Max(0, lane.Right - anchor), lane.Height), TextAlignment.Left)
            : (new ChartRect(lane.Left, lane.Top, Math.Max(0, anchor - lane.Left), lane.Height), TextAlignment.Right);
    }

    /// <summary>Fits bounded key captions using the same measured, readable size policy for every consumer.</summary>
    internal static TextStyle FitCaptionStyle(VisualSceneBuilder builder, string text, TextStyle style, double width) {
        if (builder.MeasureText(text, style).Width <= width) return style;
        var fitted = style.Clone(); var minimum = Math.Min(VisualTextFitting.MinimumOrdinaryFontSize, style.EffectiveFontSize);
        fitted.FontSize = style.EffectiveFontSize; fitted.Baseline = TextBaseline.Normal;
        while (fitted.FontSize > minimum && builder.MeasureText(text, fitted).Width > width)
            fitted.FontSize = Math.Max(minimum, fitted.FontSize - .25);
        return fitted;
    }

    /// <summary>Retains every authored anchor as a source fact, independently of the bounded painted samples.</summary>
    internal static void PositionedStops(VisualSceneBuilder builder, ChartColorScale scale, ChartRect bounds, string prefix,
        Func<IReadOnlyList<double>, string[]> formatValues, string? valueName = null) {
        if (!scale.HasPositionedStops) return;
        var stops = scale.Stops;
        var values = formatValues(stops.Select(stop => stop.Value).ToArray());
        var captions = stops.Select((stop, index) => values[index] + " · " + stop.Color.ToHexRgba()).ToArray();
        var description = string.Join("; ", captions);
        builder.AddRegion(new VisualSemanticRegion(prefix + "-stops", prefix + "-stops", bounds, description));
        using (builder.PushGroup(prefix + "-stops", prefix + "-stops", new Dictionary<string, string> {
            ["data-cfx-positioning"] = "absolute", ["data-cfx-stop-count"] = VisualStateSceneTools.Number(stops.Count),
            ["aria-label"] = description
        })) for (var index = 0; index < stops.Count; index++) {
            var metadata = new Dictionary<string, string> {
                ["data-cfx-stop"] = VisualStateSceneTools.Number(index), ["data-cfx-value"] = VisualStateSceneTools.Number(stops[index].Value),
                ["data-cfx-color"] = stops[index].Color.ToHexRgba(), ["data-cfx-label"] = captions[index], ["aria-label"] = captions[index]
            };
            if (valueName != null) {
                metadata["data-cfx-formatted-value"] = values[index];
                metadata["data-cfx-label-value"] = valueName;
            }
            if (scale.Mode == ChartColorScaleMode.Diverging && scale.MidpointIndex == index) metadata["data-cfx-midpoint"] = "true";
            using (builder.PushGroup(prefix + "-stop-" + VisualStateSceneTools.Number(index), prefix + "-stop-source", metadata)) { }
        }
    }

    // Share a caption row only when actual glyph widths leave all three labels disjoint at their exact anchors.
    private static bool InlineMidpoint(VisualSceneBuilder builder, ChartColorScale scale, ChartLabels labels, TextStyle style,
        double gap, double width, double min, double max, Func<IReadOnlyList<double>, string[]> formatValues, ChartColorLegendStyle legendStyle) {
        if (scale.Mode == ChartColorScaleMode.Discrete || scale.MidpointLabel == null || width <= 0) return false;
        var endpoints = formatValues(new[] { scale.EffectiveMinimum(min), scale.EffectiveMaximum(max) });
        var low = builder.MeasureText((scale.LowLabel ?? labels.Less) + " · " + endpoints[0], style).Width;
        var high = builder.MeasureText((scale.HighLabel ?? labels.More) + " · " + endpoints[1], style).Width;
        var midpoint = builder.MeasureText(scale.MidpointLabel + " · " + formatValues(new[] { scale.EffectiveMidpoint(min, max) })[0], style).Width;
        var steps = ChartColorScaleLegend.Steps(scale, min, max);
        var x = scale.Mode == ChartColorScaleMode.Diverging && scale.EffectiveMaximum(max) > scale.EffectiveMinimum(min)
            ? legendStyle == ChartColorLegendStyle.Gradient ? width * ChartColorScaleLegend.MidpointRatio(scale, min, max)
                : (ChartColorScaleLegend.MidpointStep(scale, min, max, steps.Length) + .5) * width / steps.Length : width / 2;
        return low + gap <= x - midpoint / 2 && x + midpoint / 2 + gap <= width - high;
    }
}
