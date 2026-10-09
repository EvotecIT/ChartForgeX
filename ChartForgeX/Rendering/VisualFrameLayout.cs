using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal sealed class VisualLegendEntry {
    internal VisualLegendEntry(string label, ChartColor color, string id, ChartSeriesKind? kind = null,
        ChartFillPattern pattern = ChartFillPattern.None, ChartSeriesState stateRole = ChartSeriesState.None, string? seriesKey = null,
        ChartStateCategory? state = null, bool pinStateColors = false, Action<VisualSceneBuilder, ChartRect, VisualRenderContext>? marker = null, SvgPaint? paint = null, string? value = null, string? percentage = null) {
        Label = label; Color = color; Id = id; Kind = kind; Pattern = pattern; StateRole = stateRole; SeriesKey = seriesKey;
        State = state; PinStateColors = pinStateColors; Marker = marker; Paint = paint; Value = value; Percentage = percentage;
    }
    internal string Label { get; }
    internal string? Value { get; }
    internal string? Percentage { get; }
    internal string Description => string.IsNullOrEmpty(Value) ? Label : Label + ": " + Value + (string.IsNullOrEmpty(Percentage) ? "" : " (" + Percentage + ")");
    internal ChartColor Color { get; }
    internal string Id { get; }
    internal ChartSeriesKind? Kind { get; }
    internal ChartFillPattern Pattern { get; }
    internal ChartSeriesState StateRole { get; }
    internal string? SeriesKey { get; }
    internal ChartStateCategory? State { get; }
    internal bool PinStateColors { get; }
    internal Action<VisualSceneBuilder, ChartRect, VisualRenderContext>? Marker { get; }
    internal SvgPaint? Paint { get; }
}

