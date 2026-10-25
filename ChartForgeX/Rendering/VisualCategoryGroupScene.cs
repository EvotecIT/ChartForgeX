using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>One measured parent-tier scene owner for Cartesian categories and rectangular matrix slots.</summary>
internal static class VisualCategoryGroupScene {
    /// <summary>Measures parent tiers inside the existing axis allocation, without shrinking ordinary authored type.</summary>
    internal static double[] Measure(VisualSceneBuilder builder, ChartCategoryGroupPlan plan, IReadOnlyList<ChartCategoryGroupSpan> spans, TextStyle style,
        double spacing, double minimum, double maximum, double trackLength, double budget) {
        var depthCount = spans.Select(span => span.Run.Depth + 1).DefaultIfEmpty(0).Max();
        if (depthCount == 0) return Array.Empty<double>();
        var sizes = new double[depthCount];
        if (budget <= 0 || trackLength <= 0) return sizes;
        var axis = plan.Axis;
        var horizontal = plan.Horizontal;
        var perTierBudget = horizontal ? budget / depthCount : budget;
        var remaining = budget;
        // The closest parent is adjacent to its leaves; outer ancestors use only remaining space.
        for (var depth = depthCount - 1; depth >= 0; depth--) {
            var extent = 0d;
            foreach (var span in spans.Where(span => span.Run.Depth == depth)) {
                if (!TryVisibleGroupSpan(span, minimum, maximum, value =>
                    ChartScaleTransform.Normalize(value, minimum, maximum, axis) * trackLength, out var start, out var end)) continue;
                var source = span.Run.Path[depth];
                if (horizontal) {
                    var width = Math.Max(0, perTierBudget - spacing);
                    var display = FitCategoryGroupCaption(builder, source, style, width, out _);
                    extent = Math.Max(extent, builder.MeasureText(display, style).Width + spacing);
                } else {
                    var display = FitCategoryGroupCaption(builder, source, style, Math.Max(0, end - start - spacing), out _);
                    // A later gutter refit can turn a complete multiword caption into two lines.
                    // Reserve that bounded possibility once instead of clipping its ordinary type.
                    var measured = source.IndexOf('\n') < 0 && source.IndexOf('\r') < 0 && source.Trim().IndexOf(' ') > 0
                        ? builder.MeasureText("Mg\nMg", style).Height : builder.MeasureText(display, style).Height;
                    extent = Math.Max(extent, measured + spacing);
                }
            }
            sizes[depth] = Math.Min(horizontal ? perTierBudget : remaining, extent);
            remaining = Math.Max(0, remaining - sizes[depth]);
        }
        return sizes;
    }

