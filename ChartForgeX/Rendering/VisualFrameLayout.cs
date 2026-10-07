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
        ChartStateCategory? state = null, bool pinStateColors = false, Action<VisualSceneBuilder, ChartRect>? marker = null, SvgPaint? paint = null) {
        Label = label; Color = color; Id = id; Kind = kind; Pattern = pattern; StateRole = stateRole; SeriesKey = seriesKey;
        State = state; PinStateColors = pinStateColors; Marker = marker; Paint = paint;
    }
    internal string Label { get; }
    internal ChartColor Color { get; }
    internal string Id { get; }
    internal ChartSeriesKind? Kind { get; }
    internal ChartFillPattern Pattern { get; }
    internal ChartSeriesState StateRole { get; }
    internal string? SeriesKey { get; }
    internal ChartStateCategory? State { get; }
    internal bool PinStateColors { get; }
    internal Action<VisualSceneBuilder, ChartRect>? Marker { get; }
    internal SvgPaint? Paint { get; }
}

/// <summary>Measures and paints one common frame before any family lays out its marks.</summary>
internal static class VisualFrameLayout {
    internal static ChartRect Build(VisualSceneBuilder builder, VisualRenderContext context, IReadOnlyList<VisualLegendEntry> entries) {
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
        var titleStyle = Style(context.Frame.TitleStyle, typography.TitleSize, 600, colors.Foreground);
        var subtitleStyle = Style(context.Frame.SubtitleStyle, typography.SubtitleSize, 400, colors.MutedForeground);
        var legendStyle = Style(context.Frame.LegendStyle, typography.LegendSize, 400, colors.Foreground);
        if (!context.Frame.TransparentBackground) builder.Rect(new ChartRect(0, 0, size.Width, size.Height), colors.Background, role: "background", paint: VisualChartPaint.Fill(colors.Background, SvgColorRole.Surface));
        var left = pad.Left; var right = size.Width - pad.Right; var top = pad.Top; var bottom = size.Height - pad.Bottom;
        using (builder.PushClip(new ChartRect(left, top, right - left, bottom - top))) {
            Header(context.Frame.Title, titleStyle);
            Header(context.Frame.Subtitle, subtitleStyle);
        }
        if (context.Frame.ShowLegend && entries.Count > 0) {
            var position = context.Frame.LegendPosition;
            var side = position == ChartLegendPosition.Left || position == ChartLegendPosition.Right;
            var above = position is ChartLegendPosition.Top or ChartLegendPosition.TopLeft or ChartLegendPosition.TopRight;
            var legendWidth = side ? Math.Min((right - left) * 0.32, 180) : right - left;
            var rows = new List<List<VisualLegendEntry>>(); var row = new List<VisualLegendEntry>(); var used = 0d;
            foreach (var entry in entries) {
                var width = Math.Min(legendWidth, builder.MeasureText(OneLine(entry.Label), legendStyle).Width + 28);
                if (row.Count > 0 && (side || used + gap + width > legendWidth)) { rows.Add(row); row = new(); used = 0; }
                row.Add(entry); used += (row.Count > 1 ? gap : 0) + width;
            }
            if (row.Count > 0) rows.Add(row);
            var lineHeight = Math.Max(legendStyle.EffectiveFontSize * 1.5, builder.MeasureText("Mg", legendStyle).Height);
            var available = Math.Max(0, Math.Min(bottom - top, size.Height * context.Frame.LegendMaximumHeightFraction));
            var legendGap = Math.Min(gap, Math.Max(0, available - lineHeight));
            var maximumRows = available < lineHeight ? 0 : (int)Math.Floor((available - legendGap) / lineHeight);
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
            var height = visible * lineHeight;
            var x = position == ChartLegendPosition.Right ? right - legendWidth : left;
            var y = side || above ? top : bottom - height;
            using (builder.PushClip(new ChartRect(x, y, legendWidth, height))) {
                for (var r = 0; r < visible; r++) {
                    var rowWidth = 0d;
                    foreach (var entry in rows[r]) rowWidth += Math.Min(legendWidth, builder.MeasureText(OneLine(entry.Label), legendStyle).Width + 28) + gap;
                    rowWidth = Math.Max(0, rowWidth - gap);
                    var alignRight = position is ChartLegendPosition.TopRight or ChartLegendPosition.BottomRight;
                    var alignCenter = position is ChartLegendPosition.Top or ChartLegendPosition.Bottom;
                    var cursor = x + (alignRight ? legendWidth - rowWidth : alignCenter ? (legendWidth - rowWidth) / 2 : 0);
                    foreach (var entry in rows[r]) {
                        var width = Math.Min(legendWidth, builder.MeasureText(OneLine(entry.Label), legendStyle).Width + 28);
                        var baseline = y + r * lineHeight + builder.TextAscent(legendStyle);
                        var swatch = new ChartRect(cursor, baseline - legendStyle.EffectiveFontSize * 0.65, 10, 10);
                        using (builder.PushGroup("legend-" + entry.Id, "legend-entry", new Dictionary<string, string> {
                            ["data-cfx-series-key"] = entry.SeriesKey ?? "", ["data-cfx-state"] = entry.StateRole.ToString(),
                            ["aria-label"] = entry.Label
                        })) {
                            if (!ReferenceEquals(entry, overflow)) using (builder.PushClip(swatch)) {
                            if (entry.Marker != null) entry.Marker(builder, swatch);
                            else if (entry.State != null) {
                                using (builder.PushGroup("legend-" + entry.Id + "-swatch", "state-legend-swatch",
                                    VisualStateSceneTools.StateMetadata(entry.PinStateColors, entry.State)))
                                    VisualStateSceneTools.StateRect(builder, swatch, entry.State, colors, ChartStateCategoryLegend.SwatchRadius, "legend-swatch");
                            } else if (entry.Kind is ChartSeriesKind.Line or ChartSeriesKind.StepLine or ChartSeriesKind.TrendLine)
                                builder.Line(swatch.Left, swatch.Top + 5, swatch.Right, swatch.Top + 5, entry.Color, context.Theme.SeriesStrokeWidth, role: "legend-swatch", paint: VisualChartPaint.Stroke(entry.Paint ?? SvgPaint.Literal(entry.Color)));
                            else {
                                builder.Rect(swatch, entry.Color, role: "legend-swatch", paint: VisualChartPaint.Fill(entry.Paint ?? SvgPaint.Literal(entry.Color)));
                                if (entry.Pattern != ChartFillPattern.None) builder.Pattern(new ChartPath(new[] {
                                    ChartPathCommand.MoveTo(swatch.Left, swatch.Top), ChartPathCommand.LineTo(swatch.Right, swatch.Top),
                                    ChartPathCommand.LineTo(swatch.Right, swatch.Bottom), ChartPathCommand.LineTo(swatch.Left, swatch.Bottom)
                                }), entry.Pattern, colors.Surface, spacing: 4, strokeWidth: 1, role: "legend-pattern",
                                    paint: SvgPaint.Of(colors.Surface, SvgColorRole.Surface));
                            }
                            }
                            var label = Fit(OneLine(entry.Label), Math.Max(0, width - 22), legendStyle);
                            var anchor = cursor + 18 + (legendStyle.Alignment == TextAlignment.Center ? Math.Max(0, width - 22) / 2 : legendStyle.Alignment == TextAlignment.Right ? Math.Max(0, width - 22) : 0);
                            builder.Text(label, anchor, baseline, legendStyle, role: "legend-label", paint: VisualChartPaint.Text(legendStyle));
                        }
                        builder.AddRegion(new VisualSemanticRegion("legend-" + entry.Id, "legend", new ChartRect(cursor, y + r * lineHeight, width, lineHeight), entry.Label));
                        cursor += width + gap;
                    }
                }
            }
            if (omitted > 0) {
                var shown = new HashSet<VisualLegendEntry>();
                foreach (var visibleRow in rows) foreach (var entry in visibleRow) shown.Add(entry);
                foreach (var entry in entries) if (!shown.Contains(entry)) {
                    builder.AddRegion(new VisualSemanticRegion("legend-" + entry.Id, "legend", new ChartRect(x, y, 0, 0), entry.Label));
                    using (builder.PushGroup("legend-" + entry.Id, "legend-entry-omitted", new Dictionary<string, string> {
                        ["data-cfx-series-key"] = entry.SeriesKey ?? "", ["aria-label"] = entry.Label
                    })) { }
                }
            }
            if (height > 0) {
                if (side) { if (position == ChartLegendPosition.Left) left += legendWidth + legendGap; else right -= legendWidth + legendGap; }
                else if (above) top += height + legendGap; else bottom -= height + legendGap;
            }
        }
        if (bottom <= top || right <= left) {
            builder.AddDiagnostic(new VisualDiagnostic("frame.insufficient-space", "The frame leaves no content viewport."));
            return new ChartRect(left, Math.Min(top, size.Height - pad.Bottom), Math.Max(0, right - left), 0);
        }
        var content = new ChartRect(left, top, right - left, bottom - top);
        if (context.Frame.ShowSurface) builder.Rect(content, colors.Surface, colors.Border, radius: context.Theme.BarRadius, role: "content-surface",
            paint: new VisualScenePaintBinding(SvgPaint.Of(colors.Surface, SvgColorRole.Surface), SvgPaint.Of(colors.Border, SvgColorRole.Surface)));
        return content;

        void Header(string? text, TextStyle style) {
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
                builder.Text(line, anchor, top + builder.TextAscent(style), style, role: count == 0 ? "frame-heading" : "frame-heading-continuation", paint: VisualChartPaint.Text(style));
                top += lineHeight; count++;
                if (line.EndsWith("…", StringComparison.Ordinal)) { remainder = ""; break; }
                remainder = remainder.Substring(Math.Min(remainder.Length, line.Length)).TrimStart();
            }
            if (remainder.Length > 0 || paragraphs.Count > 0) builder.AddDiagnostic(new VisualDiagnostic("frame.heading-overflow", "The heading exceeds the available frame space."));
            top += gap;
        }
        string Fit(string text, double width, TextStyle style, bool ellipsis = true) {
            if (builder.MeasureText(text, style).Width <= width) return text;
            var elements = StringInfo.ParseCombiningCharacters(text);
            var count = elements.Length;
            var length = text.Length;
            while (count > 0 && builder.MeasureText(text.Substring(0, length) + (ellipsis ? "…" : ""), style).Width > width) {
                count--; length = count == 0 ? 0 : elements[count];
            }
            if (ellipsis) {
                builder.AddDiagnostic(new VisualDiagnostic("frame.text-truncated", "Frame text was shortened; its complete value remains in semantic metadata."));
                return length == 0 ? "" : text.Substring(0, length) + "…";
            }
            var space = length > 0 ? text.LastIndexOf(' ', length - 1, length) : -1;
            return text.Substring(0, space > 0 ? space : length);
        }
    }
    private static string OneLine(string text) => text.Replace('\r', ' ').Replace('\n', ' ');
}