/// <summary>Measures and paints one common frame before any family lays out its marks.</summary>
internal static class VisualFrameLayout {
    internal static ChartRect Build(VisualSceneBuilder builder, VisualRenderContext context, IReadOnlyList<VisualLegendEntry> entries, VisualFramePaints? paints = null) {
        var size = context.Layout.Size;
        var colors = context.Theme.Resolve(context.ThemeMode);
        var typography = context.Theme.Typography;
        var gap = context.Theme.Spacing;
        var pad = context.Layout.PaddingEdges;
        TextStyle Style(TextStyle? explicitStyle, double size, int weight, ChartColor color) {
            if (explicitStyle != null) return explicitStyle;
            var font = context.Font; font.Weight = weight;
            return new TextStyle { Font = font, FontSize = size, Color = color, LineHeight = 1 };
        }
        var titleStyle = Style(context.Frame.TitleStyle, typography.TitleSize, 700, colors.Foreground);
        var subtitleStyle = Style(context.Frame.SubtitleStyle, typography.SubtitleSize, 400, colors.MutedForeground);
        var legendStyle = Style(context.Frame.LegendStyle, typography.LegendSize, 400, colors.Foreground);
        if (!context.Frame.TransparentBackground) builder.Rect(new ChartRect(0, 0, size.Width, size.Height), colors.Background, role: "background", paint: VisualChartPaint.Fill(paints?.Background ?? SvgPaint.Of(colors.Background, SvgColorRole.Surface)));
        if (context.Frame.ShowCard) VisualFrameSurface.Paint(builder, context, colors, paints);
        var left = pad.Left; var right = size.Width - pad.Right; var top = pad.Top; var bottom = size.Height - pad.Bottom;
        using (builder.PushClip(new ChartRect(left, top, right - left, bottom - top))) {
            var headingTop = top;
            Header(context.Frame.Title, titleStyle, paints?.Foreground);
            if (top > headingTop && !string.IsNullOrEmpty(context.Frame.Subtitle)) top += gap / 6;
            Header(context.Frame.Subtitle, subtitleStyle, paints?.MutedForeground);
            if (top > headingTop) top += gap * 5 / 6;
        }
        if (context.Frame.ShowLegend && entries.Count > 0) {
            Dictionary<VisualLegendEntry, (double Value, double Percentage)>? numericWidths = null;
            var position = context.Frame.LegendPosition;
            var side = position == ChartLegendPosition.Left || position == ChartLegendPosition.Right;
            var above = position is ChartLegendPosition.Top or ChartLegendPosition.TopLeft or ChartLegendPosition.TopRight;
            var legendWidth = side ? Math.Min((right - left) * 0.32, 180) : right - left;
            var rows = new List<List<VisualLegendEntry>>(); var row = new List<VisualLegendEntry>(); var used = 0d;
            foreach (var entry in entries) {
                var width = LegendWidth(entry, 28);
                if (row.Count > 0 && (side || used + gap + width > legendWidth)) { rows.Add(row); row = new(); used = 0; }
                row.Add(entry); used += (row.Count > 1 ? gap : 0) + width;
            }
            if (row.Count > 0) rows.Add(row);
            var lineHeight = Math.Max(legendStyle.EffectiveFontSize * 1.5, builder.MeasureText("Mg", legendStyle).Height);
            var available = Math.Max(0, Math.Min(bottom - top, size.Height * context.Frame.LegendMaximumHeightFraction));
            var legendGap = Math.Min(gap, Math.Max(0, available - lineHeight));
            var legendTitle = context.Frame.LegendTitle;
            var legendTitleStyle = legendStyle.Clone(); legendTitleStyle.Font.Weight = 600;
            var titleLineHeight = Math.Max(legendTitleStyle.EffectiveFontSize * 1.3, builder.MeasureText("Mg", legendTitleStyle).Height);
            var titleGap = Math.Min(gap, lineHeight * .25);
            var titleHeight = !string.IsNullOrWhiteSpace(legendTitle) && available >= titleLineHeight + titleGap + lineHeight + legendGap
                ? titleLineHeight + titleGap : 0;
            var maximumRows = available - titleHeight < lineHeight ? 0 : (int)Math.Floor((available - titleHeight - legendGap) / lineHeight);
            if (context.Frame.LegendMaximumRows.HasValue) maximumRows = Math.Min(maximumRows, context.Frame.LegendMaximumRows.Value);
            var omitted = 0; VisualLegendEntry? overflow = null;
            LegendRowBudget.Apply(rows, maximumRows, row => row.Count, count => {
                omitted = count;
                overflow = new VisualLegendEntry(LegendRowBudget.Summary(count), colors.MutedForeground, "overflow");
                return new List<VisualLegendEntry> { overflow };
            });
            if (maximumRows == 0) omitted = entries.Count;
            var visible = rows.Count;
            if (omitted > 0) builder.AddDiagnostic(new VisualDiagnostic("frame.legend-overflow", "Some legend entries do not fit the resolved frame; all entries remain in descriptive regions."));
            var height = visible * lineHeight + titleHeight;
            var x = position == ChartLegendPosition.Right ? right - legendWidth : left;
            var y = side || above ? top : bottom - height;
            using (builder.PushClip(new ChartRect(x, y, legendWidth, height))) {
                if (!string.IsNullOrWhiteSpace(legendTitle)) {
                    var titleBounds = new ChartRect(x, y, titleHeight > 0 ? legendWidth : 0, titleHeight > 0 ? titleLineHeight : 0);
                    builder.AddRegion(new VisualSemanticRegion("frame-legend-title", "legend-title", titleBounds, legendTitle));
                    if (titleHeight > 0) {
                        var anchor = legendTitleStyle.Alignment == TextAlignment.Center ? x + legendWidth / 2
                            : legendTitleStyle.Alignment == TextAlignment.Right ? x + legendWidth : x;
                        builder.Text(Fit(OneLine(legendTitle!), legendWidth, legendTitleStyle), anchor,
                            y + builder.TextAscent(legendTitleStyle), legendTitleStyle, id: "frame-legend-title", role: "legend-title",
                            paint: paints?.Foreground ?? VisualChartPaint.Text(legendTitleStyle));
                    } else builder.AddDiagnostic(new VisualDiagnostic("frame.legend-title-overflow", "The legend title does not fit; its complete value remains in descriptive regions."));
                }
                for (var r = 0; r < visible; r++) {
                    var rowWidth = 0d;
                    foreach (var entry in rows[r]) rowWidth += LegendWidth(entry, ReferenceEquals(entry, overflow) ? 0 : 28) + gap;
                    rowWidth = Math.Max(0, rowWidth - gap);
                    var alignRight = position is ChartLegendPosition.TopRight or ChartLegendPosition.BottomRight;
                    var alignCenter = position is ChartLegendPosition.Top or ChartLegendPosition.Bottom;
                    var cursor = x + (alignRight ? legendWidth - rowWidth : alignCenter ? (legendWidth - rowWidth) / 2 : 0);
                    foreach (var entry in rows[r]) {
                        var isSummary = ReferenceEquals(entry, overflow);
                        var width = LegendWidth(entry, isSummary ? 0 : 28);
                        var baseline = y + titleHeight + r * lineHeight + builder.TextAscent(legendStyle);
                        var swatch = new ChartRect(cursor, baseline - legendStyle.EffectiveFontSize * 0.65, 10, 10);
                        using (builder.PushGroup("legend-" + entry.Id, "legend-entry", new Dictionary<string, string> {
                            ["data-cfx-series-key"] = entry.SeriesKey ?? "", ["data-cfx-state"] = entry.StateRole.ToString(),
                            ["aria-label"] = entry.Description
                        })) {
                            if (!ReferenceEquals(entry, overflow)) using (builder.PushClip(swatch)) {
                            if (entry.Marker != null) entry.Marker(builder, swatch, context);
                            else if (entry.State != null) {
                                using (builder.PushGroup("legend-" + entry.Id + "-swatch", "state-legend-swatch",
                                    VisualStateSceneTools.StateMetadata(entry.PinStateColors, entry.State)))
                                    VisualStateSceneTools.StateRect(builder, swatch, entry.State, colors, ChartStateCategoryLegend.SwatchRadius, "legend-swatch");
                            } else if (entry.Kind is ChartSeriesKind.Line or ChartSeriesKind.StepLine or ChartSeriesKind.TrendLine)
                                builder.Line(swatch.Left, swatch.Top + 5, swatch.Right, swatch.Top + 5, entry.Color, context.Theme.SeriesStrokeWidth, role: "legend-swatch", paint: VisualChartPaint.Stroke(entry.Paint ?? SvgPaint.Literal(entry.Color)));
                            else {
                                builder.Rect(swatch, entry.Color, role: "legend-swatch", paint: VisualChartPaint.Fill(entry.Paint ?? SvgPaint.Literal(entry.Color)));
                                DrawLegendPattern(builder, swatch, entry.Pattern, context);
                            }
                            }
                            var fullLabel = OneLine(entry.Label);
                            // The summary has no swatch. Use that space for its count, and shorten the wording before
                            // trimming so a compact side legend still explains that more entries are available.
                            if (isSummary && builder.MeasureText(fullLabel, legendStyle).Width > width)
                                fullLabel = "+ " + omitted.ToString(CultureInfo.InvariantCulture) + " more";
                            var valueWidth = ValueWidth(entry);
                            var labelWidth = Math.Max(0, width - (isSummary ? 0 : 22) - (valueWidth > 0 ? valueWidth + gap : 0));
                            var label = Fit(fullLabel, labelWidth, legendStyle);
                            var anchor = cursor + (isSummary ? 0 : 18) + (legendStyle.Alignment == TextAlignment.Center ? labelWidth / 2 : legendStyle.Alignment == TextAlignment.Right ? labelWidth : 0);
                            builder.Text(label, anchor, baseline, legendStyle, role: "legend-label", paint: paints?.Foreground ?? VisualChartPaint.Text(legendStyle));
                            if (valueWidth > 0) {
                                var valueStyle = legendStyle.Clone(); valueStyle.Alignment = TextAlignment.Right;
                                var percentageWidth = NumericWidths(entry).Percentage;
                                var valueRight = cursor + width - 4;
                                if (percentageWidth > 0) {
                                    var percentageStyle = valueStyle.Clone(); percentageStyle.Color = colors.MutedForeground;
                                    builder.Text(entry.Percentage!, valueRight, baseline, percentageStyle, role: "legend-percentage", paint: paints?.MutedForeground ?? VisualChartPaint.Text(percentageStyle));
                                    valueRight -= percentageWidth + gap * 2 / 3;
                                }
                                builder.Text(Fit(entry.Value!, Math.Max(0, valueRight - cursor - (isSummary ? 0 : 22)), valueStyle), valueRight, baseline, valueStyle, role: "legend-value", paint: paints?.Foreground ?? VisualChartPaint.Text(valueStyle));
                            }
                        }
                        builder.AddRegion(new VisualSemanticRegion("legend-" + entry.Id, "legend", new ChartRect(cursor, y + titleHeight + r * lineHeight, width, lineHeight), entry.Description));
                        cursor += width + gap;
                    }
                }
            }
            if (omitted > 0) {
                var shown = new HashSet<VisualLegendEntry>();
                foreach (var visibleRow in rows) foreach (var entry in visibleRow) shown.Add(entry);
                foreach (var entry in entries) if (!shown.Contains(entry)) {
                    builder.AddRegion(new VisualSemanticRegion("legend-" + entry.Id, "legend", new ChartRect(x, y, 0, 0), entry.Description));
                    using (builder.PushGroup("legend-" + entry.Id, "legend-entry-omitted", new Dictionary<string, string> {
                        ["data-cfx-series-key"] = entry.SeriesKey ?? "", ["aria-label"] = entry.Description
                    })) { }
                }
            }
            if (height > 0) {
                if (side) { if (position == ChartLegendPosition.Left) left += legendWidth + legendGap; else right -= legendWidth + legendGap; }
                else if (above) top += height + legendGap; else bottom -= height + legendGap;
            }
            double LegendWidth(VisualLegendEntry entry, double overhead) {
                var valueWidth = ValueWidth(entry);
                if (valueWidth > 0) valueWidth += gap;
                var label = OneLine(entry.Label); var availableWidth = Math.Max(0, legendWidth - overhead - valueWidth);
                var length = ChartTextFitting.PrefixLength(label, availableWidth, text => builder.MeasureText(text, legendStyle).Width);
                return length == label.Length ? Math.Min(legendWidth, builder.MeasureText(label, legendStyle).Width + overhead + valueWidth) : legendWidth;
            }
            double ValueWidth(VisualLegendEntry entry) {
                if (string.IsNullOrEmpty(entry.Value)) return 0;
                var widths = NumericWidths(entry);
                return widths.Value + (string.IsNullOrEmpty(entry.Percentage) ? 0 : widths.Percentage + gap * 2 / 3);
            }
            (double Value, double Percentage) NumericWidths(VisualLegendEntry entry) {
                if (string.IsNullOrEmpty(entry.Value)) return default;
                numericWidths ??= new Dictionary<VisualLegendEntry, (double Value, double Percentage)>();
                if (!numericWidths.TryGetValue(entry, out var widths)) {
                    // Wrapping, row alignment and paint placement share this frame's unchanged legend style.
                    widths = (builder.MeasureText(entry.Value!, legendStyle).Width,
                        string.IsNullOrEmpty(entry.Percentage) ? 0 : builder.MeasureText(entry.Percentage!, legendStyle).Width);
                    numericWidths.Add(entry, widths);
                }
                return widths;
            }
        }
        if (bottom <= top || right <= left) {
            builder.AddDiagnostic(new VisualDiagnostic("frame.insufficient-space", "The frame leaves no content viewport."));
            return new ChartRect(left, Math.Min(top, size.Height - pad.Bottom), Math.Max(0, right - left), 0);
        }
        var content = new ChartRect(left, top, right - left, bottom - top);
        if (context.Frame.ShowSurface) builder.Rect(content, colors.Surface, colors.Border, radius: context.Theme.BarRadius, role: "content-surface",
            paint: new VisualScenePaintBinding(paints?.Surface ?? SvgPaint.Of(colors.Surface, SvgColorRole.Surface), paints?.Border ?? SvgPaint.Of(colors.Border, SvgColorRole.Surface)));
        return content;

        void Header(string? text, TextStyle style, SvgPaint? paint) {
            if (string.IsNullOrEmpty(text)) return;
            var fontSize = style.EffectiveFontSize;
            var lineHeight = Math.Max(fontSize * 1.3, builder.MeasureText("Mg", style).Height);
            // Bounded two-line wrapping leaves content space; full text remains in accessibility metadata.
            var paragraphs = new Queue<string>(text!.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'));
            var remainder = paragraphs.Dequeue(); var count = 0;
            while ((remainder.Length > 0 || paragraphs.Count > 0) && count < 2 && top + lineHeight < bottom) {
                if (remainder.Length == 0) { remainder = paragraphs.Dequeue(); continue; }
                var line = Fit(remainder, right - left, style, count == 1);
                if (line.Length == 0) break;
                var anchor = style.Alignment == TextAlignment.Center ? (left + right) / 2 : style.Alignment == TextAlignment.Right ? right : left;
                builder.Text(line, anchor, top + builder.TextAscent(style), style, role: count == 0 ? "frame-heading" : "frame-heading-continuation", paint: paint ?? VisualChartPaint.Text(style));
                top += lineHeight; count++;
                if (line.EndsWith("…", StringComparison.Ordinal)) { remainder = ""; break; }
                remainder = remainder.Substring(Math.Min(remainder.Length, line.Length)).TrimStart();
            }
            if (remainder.Length > 0 || paragraphs.Count > 0) builder.AddDiagnostic(new VisualDiagnostic("frame.heading-overflow", "The heading exceeds the available frame space."));
        }
        string Fit(string text, double width, TextStyle style, bool ellipsis = true) {
            var fitted = ChartTextFitting.FitEnd(text, width, value => builder.MeasureText(value, style).Width, ellipsis ? "…" : "");
            if (fitted == text) return text;
            if (ellipsis) {
                builder.AddDiagnostic(new VisualDiagnostic("frame.text-truncated", "Frame text was shortened; its complete value remains in semantic metadata."));
                return fitted;
            }
            var length = fitted.Length;
            var space = length > 0 ? text.LastIndexOf(' ', length - 1, length) : -1;
            return text.Substring(0, space > 0 ? space : length);
        }
    }
    internal static void DrawLegendPattern(VisualSceneBuilder builder, ChartRect swatch, ChartFillPattern pattern,
        VisualRenderContext context, double opacity = 1, string role = "legend-pattern") {
        if (pattern == ChartFillPattern.None || opacity <= 0) return;
        var surface = context.Theme.Resolve(context.ThemeMode).Surface;
        var color = ChartColorMath.WithOpacity(surface, opacity);
        var paint = SvgPaint.Of(surface, SvgColorRole.Surface);
        if (opacity < 1) paint = paint.WithOpacity(color, opacity);
        builder.Pattern(ChartPathBuilder.RoundedRectangle(swatch, 0), pattern, color,
            spacing: 4, strokeWidth: 1, role: role, paint: paint);
    }

    private static string OneLine(string text) => text.Replace('\r', ' ').Replace('\n', ' ');
}