    /// <summary>Emits passive source facts and shared SVG/raster ink after intersecting source cells with the visible domain.</summary>
    internal static void Draw(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartCategoryGroupPlan plan, IReadOnlyList<ChartCategoryGroupSpan> spans, Func<double, double> coordinate, double minimum, double maximum, ChartRect band,
        VisualThemeColors colors, double[]? measuredBandSizes = null, List<LabelObstacle>? obstacles = null,
        string diagnosticCode = "cartesian.category-group-overflow", string? coordinateSpace = null, bool includeLabels = true) {
        var depthCount = spans.Select(span => span.Run.Depth + 1).DefaultIfEmpty(0).Max();
        if (depthCount == 0) return;
        var horizontal = plan.Horizontal;
        var axis = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        var role = horizontal ? "axis-y-category-group" : "axis-x-category-group";
        var style = chart.Options.TickLabelStyle.Resolve(new TextStyle {
            Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = colors.MutedForeground
        });
        var spacing = context.Theme.Spacing;
        var axisVisible = includeLabels && chart.Options.ShowAxes && axis.Visible;
        var paint = axisVisible && band.Width > 0 && band.Height > 0;
        var sizes = measuredBandSizes ?? (horizontal ? Measure(builder, plan, spans, style, spacing,
            minimum, maximum, band.Height, band.Width)
            : Measure(builder, plan, spans, style, spacing, minimum, maximum, band.Width, band.Height));
        var shortened = false;
        var hasVisibleGroup = false;
        var consumed = 0d;
        var index = 0;
        for (var depth = depthCount - 1; depth >= 0; depth--) {
            var tier = horizontal
                ? new ChartRect(band.Right - consumed - sizes[depth], band.Top, sizes[depth], band.Height)
                : new ChartRect(band.Left, band.Top + consumed, band.Width, sizes[depth]);
            consumed += sizes[depth];
            var entries = new List<(ChartCategoryGroupSpan Span, string Id, ChartRect Bounds, string Text, bool Shortened)>();
            foreach (var span in spans.Where(span => span.Run.Depth == depth)) {
                var id = role + "-" + Number(depth) + "-" + Number(index++);
                var visible = TryVisibleGroupSpan(span, minimum, maximum, coordinate, out var start, out var end);
                hasVisibleGroup |= visible;
                var bounds = !visible ? default : horizontal
                    ? new ChartRect(tier.Left, start, tier.Width, end - start)
                    : new ChartRect(start, tier.Top, end - start, tier.Height);
                var source = span.Run.Path[depth];
                var display = FitCategoryGroupCaption(builder, source, style,
                    Math.Max(0, bounds.Width - spacing), out var clipped);
                entries.Add((span, id, bounds, display, clipped));
            }
            // Shortening must not manufacture identical visible names for different siblings.
            var ambiguous = new HashSet<string>(entries.Where(entry => entry.Text.Length > 0)
                .GroupBy(entry => TextCaseTransformer.Apply(entry.Text, style.TextCase, CultureInfo.InvariantCulture), StringComparer.Ordinal)
                .Where(group => group.Select(entry => entry.Span.Run.Path[depth]).Distinct(StringComparer.Ordinal).Skip(1).Any())
                .Select(group => group.Key), StringComparer.Ordinal);
            foreach (var entry in entries) {
                var span = entry.Span;
                var fullPath = string.Join(" / ", span.Run.Path);
                builder.AddRegion(new VisualSemanticRegion(entry.Id, role, entry.Bounds, fullPath));
                var metadata = CategoryGroupMetadata(span);
                if (coordinateSpace != null) metadata["data-cfx-group-coordinate-space"] = coordinateSpace;
                using (builder.PushGroup(entry.Id, role, metadata)) {
                    if (!paint || entry.Bounds.Width <= 0 || entry.Bounds.Height <= 0) continue;
                    var textBounds = horizontal
                        ? new ChartRect(entry.Bounds.Left + spacing / 2, entry.Bounds.Top,
                            Math.Max(0, entry.Bounds.Width - spacing), entry.Bounds.Height)
                        : new ChartRect(entry.Bounds.Left + spacing / 2, entry.Bounds.Top + spacing / 2,
                            Math.Max(0, entry.Bounds.Width - spacing), Math.Max(0, entry.Bounds.Height - spacing / 2));
                    using (builder.PushClip(entry.Bounds)) {
                        if (axis.ShowLine) DrawCategoryGroupRule(context, builder, entry.Bounds, colors, horizontal);
                        var display = TextCaseTransformer.Apply(entry.Text, style.TextCase, CultureInfo.InvariantCulture);
                        if (display.Length == 0 || display.Replace("…", string.Empty).Trim().Length == 0 || ambiguous.Contains(display)) {
                            shortened = true;
                            continue;
                        }
                        var anchor = horizontal ? new ChartPoint(textBounds.Left + textBounds.Width / 2, textBounds.Top + textBounds.Height / 2)
                            : new ChartPoint(textBounds.Left + textBounds.Width / 2, textBounds.Top);
                        var request = new LabelPlacementRequest(entry.Text, anchor, style,
                            new[] { new LabelCandidate(0, 0, .5, horizontal ? .5 : 0) }) {
                            MeasuredSize = builder.MeasureText(entry.Text, style), Fallback = LabelFallbackRule.Drop
                        };
                        var label = new LabelPlacementService().Place(new[] { request }, textBounds, obstacles, 0, builder.MeasureText)[0];
                        shortened |= entry.Shortened || label.IsDropped;
                        VisualAxisText.Draw(builder, label, role + "-label");
                        if (!label.IsDropped) obstacles?.Add(new LabelObstacle(entry.Id + "-text", label.Bounds));
                    }
                }
            }
        }
        if (axisVisible && hasVisibleGroup && (shortened || sizes.Any(size => size <= 0))) builder.AddDiagnostic(new VisualDiagnostic(
            diagnosticCode, "A category parent caption was shortened or omitted inside its measured axis band; complete paths and source spans are retained."));
    }

