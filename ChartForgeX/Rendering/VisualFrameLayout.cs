using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal sealed class VisualLegendEntry {
    internal VisualLegendEntry(string label, ChartColor color, string id, ChartSeriesKind? kind = null,
        ChartFillPattern pattern = ChartFillPattern.None, ChartSeriesState stateRole = ChartSeriesState.None, string? seriesKey = null,
        ChartStateCategory? state = null, bool pinStateColors = false, Action<VisualSceneBuilder, ChartRect, VisualRenderContext>? marker = null, SvgPaint? paint = null, string? value = null, string? percentage = null,
        string? targetKind = null, string? targetId = null, IReadOnlyDictionary<string, string>? metadata = null) {
        Label = label; Color = color; Id = id; Kind = kind; Pattern = pattern; StateRole = stateRole; SeriesKey = seriesKey;
        State = state; PinStateColors = pinStateColors; Marker = marker; Paint = paint; Value = value; Percentage = percentage;
        TargetKind = targetKind; TargetId = targetId;
        if (metadata != null) {
            var snapshot = new Dictionary<string, string>();
            foreach (var item in metadata) snapshot.Add(item.Key, item.Value);
            Metadata = snapshot;
        }
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
    // A legend describes a native target independently of its own rendered legend identity.
    internal string? TargetKind { get; }
    internal string? TargetId { get; }
    internal IReadOnlyDictionary<string, string>? Metadata { get; }

    internal IReadOnlyDictionary<string, string> SceneMetadata() {
        var result = new Dictionary<string, string> {
            ["data-cfx-series-key"] = SeriesKey ?? "", ["data-cfx-state"] = StateRole.ToString(), ["aria-label"] = Description
        };
        if (Metadata != null) foreach (var item in Metadata) result[item.Key] = item.Value;
        if (TargetKind != null && TargetId != null) {
            result["data-cfx-legend-target-kind"] = TargetKind;
            result["data-cfx-legend-target-id"] = TargetId;
            result["data-cfx-label"] = Label;
        }
        return result;
    }
}

/// <summary>Measures and paints one common frame before any family lays out its marks.</summary>
internal static class VisualFrameLayout {
    /// <summary>Paints existing legend identities after family surfaces inside an owner-measured empty slot.</summary>
    internal static void PaintOverlayLegend(VisualSceneBuilder builder, VisualRenderContext context,
        IReadOnlyList<VisualLegendEntry> entries, ChartRect bounds) {
        var size = context.Layout.Size;
        var layout = new VisualLayoutOptions(size, new ChartPadding(bounds.Left, bounds.Top, size.Width - bounds.Right, size.Height - bounds.Bottom));
        var frame = new VisualFrame("", "", showLegend: true, legendPosition: ChartLegendPosition.TopLeft,
            transparentBackground: true, legendStyle: context.Frame.LegendStyle,
            legendMaximumRows: context.Frame.LegendMaximumRows, legendMaximumHeightFraction: 1);
        Build(builder, new VisualRenderContext(layout, context.Theme, context.ThemeMode, frame, context.Font), entries);
    }

    internal static ChartRect Build(VisualSceneBuilder builder, VisualRenderContext context, IReadOnlyList<VisualLegendEntry> entries, VisualFramePaints? paints = null) {
        return Build(builder, context, entries, out _, paints);
    }

    internal static ChartRect Build(VisualSceneBuilder builder, VisualRenderContext context, IReadOnlyList<VisualLegendEntry> entries,
        out double legendHeight, VisualFramePaints? paints = null) {
        legendHeight = 0;
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
        // Design rule 7: title 16/600 primary; subtitle 13 secondary; legends 12 secondary like axis ticks.
        var titleStyle = Style(context.Frame.TitleStyle, typography.TitleSize, 600, colors.Foreground);
        var subtitleStyle = Style(context.Frame.SubtitleStyle, typography.SubtitleSize, 400, colors.MutedForeground);
        var legendStyle = Style(context.Frame.LegendStyle, typography.LegendSize, 400, colors.MutedForeground);
        if (!context.Frame.TransparentBackground) builder.Rect(new ChartRect(0, 0, size.Width, size.Height), colors.Background, role: "background", paint: VisualChartPaint.Fill(paints?.Background ?? SvgPaint.Of(colors.Background, SvgColorRole.Surface)));
        if (context.Frame.ShowCard) VisualFrameSurface.Paint(builder, context, colors, paints);
        var left = pad.Left; var right = size.Width - pad.Right; var top = pad.Top; var bottom = size.Height - pad.Bottom;
        // Preserve requested sizes when each heading can contribute a line to the frame.
        // Otherwise fit cloned styles into a bounded share, retaining their title/subtitle ratio.
        double HeadingHeight(string? text, TextStyle style) => string.IsNullOrEmpty(text) ? 0 : 2 * Math.Max(style.EffectiveFontSize * 1.3, builder.MeasureText("Mg", style).Height);
        var headingHeight = HeadingHeight(context.Frame.Title, titleStyle) + HeadingHeight(context.Frame.Subtitle, subtitleStyle);
        var headingGaps = headingHeight > 0 ? gap * 5 / 6 + (!string.IsNullOrEmpty(context.Frame.Title) && !string.IsNullOrEmpty(context.Frame.Subtitle) ? gap / 6 : 0) : 0;
        // Preserve the theme's role hierarchy: a normal dashboard title is larger
        // than its body text. Global font enlargement scales both heading roles.
        var ordinaryScale = VisualTextFitting.MaximumOrdinaryFontSize / typography.DataLabelSize;
        var nativeOrdinaryScale = VisualTextFitting.MaximumOrdinaryFontSize / ResolvedVisualTheme.DefaultDataLabelFontSize;
        var oversizedTitle = titleStyle.EffectiveFontSize > Math.Max(typography.TitleSize * ordinaryScale,
            ResolvedVisualTheme.DefaultTitleFontSize * nativeOrdinaryScale);
        var oversizedSubtitle = subtitleStyle.EffectiveFontSize > Math.Max(typography.SubtitleSize * ordinaryScale,
            ResolvedVisualTheme.DefaultSubtitleFontSize * nativeOrdinaryScale);
        var oversizedHeadings = oversizedTitle || oversizedSubtitle;
        var coupledOversizedHeadings = (string.IsNullOrEmpty(context.Frame.Title) || oversizedTitle)
            && (string.IsNullOrEmpty(context.Frame.Subtitle) || oversizedSubtitle);
        // Normal headings retain bounded wrapping. Fit full text only when even a short
        // font sample cannot fit the line, independently of an ordinary companion heading.
        bool OversizedHeading(TextStyle style) => builder.MeasureText("Mg", style).Width > Math.Max(1, right - left);
        double HeadingWidthScale(string? text, TextStyle style) {
            if (string.IsNullOrEmpty(text) || !OversizedHeading(style)) return 1;
            var fitted = VisualTextFitting.FitOversized(builder, style, double.MaxValue,
                candidate => builder.MeasureText(text!, candidate).Width <= Math.Max(1, right - left));
            return fitted.EffectiveFontSize / style.EffectiveFontSize;
        }
        var headingWidthScale = Math.Min(HeadingWidthScale(context.Frame.Title, titleStyle),
            HeadingWidthScale(context.Frame.Subtitle, subtitleStyle));
        // Account for width fitting before deciding whether the shared height needs
        // reducing; a wide title alone must not unnecessarily shrink an ordinary subtitle.
        var fittedHeadingHeight = HeadingHeight(context.Frame.Title, titleStyle) * (OversizedHeading(titleStyle) ? headingWidthScale : 1)
            + HeadingHeight(context.Frame.Subtitle, subtitleStyle) * (OversizedHeading(subtitleStyle) ? headingWidthScale : 1);
        var headingScale = oversizedHeadings && (fittedHeadingHeight / 2 + headingGaps >= bottom - top
            || coupledOversizedHeadings && fittedHeadingHeight + headingGaps > (bottom - top) * .4)
            ? Math.Min(1, Math.Max(1, (bottom - top) * .4 - headingGaps) / fittedHeadingHeight) : 1;
        using (builder.PushClip(new ChartRect(left, top, right - left, bottom - top))) {
            var headingTop = top;
            Header(context.Frame.Title, titleStyle, paints?.Foreground);
            if (top > headingTop && !string.IsNullOrEmpty(context.Frame.Subtitle)) top += gap / 6;
            Header(context.Frame.Subtitle, subtitleStyle, paints?.MutedForeground);
            if (top > headingTop) top += gap * 5 / 6;
        }
        if (context.Frame.ShowLegend && entries.Count > 0) {
            Dictionary<VisualLegendEntry, (string Text, double Value, double Percentage, double Gap)>? numericWidths = null;
            var position = context.Frame.LegendPosition;
            var side = position == ChartLegendPosition.Left || position == ChartLegendPosition.Right;
            var above = position is ChartLegendPosition.Top or ChartLegendPosition.TopLeft or ChartLegendPosition.TopRight;
            var legendWidth = side ? Math.Min((right - left) * 0.32, 180) : right - left;
            var available = Math.Max(0, Math.Min(bottom - top, size.Height * context.Frame.LegendMaximumHeightFraction));
            var requestedLineHeight = Math.Max(legendStyle.EffectiveFontSize * 1.5, builder.MeasureText("Mg", legendStyle).Height);
            double LegendWidthScale(TextStyle candidate) {
                var scale = 1d;
                foreach (var entry in entries) {
                    var fixedWidth = Overhead(entry) + (string.IsNullOrEmpty(entry.Value) ? 0 : gap)
                        + (string.IsNullOrEmpty(entry.Percentage) ? 0 : gap * 2 / 3);
                    var textWidth = builder.MeasureText(OneLine(entry.Label), candidate).Width
                        + builder.MeasureText(entry.Value ?? string.Empty, candidate).Width
                        + builder.MeasureText(entry.Percentage ?? string.Empty, candidate).Width;
                    if (textWidth > 0) scale = Math.Min(scale, Math.Max(1, legendWidth - fixedWidth) / textWidth);
                }
                return scale;
            }
            if (legendStyle.EffectiveFontSize > VisualTextFitting.MaximumOrdinaryFontSize && available > 0 &&
                (requestedLineHeight > available || LegendWidthScale(legendStyle) < 1)) {
                // Keep ordinary sizes and overflow behavior. Oversized text still contributes
                // visible entries, with an 18px line floor before the normal row budget applies.
                var targetRows = Math.Max(1, Math.Min(entries.Count, context.Frame.LegendMaximumRows ?? entries.Count));
                var fittedLineHeight = Math.Max(Math.Min(18, available), (available - Math.Min(gap, available / 4)) / targetRows);
                legendStyle = legendStyle.Clone();
                legendStyle.FontSize *= Math.Min(1, fittedLineHeight / requestedLineHeight);
                var widthScale = LegendWidthScale(legendStyle);
                legendStyle.FontSize = Math.Max(Math.Min(12, legendStyle.FontSize), legendStyle.FontSize * widthScale);
            }
            var columnWidth = 0d; var groupedValueList = false;
            List<List<VisualLegendEntry>> PlanRows() {
                var planned = new List<List<VisualLegendEntry>>(); var row = new List<VisualLegendEntry>(); var used = 0d;
                foreach (var entry in entries) {
                    var width = EntryWidth(entry, false);
                    if (row.Count > 0 && (side || used + gap + width > legendWidth)) { planned.Add(row); row = new(); used = 0; }
                    row.Add(entry); used += (row.Count > 1 ? gap : 0) + width;
                }
                if (row.Count > 0) planned.Add(row);
                return planned;
            }
            var rows = PlanRows();
            // Legend v2: an automatic legend keeps at most two wrapped rows. A wide frame moves a longer legend, and a
            // part-to-whole value list beside a wide plot, to the right; a compact frame lists the entries in two columns.
            if (context.Frame.AutomaticLegendPlacement && !side && context.Frame.LegendMaximumRows == null && entries.Count > 1) {
                var partToWhole = entries.All(entry => !string.IsNullOrEmpty(entry.Value));
                var wideFrame = size.Width >= LegendCompactFrameWidth;
                var widePlot = bottom - top > 0 && (right - left) / (bottom - top) > LegendSideAspect;
                if (wideFrame && (rows.Count > LegendMaximumAutomaticRows || partToWhole && widePlot)) {
                    position = ChartLegendPosition.Right; side = true; above = false;
                    var natural = entries.Max(entry => LegendWidth(entry, Overhead(entry), double.PositiveInfinity));
                    legendWidth = Math.Min(natural, (right - left) * LegendSideMaximumShare);
                    available = Math.Max(0, bottom - top);
                    // One column: every entry spans it, so values and percentages align.
                    columnWidth = legendWidth;
                    numericWidths = null;
                    groupedValueList = partToWhole;
                    rows = PlanRows();
                } else if (!wideFrame && (rows.Count > LegendMaximumAutomaticRows || partToWhole && rows.Count > 1)) {
                    columnWidth = Math.Max(0, (legendWidth - gap) / 2);
                    rows = PlanRows();
                }
            }
            var lineHeight = Math.Max(legendStyle.EffectiveFontSize * 1.5, builder.MeasureText("Mg", legendStyle).Height);
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
            if (height > 0) legendHeight = height + legendGap;
            var x = position == ChartLegendPosition.Right ? right - legendWidth : left;
            var y = side || above ? top : bottom - height;
            // A part-to-whole value list beside its pie reads as one group: a square plot and the list, a fixed gap apart,
            // centred together in the frame, with the list centred vertically against the pie.
            var groupLeft = 0d; var groupPlot = 0d;
            if (groupedValueList && height > 0) {
                var plotHeight = Math.Max(0, bottom - top);
                var groupGap = Math.Max(0, LegendValueListGap - gap);
                groupPlot = Math.Max(0, Math.Min(plotHeight, right - left - legendWidth - groupGap));
                // The pie paints inside its plot by the theme spacing, so centre what is visible: pie edge to list edge.
                var visibleWidth = groupPlot - gap + groupGap + legendWidth;
                groupLeft = Math.Max(left, left + (right - left - visibleWidth) / 2 - gap);
                x = groupLeft + groupPlot + groupGap;
                y = top + Math.Max(0, (plotHeight - height) / 2);
            }
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
                    foreach (var entry in rows[r]) rowWidth += EntryWidth(entry, ReferenceEquals(entry, overflow)) + gap;
                    rowWidth = Math.Max(0, rowWidth - gap);
                    var alignRight = position is ChartLegendPosition.TopRight or ChartLegendPosition.BottomRight;
                    var alignCenter = position is ChartLegendPosition.Top or ChartLegendPosition.Bottom;
                    var cursor = x + (alignRight ? legendWidth - rowWidth : alignCenter ? (legendWidth - rowWidth) / 2 : 0);
                    foreach (var entry in rows[r]) {
                        var isSummary = ReferenceEquals(entry, overflow);
                        var width = EntryWidth(entry, isSummary);
                        var baseline = y + titleHeight + r * lineHeight + builder.TextAscent(legendStyle);
                        var glyph = Glyph(entry);
                        var swatch = new ChartRect(cursor, baseline - legendStyle.EffectiveFontSize * 0.65, glyph == LegendGlyph.Line ? LegendLineWidth : LegendSwatchSize, LegendSwatchSize);
                        using (builder.PushGroup("legend-" + entry.Id, "legend-entry", entry.SceneMetadata())) {
                            if (!ReferenceEquals(entry, overflow)) using (builder.PushClip(swatch)) {
                            if (entry.Marker != null) entry.Marker(builder, swatch, context);
                            else if (entry.State != null) {
                                using (builder.PushGroup("legend-" + entry.Id + "-swatch", "state-legend-swatch",
                                    VisualStateSceneTools.StateMetadata(entry.PinStateColors, entry.State)))
                                    VisualStateSceneTools.StateRect(builder, swatch, entry.State, colors, ChartStateCategoryLegend.SwatchRadius, "legend-swatch");
                            } else if (glyph == LegendGlyph.Line)
                                builder.Line(swatch.Left, swatch.Top + swatch.Height / 2, swatch.Right, swatch.Top + swatch.Height / 2, entry.Color, LegendLineStroke, role: "legend-swatch", paint: VisualChartPaint.Stroke(entry.Paint ?? SvgPaint.Literal(entry.Color)));
                            else if (glyph == LegendGlyph.Dot)
                                builder.Ellipse(swatch.Left + swatch.Width / 2, swatch.Top + swatch.Height / 2, LegendDotDiameter / 2, LegendDotDiameter / 2, entry.Color, role: "legend-swatch", paint: VisualChartPaint.Fill(entry.Paint ?? SvgPaint.Literal(entry.Color)));
                            else {
                                builder.Rect(swatch, entry.Color, radius: LegendSwatchRadius, role: "legend-swatch", paint: VisualChartPaint.Fill(entry.Paint ?? SvgPaint.Literal(entry.Color)));
                                DrawLegendPattern(builder, swatch, entry.Pattern, context);
                            }
                            }
                            var fullLabel = OneLine(entry.Label);
                            // The summary has no swatch. Use that space for its count, and shorten the wording before
                            // trimming so a compact side legend still explains that more entries are available.
                            if (isSummary && builder.MeasureText(fullLabel, legendStyle).Width > width)
                                fullLabel = "+ " + omitted.ToString(CultureInfo.InvariantCulture) + " more";
                            var valueWidth = ValueWidth(entry);
                            var labelOffset = isSummary ? 0 : swatch.Width + LegendLabelGap;
                            var labelWidth = Math.Max(0, width - (isSummary ? 0 : labelOffset + LegendTrailingSlack) - (valueWidth > 0 ? valueWidth + gap : 0));
                            var label = Fit(fullLabel, labelWidth, legendStyle);
                            var anchor = cursor + labelOffset + (legendStyle.Alignment == TextAlignment.Center ? labelWidth / 2 : legendStyle.Alignment == TextAlignment.Right ? labelWidth : 0);
                            builder.Text(label, anchor, baseline, legendStyle, role: "legend-label", paint: paints?.Foreground ?? VisualChartPaint.Text(legendStyle));
                            if (valueWidth > 0) {
                                var valueStyle = legendStyle.Clone(); valueStyle.Alignment = TextAlignment.Right;
                                var numeric = NumericWidths(entry);
                                var percentageWidth = numeric.Percentage;
                                var valueRight = cursor + width - 4;
                                if (percentageWidth > 0) {
                                    var percentageStyle = valueStyle.Clone(); percentageStyle.Color = colors.MutedForeground;
                                    builder.Text(entry.Percentage!, valueRight, baseline, percentageStyle, role: "legend-percentage", paint: paints?.MutedForeground ?? VisualChartPaint.Text(percentageStyle));
                                    valueRight -= percentageWidth + numeric.Gap;
                                }
                                builder.Text(numeric.Text, valueRight, baseline, valueStyle, role: "legend-value", paint: paints?.Foreground ?? VisualChartPaint.Text(valueStyle));
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
                    using (builder.PushGroup("legend-" + entry.Id, "legend-entry-omitted", entry.SceneMetadata())) { }
                }
            }
            if (height > 0) {
                if (groupedValueList) { left = groupLeft; right = groupLeft + groupPlot; }
                else if (side) { if (position == ChartLegendPosition.Left) left += legendWidth + legendGap; else right -= legendWidth + legendGap; }
                else if (above) top += height + legendGap; else bottom -= height + legendGap;
            }
            double LegendWidth(VisualLegendEntry entry, double overhead, double? limit = null) {
                var bound = limit ?? legendWidth;
                var valueWidth = ValueWidth(entry);
                if (valueWidth > 0) valueWidth += gap;
                var label = OneLine(entry.Label); var availableWidth = Math.Max(0, bound - overhead - valueWidth);
                var length = ChartTextFitting.PrefixLength(label, availableWidth, text => builder.MeasureText(text, legendStyle).Width);
                return length == label.Length ? Math.Min(bound, builder.MeasureText(label, legendStyle).Width + overhead + valueWidth) : bound;
            }
            // A two-column list gives every entry the same column so names, values and percentages line up.
            double EntryWidth(VisualLegendEntry entry, bool summary) =>
                summary ? LegendWidth(entry, 0) : columnWidth > 0 ? columnWidth : LegendWidth(entry, Overhead(entry));
            double ValueWidth(VisualLegendEntry entry) {
                if (string.IsNullOrEmpty(entry.Value)) return 0;
                var widths = NumericWidths(entry);
                return widths.Value + (string.IsNullOrEmpty(entry.Percentage) ? 0 : widths.Percentage + widths.Gap);
            }
            (string Text, double Value, double Percentage, double Gap) NumericWidths(VisualLegendEntry entry) {
                if (string.IsNullOrEmpty(entry.Value)) return default;
                numericWidths ??= new Dictionary<VisualLegendEntry, (string Text, double Value, double Percentage, double Gap)>();
                if (!numericWidths.TryGetValue(entry, out var widths)) {
                    var overflowValue = entry.Metadata != null && entry.Metadata.TryGetValue("data-cfx-value-overflow", out var overflowFlag)
                        && overflowFlag == "true";
                    // An overflow bound is longer than an ordinary value. Its browser-selected fallback
                    // face can consume a small advance-only gap, so keep a full line of clearance.
                    var numericGap = overflowValue ? Math.Max(gap * 2 / 3, legendStyle.EffectiveFontSize * 1.5) : gap * 2 / 3;
                    var percentageWidth = string.IsNullOrEmpty(entry.Percentage) ? 0 : builder.MeasureText(entry.Percentage!, legendStyle).Width;
                    var textWidth = Math.Max(0, (columnWidth > 0 ? columnWidth : legendWidth) - Overhead(entry) - gap - percentageWidth - (percentageWidth > 0 ? numericGap : 0));
                    var labelWidth = builder.MeasureText(OneLine(entry.Label), legendStyle).Width;
                    // Reserve a readable label before fitting the value. A long authored numeric caption
                    // must not erase the category or paint through its separately allocated column.
                    var labelReserve = Math.Min(labelWidth, Math.Max(builder.MeasureText(ShortenedMinimum(OneLine(entry.Label)), legendStyle).Width, textWidth / 3));
                    var valueMaximum = Math.Max(0, textWidth - labelReserve);
                    var value = entry.Value!;
                    if (builder.MeasureText(value, legendStyle).Width > valueMaximum) {
                        if (overflowValue) {
                            value = string.Empty;
                            foreach (var candidate in new[] { "> 1.79E308", ">1E308" })
                                if (builder.MeasureText(candidate, legendStyle).Width <= valueMaximum) { value = candidate; break; }
                            builder.AddDiagnostic(new VisualDiagnostic("frame.legend-value-compacted", "An overflow bound was shortened or omitted to fit its legend column; its complete bound remains in semantic metadata."));
                        } else value = Fit(value, valueMaximum, legendStyle);
                    }
                    // Wrapping, row alignment and painting share these resolved text allocations.
                    widths = (value, builder.MeasureText(value, legendStyle).Width, percentageWidth, numericGap);
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
        if (context.Frame.ShowSurface) builder.Rect(content, colors.Surface, colors.PlotBorder, radius: context.Theme.PlotRadius, role: "content-surface",
            paint: new VisualScenePaintBinding(paints?.Surface ?? SvgPaint.Of(colors.Surface, SvgColorRole.Surface), paints?.PlotBorder ?? (colors.PlotBorder.Equals(colors.Border) ? paints?.Border : null) ?? SvgPaint.Of(colors.PlotBorder, SvgColorRole.Surface)));
        return content;

        void Header(string? text, TextStyle style, SvgPaint? paint) {
            if (string.IsNullOrEmpty(text)) return;
            // Keep the authored request separate from the face-dependent fitted paint size.
            using var fontRequest = builder.PushGroup(null, "frame-heading-source", new Dictionary<string, string> {
                ["data-cfx-authored-font-request"] = string.Join("|", style.Font.Family,
                    style.Font.Weight.ToString(CultureInfo.InvariantCulture), style.Font.Italic ? "italic" : "normal",
                    style.EffectiveFontSize.ToString("R", CultureInfo.InvariantCulture), style.Font.Variations.Key)
            });
            var scale = (OversizedHeading(style) ? headingWidthScale : 1) * headingScale;
            if (scale < 1) { style = style.Clone(); style.FontSize *= scale; }
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
            var fitted = ChartTextFitting.FitEnd(text, width, value => builder.MeasureText(value, style).Width, ellipsis ? "…" : "", ChartTextFitting.MinimumEllipsizedTextElements);
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

    /// <summary>The shortest displayed form of a label that still identifies it: its first six text elements and an ellipsis.</summary>
    private static string ShortenedMinimum(string label) {
        var end = 0;
        for (var count = 0; end < label.Length && count < ChartTextFitting.MinimumEllipsizedTextElements; count++) end = TextElementBoundary.Next(label, end);
        return end >= label.Length ? label : label.Substring(0, end) + "…";
    }

    /// <summary>Legend v2 glyphs match their mark: a short line for stroked series, a dot for point clouds, a rounded square for filled marks.</summary>
    internal enum LegendGlyph { Square, Line, Dot }

    internal const double LegendSwatchSize = 10;
    internal const double LegendSwatchRadius = 2;
    internal const double LegendLineWidth = 14;
    internal const double LegendLineStroke = 2;
    internal const double LegendDotDiameter = 8;
    internal const double LegendLineMarkerDiameter = 6;
    internal const double LegendLabelGap = 6;
    private const double LegendTrailingSlack = 4;
    private const double LegendTrailingSpace = 10;
    /// <summary>An automatic top or bottom legend keeps at most this many wrapped rows before it moves or forms columns.</summary>
    internal const int LegendMaximumAutomaticRows = 2;
    /// <summary>Frames narrower than this are compact: their legend forms two columns instead of moving beside the plot.</summary>
    internal const double LegendCompactFrameWidth = 480;
    /// <summary>A part-to-whole value list moves beside a plot wider than this aspect ratio.</summary>
    internal const double LegendSideAspect = 1.4;
    private const double LegendSideMaximumShare = .36;
    /// <summary>The visible gap between a pie and its value list; the pie's own inset (the theme spacing) counts toward it.</summary>
    internal const double LegendValueListGap = 32;

    internal static LegendGlyph Glyph(VisualLegendEntry entry) => entry.State != null ? LegendGlyph.Square : entry.Kind switch {
        ChartSeriesKind.Line or ChartSeriesKind.StepLine or ChartSeriesKind.TrendLine or ChartSeriesKind.Slope
            or ChartSeriesKind.Radar or ChartSeriesKind.Polar => LegendGlyph.Line,
        ChartSeriesKind.Scatter or ChartSeriesKind.Bubble => LegendGlyph.Dot,
        _ => LegendGlyph.Square
    };

    /// <summary>The fixed width an entry needs besides its text: glyph, glyph-to-label gap and trailing space.</summary>
    private static double Overhead(VisualLegendEntry entry) =>
        (Glyph(entry) == LegendGlyph.Line ? LegendLineWidth : LegendSwatchSize) + LegendLabelGap + LegendTrailingSpace;
}