    private static Dictionary<string, string> CategoryGroupMetadata(ChartCategoryGroupSpan span) {
        var run = span.Run;
        var metadata = new Dictionary<string, string> {
            ["data-cfx-group-depth"] = Number(run.Depth), ["data-cfx-group-count"] = Number(run.Path.Count),
            ["data-cfx-group-first-leaf"] = Number(run.FirstLeafIndex), ["data-cfx-group-last-leaf"] = Number(run.LastLeafIndex),
            ["data-cfx-group-minimum"] = Number(run.Minimum), ["data-cfx-group-maximum"] = Number(run.Maximum),
            ["data-cfx-group-start"] = Number(span.Start), ["data-cfx-group-end"] = Number(span.End)
        };
        for (var index = 0; index < run.Path.Count; index++) metadata["data-cfx-group-" + Number(index)] = run.Path[index];
        return metadata;
    }

    private static string FitCategoryGroupCaption(VisualSceneBuilder builder, string source, TextStyle style, double width, out bool shortened) {
        if (builder.MeasureText(source, style).Width <= width) { shortened = false; return source; }
        if (source.IndexOf('\n') < 0 && source.IndexOf('\r') < 0)
            return VisualAxisText.FitWrappedCategory(builder, source, style, width, out shortened);
        var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var fitted = lines.Select(line => ChartTextFitting.FitEnd(line, width, value => builder.MeasureText(value, style).Width, "…")).ToArray();
        shortened = !lines.SequenceEqual(fitted);
        return string.Join("\n", fitted);
    }

    private static bool TryVisibleGroupSpan(ChartCategoryGroupSpan span, double minimum, double maximum,
        Func<double, double> coordinate, out double start, out double end) {
        start = end = 0;
        var clippedStart = Math.Max(span.Start, minimum); var clippedEnd = Math.Min(span.End, maximum);
        if (clippedEnd <= clippedStart) return false;
        var first = coordinate(clippedStart); var last = coordinate(clippedEnd);
        if (!ChartMath.IsFinite(first) || !ChartMath.IsFinite(last)) return false;
        start = Math.Min(first, last); end = Math.Max(first, last);
        return end > start;
    }

    private static void DrawCategoryGroupRule(VisualRenderContext context, VisualSceneBuilder builder, ChartRect bounds,
        VisualThemeColors colors, bool horizontal) {
        var inset = Math.Min(2, horizontal ? bounds.Height / 2 : bounds.Width / 2);
        var tick = Math.Min(3, horizontal ? bounds.Width / 4 : bounds.Height / 4);
        var width = context.Theme.AxisStrokeWidth;
        var paint = VisualChartPaint.Stroke(colors.Axis, SvgColorRole.Axis);
        if (horizontal) {
            var x = bounds.Right - .5;
            builder.Line(x, bounds.Top + inset, x, bounds.Bottom - inset, colors.Axis, width, role: "axis-category-group-rule", paint: paint);
            builder.Line(x - tick, bounds.Top + inset, x, bounds.Top + inset, colors.Axis, width, role: "axis-category-group-rule", paint: paint);
            builder.Line(x - tick, bounds.Bottom - inset, x, bounds.Bottom - inset, colors.Axis, width, role: "axis-category-group-rule", paint: paint);
        } else {
            var y = bounds.Top + .5;
            builder.Line(bounds.Left + inset, y, bounds.Right - inset, y, colors.Axis, width, role: "axis-category-group-rule", paint: paint);
            builder.Line(bounds.Left + inset, y, bounds.Left + inset, y + tick, colors.Axis, width, role: "axis-category-group-rule", paint: paint);
            builder.Line(bounds.Right - inset, y, bounds.Right - inset, y + tick, colors.Axis, width, role: "axis-category-group-rule", paint: paint);
        }
    }
    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    private static string Number(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
}
