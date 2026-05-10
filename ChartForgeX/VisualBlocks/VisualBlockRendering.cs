using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.VisualBlocks;

internal static class VisualBlockRendering {
    public const int MaximumScheduleTicks = 512;
    public const int MaximumScheduleLanes = 256;
    public const int MaximumTableMicroVisualPoints = 512;

    public static void Validate(IVisualBlock block) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        if (block is ChartTable table) {
            if (table.Columns.Count == 0) throw new InvalidOperationException("Chart tables must contain at least one column.");
            foreach (var row in table.Rows) {
                if (row.Cells.Count != table.Columns.Count) throw new InvalidOperationException("Chart table rows must match the column count.");
                foreach (var cell in row.Cells) ValidateTableCellMicroVisual(cell);
            }

            return;
        }

        if (block is ChartList list) {
            if (list.Items.Count == 0) throw new InvalidOperationException("Chart lists must contain at least one item.");
            return;
        }

        if (block is MetricCard card) {
            if (card.Label.Length == 0) throw new InvalidOperationException("Metric cards must define a label.");
            if (card.Value.Length == 0) throw new InvalidOperationException("Metric cards must define a value.");
            if (card.Unit.Length > 24) throw new InvalidOperationException("Metric card units must be twenty-four characters or fewer.");
            if (card.Symbol.Length > 12) throw new InvalidOperationException("Metric card symbols must be twelve characters or fewer.");
            foreach (var detail in card.Details) {
                if (detail.Label.Length == 0) throw new InvalidOperationException("Metric card details must define a label.");
                if (detail.Value.Length == 0) throw new InvalidOperationException("Metric card details must define a value.");
            }

            if (card.ActionLabel.Length > 48) throw new InvalidOperationException("Metric card action labels must be forty-eight characters or fewer.");
            if (card.ActionSymbol.Length > 4) throw new InvalidOperationException("Metric card action symbols must be four characters or fewer.");
            if (card.ActionUrl.Length > 0 && !IsSafeActionUrl(card.ActionUrl)) throw new InvalidOperationException("Metric card action URLs must be relative URLs, http(s), or mailto links.");
            if (card.MiniBarMinimum.HasValue && card.MiniBarMaximum.HasValue && card.MiniBarMaximum.Value <= card.MiniBarMinimum.Value) throw new InvalidOperationException("Metric card mini bar maximum must be greater than minimum.");
            if (card.MiniBarHighlightIndex.HasValue && card.MiniBarHighlightIndex.Value >= card.MiniBars.Count) throw new InvalidOperationException("Metric card mini bar highlight index must reference an existing mini bar.");
            if (card.MiniSparklineMinimum.HasValue && card.MiniSparklineMaximum.HasValue && card.MiniSparklineMaximum.Value <= card.MiniSparklineMinimum.Value) throw new InvalidOperationException("Metric card mini sparkline maximum must be greater than minimum.");
            if (card.SecondaryMiniSparkline.Count > 0 && card.SecondaryMiniSparkline.Count != card.MiniSparkline.Count) throw new InvalidOperationException("Metric card secondary mini sparklines must match the primary sparkline count.");
            return;
        }

        if (block is RadialMetricCard radialCard) {
            if (radialCard.Label.Length == 0) throw new InvalidOperationException("Radial metric cards must define a label.");
            if (radialCard.Value.Length == 0) throw new InvalidOperationException("Radial metric cards must define a value.");
            if (radialCard.Layers.Count == 0) throw new InvalidOperationException("Radial metric cards must contain at least one radial layer.");
            foreach (var layer in radialCard.Layers) if (layer.Maximum <= layer.Minimum) throw new InvalidOperationException("Radial metric card layer maximum must be greater than minimum.");
            return;
        }

        if (block is SegmentedProgressCard segmentedCard) {
            if (segmentedCard.Rows.Count == 0) throw new InvalidOperationException("Segmented progress cards must contain at least one row.");
            if (segmentedCard.HeaderSymbol.Length > 4) throw new InvalidOperationException("Segmented progress card header symbols must be four characters or fewer.");
            if (segmentedCard.ActionLabel.Length > 48) throw new InvalidOperationException("Segmented progress card action labels must be forty-eight characters or fewer.");
            if (segmentedCard.ActionSymbol.Length > 4) throw new InvalidOperationException("Segmented progress card action symbols must be four characters or fewer.");
            if (segmentedCard.ActionUrl.Length > 0 && !IsSafeActionUrl(segmentedCard.ActionUrl)) throw new InvalidOperationException("Segmented progress card action URLs must be relative URLs, http(s), or mailto links.");
            foreach (var row in segmentedCard.Rows) {
                if (row.Label.Length == 0) throw new InvalidOperationException("Segmented progress rows must define a label.");
                if (!IsFinite(row.Value)) throw new InvalidOperationException("Segmented progress row values must be finite.");
                if (!IsFinite(row.Maximum) || row.Maximum <= 0) throw new InvalidOperationException("Segmented progress row maximum values must be greater than zero.");
                if (row.Value < 0) throw new InvalidOperationException("Segmented progress row values must be zero or greater.");
                if (row.Segments < 1 || row.Segments > 120) throw new InvalidOperationException("Segmented progress row segment counts must be between one and one hundred twenty.");
            }

            return;
        }

        if (block is CompositionStatusCard compositionCard) {
            if (compositionCard.Label.Length == 0) throw new InvalidOperationException("Composition status cards must define a label.");
            if (compositionCard.Value.Length == 0) throw new InvalidOperationException("Composition status cards must define a value.");
            if (compositionCard.Segments.Count == 0) throw new InvalidOperationException("Composition status cards must contain at least one segment.");
            if (compositionCard.ActionLabel.Length > 48) throw new InvalidOperationException("Composition status card action labels must be forty-eight characters or fewer.");
            if (compositionCard.ActionSymbol.Length > 4) throw new InvalidOperationException("Composition status card action symbols must be four characters or fewer.");
            if (compositionCard.ActionUrl.Length > 0 && !IsSafeActionUrl(compositionCard.ActionUrl)) throw new InvalidOperationException("Composition status card action URLs must be relative URLs, http(s), or mailto links.");
            foreach (var segment in compositionCard.Segments) {
                if (segment.Label.Length == 0) throw new InvalidOperationException("Composition status card segments must define a label.");
                if (!IsFinite(segment.Value)) throw new InvalidOperationException("Composition status card segment values must be finite.");
                if (segment.Value < 0) throw new InvalidOperationException("Composition status card segment values must be zero or greater.");
            }

            if (CompositionTotal(compositionCard) <= 0) throw new InvalidOperationException("Composition status card segments must include at least one positive value.");
            return;
        }

        if (block is DistributionStripCard distributionCard) {
            if (distributionCard.Label.Length == 0) throw new InvalidOperationException("Distribution strip cards must define a label.");
            if (distributionCard.Value.Length == 0) throw new InvalidOperationException("Distribution strip cards must define a value.");
            if (distributionCard.Caption.Length > 64) throw new InvalidOperationException("Distribution strip card captions must be sixty-four characters or fewer.");
            if (distributionCard.Segments.Count == 0) throw new InvalidOperationException("Distribution strip cards must contain at least one segment.");
            if (distributionCard.ActionLabel.Length > 48) throw new InvalidOperationException("Distribution strip card action labels must be forty-eight characters or fewer.");
            if (distributionCard.ActionSymbol.Length > 4) throw new InvalidOperationException("Distribution strip card action symbols must be four characters or fewer.");
            if (distributionCard.ActionUrl.Length > 0 && !IsSafeActionUrl(distributionCard.ActionUrl)) throw new InvalidOperationException("Distribution strip card action URLs must be relative URLs, http(s), or mailto links.");
            foreach (var segment in distributionCard.Segments) {
                if (segment.Label.Length == 0) throw new InvalidOperationException("Distribution strip segments must define a label.");
                if (!IsFinite(segment.Value)) throw new InvalidOperationException("Distribution strip segment values must be finite.");
                if (segment.Value < 0) throw new InvalidOperationException("Distribution strip segment values must be zero or greater.");
                if (segment.Symbol.Length > 8) throw new InvalidOperationException("Distribution strip segment symbols must be eight characters or fewer.");
                if (segment.Detail.Length > 36) throw new InvalidOperationException("Distribution strip segment details must be thirty-six characters or fewer.");
            }

            if (DistributionTotal(distributionCard) <= 0) throw new InvalidOperationException("Distribution strip segments must include at least one positive value.");
            return;
        }

        if (block is HeatmapInsightCard heatmapCard) {
            if (heatmapCard.Columns.Count == 0) throw new InvalidOperationException("Heatmap insight cards must contain at least one column.");
            if (heatmapCard.Rows.Count == 0) throw new InvalidOperationException("Heatmap insight cards must contain at least one row.");
            if (!IsFinite(heatmapCard.Minimum) || !IsFinite(heatmapCard.Maximum) || heatmapCard.Maximum <= heatmapCard.Minimum) throw new InvalidOperationException("Heatmap insight color key maximum must be greater than minimum.");
            foreach (var row in heatmapCard.Rows) {
                if (row.Label.Length == 0) throw new InvalidOperationException("Heatmap insight rows must define a label.");
                if (row.Values.Count != heatmapCard.Columns.Count) throw new InvalidOperationException("Heatmap insight rows must match the column count.");
                foreach (var value in row.Values) if (!IsFinite(value)) throw new InvalidOperationException("Heatmap insight values must be finite.");
            }

            foreach (var item in heatmapCard.Insights) {
                if (item.Label.Length == 0) throw new InvalidOperationException("Heatmap insight items must define a label.");
                if (item.Detail.Length == 0) throw new InvalidOperationException("Heatmap insight items must define detail text.");
            }

            return;
        }

        if (block is WorkloadListBlock workloadBlock) {
            if (workloadBlock.Rows.Count == 0) throw new InvalidOperationException("Workload list blocks must contain at least one row.");
            if (workloadBlock.ActionLabel.Length > 48) throw new InvalidOperationException("Workload list block action labels must be forty-eight characters or fewer.");
            if (workloadBlock.ActionSymbol.Length > 4) throw new InvalidOperationException("Workload list block action symbols must be four characters or fewer.");
            if (workloadBlock.ActionUrl.Length > 0 && !IsSafeActionUrl(workloadBlock.ActionUrl)) throw new InvalidOperationException("Workload list block action URLs must be relative URLs, http(s), or mailto links.");
            foreach (var row in workloadBlock.Rows) {
                if (row.Label.Length == 0) throw new InvalidOperationException("Workload list rows must define a label.");
                if (!IsFinite(row.Value)) throw new InvalidOperationException("Workload list row values must be finite.");
                if (!IsFinite(row.Maximum) || row.Maximum <= 0) throw new InvalidOperationException("Workload list row maximum values must be greater than zero.");
                if (row.Value < 0) throw new InvalidOperationException("Workload list row values must be zero or greater.");
                if (row.AvatarText.Length > 4) throw new InvalidOperationException("Workload list row avatar text must be four characters or fewer.");
                if (row.Note.Length > 32) throw new InvalidOperationException("Workload list row notes must be thirty-two characters or fewer.");
            }

            return;
        }

        if (block is ActivityTimelineBlock activityBlock) {
            if (activityBlock.Items.Count == 0) throw new InvalidOperationException("Activity timeline blocks must contain at least one item.");
            foreach (var item in activityBlock.Items) {
                if (item.Title.Length == 0) throw new InvalidOperationException("Activity timeline items must define text.");
                if (item.Badge.Length > 24) throw new InvalidOperationException("Activity timeline item badges must be twenty-four characters or fewer.");
                if (item.Symbol.Length > 4) throw new InvalidOperationException("Activity timeline item symbols must be four characters or fewer.");
                if (item.HiddenCount < 0) throw new InvalidOperationException("Activity timeline hidden item counts must be zero or greater.");
            }

            return;
        }

        if (block is ScheduleTimelineBlock scheduleBlock) {
            if (!IsFinite(scheduleBlock.Start) || !IsFinite(scheduleBlock.End) || scheduleBlock.End <= scheduleBlock.Start) throw new InvalidOperationException("Schedule timeline time range must be finite and increasing.");
            if (!IsFinite(scheduleBlock.TickInterval) || scheduleBlock.TickInterval <= 0) throw new InvalidOperationException("Schedule timeline tick interval must be finite and greater than zero.");
            if (ScheduleTickCount(scheduleBlock) > MaximumScheduleTicks) throw new InvalidOperationException("Schedule timeline tick interval creates too many ticks for the configured range.");
            if (scheduleBlock.CurrentTime.HasValue && !IsFinite(scheduleBlock.CurrentTime.Value)) throw new InvalidOperationException("Schedule timeline current time must be finite.");
            if (scheduleBlock.Events.Count == 0) throw new InvalidOperationException("Schedule timeline blocks must contain at least one event.");
            foreach (var action in scheduleBlock.HeaderActions) if (action.Length > 24) throw new InvalidOperationException("Schedule timeline header actions must be twenty-four characters or fewer.");
            foreach (var item in scheduleBlock.Events) {
                if (item.Title.Length == 0) throw new InvalidOperationException("Schedule timeline events must define a title.");
                if (!IsFinite(item.Start) || !IsFinite(item.End) || item.End < item.Start) throw new InvalidOperationException("Schedule timeline event times must be finite and increasing.");
                if (item.Lane < 0) throw new InvalidOperationException("Schedule timeline event lanes must be zero or greater.");
                if (item.Lane >= MaximumScheduleLanes) throw new InvalidOperationException("Schedule timeline event lanes must be below " + MaximumScheduleLanes.ToString(CultureInfo.InvariantCulture) + ".");
                if (item.Badge.Length > 24) throw new InvalidOperationException("Schedule timeline event badges must be twenty-four characters or fewer.");
                foreach (var avatar in item.Avatars) if (avatar.Length > 4) throw new InvalidOperationException("Schedule timeline avatar labels must be four characters or fewer.");
            }

            return;
        }

        if (block is DateStripBlock dateStrip) {
            if (dateStrip.Header.Length > 64) throw new InvalidOperationException("Date strip headers must be sixty-four characters or fewer.");
            if (dateStrip.PreviousSymbol.Length > 4) throw new InvalidOperationException("Date strip previous symbols must be four characters or fewer.");
            if (dateStrip.NextSymbol.Length > 4) throw new InvalidOperationException("Date strip next symbols must be four characters or fewer.");
            if (dateStrip.Items.Count == 0) throw new InvalidOperationException("Date strip blocks must contain at least one item.");
            foreach (var item in dateStrip.Items) {
                if (item.Label.Length == 0) throw new InvalidOperationException("Date strip items must define a label.");
                if (item.Label.Length > 12) throw new InvalidOperationException("Date strip item labels must be twelve characters or fewer.");
                if (item.Value.Length == 0) throw new InvalidOperationException("Date strip items must define a value.");
                if (item.Value.Length > 16) throw new InvalidOperationException("Date strip item values must be sixteen characters or fewer.");
            }

            return;
        }

        if (block is EntityStripBlock entityStrip) {
            if (entityStrip.Items.Count == 0) throw new InvalidOperationException("Entity strip blocks must contain at least one item.");
            if (entityStrip.ActionLabel.Length > 48) throw new InvalidOperationException("Entity strip action labels must be forty-eight characters or fewer.");
            if (entityStrip.ActionSymbol.Length > 4) throw new InvalidOperationException("Entity strip action symbols must be four characters or fewer.");
            if (entityStrip.ActionUrl.Length > 0 && !IsSafeActionUrl(entityStrip.ActionUrl)) throw new InvalidOperationException("Entity strip action URLs must be relative URLs, http(s), or mailto links.");
            foreach (var item in entityStrip.Items) {
                if (item.Label.Length == 0) throw new InvalidOperationException("Entity strip items must define a label.");
                if (item.AvatarText.Length > 4) throw new InvalidOperationException("Entity strip item avatar text must be four characters or fewer.");
            }

            return;
        }

        if (block is SectionHeaderBlock sectionHeader) {
            if (sectionHeader.Title.Length == 0) throw new InvalidOperationException("Section header blocks must define a title.");
            if (sectionHeader.ActionLabel.Length > 48) throw new InvalidOperationException("Section header action labels must be forty-eight characters or fewer.");
            if (sectionHeader.ActionSymbol.Length > 4) throw new InvalidOperationException("Section header action symbols must be four characters or fewer.");
            if (sectionHeader.ActionUrl.Length > 0 && !IsSafeActionUrl(sectionHeader.ActionUrl)) throw new InvalidOperationException("Section header action URLs must be relative URLs, http(s), or mailto links.");
            return;
        }

        throw new NotSupportedException("Unsupported visual block type: " + block.GetType().FullName);
    }

    public static ChartColor StatusColor(ChartTheme theme, VisualStatus status) {
        switch (status) {
            case VisualStatus.Positive: return theme.Positive;
            case VisualStatus.Warning: return theme.Warning;
            case VisualStatus.Negative: return theme.Negative;
            case VisualStatus.Info: return PaletteAt(theme, 0);
            case VisualStatus.Neutral: return theme.MutedText;
            default: return theme.MutedText;
        }
    }

    public static VisualStatus ParseStatus(string value) {
        if (string.IsNullOrWhiteSpace(value)) return VisualStatus.None;
        var text = value.Trim();
        if (EqualsAny(text, "ok", "healthy", "success", "pass", "passed", "online", "ready")) return VisualStatus.Positive;
        if (EqualsAny(text, "warn", "warning", "attention", "degraded", "partial")) return VisualStatus.Warning;
        if (EqualsAny(text, "error", "failed", "fail", "critical", "down", "offline")) return VisualStatus.Negative;
        if (EqualsAny(text, "info", "note", "pending", "unknown")) return VisualStatus.Info;
        return VisualStatus.Neutral;
    }

    public static ChartColor PaletteAt(ChartTheme theme, int index) {
        var palette = theme.Palette;
        return palette.Length == 0 ? theme.Text : palette[Math.Abs(index) % palette.Length];
    }

    public static string CssFontFamily(string value) {
        if (string.IsNullOrWhiteSpace(value)) return "system-ui, sans-serif";
        return value.Replace(";", " ").Replace("{", " ").Replace("}", " ").Replace("<", " ").Replace(">", " ");
    }

    public static string Escape(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    public static string StableHash(params string[] values) {
        unchecked {
            var hash = 2166136261u;
            foreach (var value in values) {
                Add(ref hash, value.Length.ToString(CultureInfo.InvariantCulture));
                Add(ref hash, ":");
                Add(ref hash, value);
                Add(ref hash, "|");
            }

            return hash.ToString("x8", CultureInfo.InvariantCulture);
        }
    }

    public static double EstimateTextWidth(string text, double fontSize) {
        var width = 0.0;
        foreach (var ch in text) width += char.IsWhiteSpace(ch) ? 0.32 : char.IsUpper(ch) || char.IsDigit(ch) ? 0.62 : 0.54;
        return width * fontSize;
    }

    public static string FitText(string value, double fontSize, double maxWidth) {
        if (string.IsNullOrEmpty(value) || EstimateTextWidth(value, fontSize) <= maxWidth) return value;
        const string suffix = "...";
        if (EstimateTextWidth(suffix, fontSize) > maxWidth) return string.Empty;
        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = (low + high + 1) / 2;
            if (EstimateTextWidth(value.Substring(0, mid) + suffix, fontSize) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return value.Substring(0, low) + suffix;
    }

    public static double FitFontSize(string value, double maxWidth, double preferredFontSize, double minimumFontSize) {
        var fontSize = Math.Max(minimumFontSize, preferredFontSize);
        while (fontSize > minimumFontSize && EstimateTextWidth(value, fontSize) > maxWidth) fontSize -= 0.5;
        return Math.Max(minimumFontSize, fontSize);
    }

    public static ChartRect ContentRect(VisualBlockOptions options) {
        return new ChartRect(
            options.Padding.Left,
            options.Padding.Top,
            Math.Max(1, options.Size.Width - options.Padding.Left - options.Padding.Right),
            Math.Max(1, options.Size.Height - options.Padding.Top - options.Padding.Bottom));
    }

    public static ChartColor SurfaceBackground(VisualBlockOptions options) =>
        options.TransparentBackground ? ChartColor.Transparent : options.Theme.Background.A == 0 ? options.Theme.CardBackground : options.Theme.Background;

    public static ChartColor CardBackground(VisualBlockOptions options) => options.Theme.CardBackground;

    public static (double Minimum, double Maximum) MiniBarBounds(MetricCard card) {
        return ValueBounds(card.MiniBars, card.MiniBarMinimum, card.MiniBarMaximum, includeZero: true);
    }

    public static int MiniBarHighlightIndex(MetricCard card) => card.MiniBarHighlightIndex ?? card.MiniBars.Count - 1;

    public static (double Minimum, double Maximum) MiniSparklineBounds(MetricCard card) {
        if (card.SecondaryMiniSparkline.Count == 0 || card.MiniSparklineStyle != MetricCardSparklineStyle.Line) return ValueBounds(card.MiniSparkline, card.MiniSparklineMinimum, card.MiniSparklineMaximum, includeZero: false);
        var values = new List<double>(card.MiniSparkline.Count + card.SecondaryMiniSparkline.Count);
        values.AddRange(card.MiniSparkline);
        values.AddRange(card.SecondaryMiniSparkline);
        return ValueBounds(values, card.MiniSparklineMinimum, card.MiniSparklineMaximum, includeZero: false);
    }

    public static VisualMiniBar[] CreateMiniBars(MetricCard card, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        var bounds = MiniBarBounds(card);
        var highlight = MiniBarHighlightIndex(card);
        var count = card.MiniBars.Count;
        var gap = count > 5 ? ChartVisualPrimitives.MiniBarDenseGap : ChartVisualPrimitives.MiniBarGap;
        var barWidth = Math.Max(ChartVisualPrimitives.MiniBarMinWidth, (width - gap * Math.Max(0, count - 1)) / count);
        var activeColor = card.MiniBarColor ?? (card.Status == VisualStatus.None ? PaletteAt(theme, 0) : StatusColor(theme, card.Status));
        var mutedColor = card.MiniBarMutedColor ?? theme.MutedText.WithAlpha(85);
        var bars = new VisualMiniBar[count];
        for (var i = 0; i < count; i++) {
            var value = card.MiniBars[i];
            var ratio = Math.Max(0, Math.Min(1, (value - bounds.Minimum) / (bounds.Maximum - bounds.Minimum)));
            var barHeight = Math.Max(ChartVisualPrimitives.MiniBarMinHeight, height * ratio);
            var currentX = x + i * (barWidth + gap);
            var currentY = y + height - barHeight;
            var highlighted = i == highlight;
            var color = highlighted ? activeColor : mutedColor.WithAlpha((byte)Math.Min(255, Math.Round(mutedColor.A * ChartVisualPrimitives.MiniBarMutedOpacity)));
            bars[i] = new VisualMiniBar(i, value, currentX, currentY, barWidth, barHeight, Math.Min(ChartVisualPrimitives.MiniBarRadiusMax, barWidth * 0.45), color, highlighted);
        }

        return bars;
    }

    public static VisualMiniSparkline CreateMiniSparkline(MetricCard card, double x, double y, double width, double height) {
        return CreateMiniSparkline(card, card.MiniSparkline, card.MiniSparklineColor, card.MiniSparklineFillColor, x, y, width, height);
    }

    public static VisualMiniSparkline CreateSecondaryMiniSparkline(MetricCard card, double x, double y, double width, double height) {
        var color = card.SecondaryMiniSparklineColor ?? (card.MiniSparklineColor ?? PaletteAt(card.Options.Theme, 0)).WithAlpha(220);
        return CreateMiniSparkline(card, card.SecondaryMiniSparkline, color, null, x, y, width, height);
    }

    private static VisualMiniSparkline CreateMiniSparkline(MetricCard card, IReadOnlyList<double> values, ChartColor? lineColor, ChartColor? fill, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        var bounds = MiniSparklineBounds(card);
        var color = lineColor ?? (card.Status == VisualStatus.None ? PaletteAt(theme, 0) : StatusColor(theme, card.Status));
        var fillColor = fill ?? color.WithAlpha((byte)Math.Round(255 * ChartVisualPrimitives.MiniSparklineFillOpacity));
        var points = new ChartPoint[values.Count];
        var step = width / Math.Max(1, values.Count - 1);
        for (var i = 0; i < values.Count; i++) {
            var ratio = Math.Max(0, Math.Min(1, (values[i] - bounds.Minimum) / (bounds.Maximum - bounds.Minimum)));
            points[i] = new ChartPoint(x + i * step, y + height - ratio * height);
        }

        var area = new ChartPoint[points.Length + 2];
        area[0] = new ChartPoint(points[0].X, y + height);
        for (var i = 0; i < points.Length; i++) area[i + 1] = points[i];
        area[area.Length - 1] = new ChartPoint(points[points.Length - 1].X, y + height);
        var strokeWidth = card.MiniSparklineStyle == MetricCardSparklineStyle.Line ? 3.4 : ChartVisualPrimitives.MiniSparklineStrokeWidth;
        var currentRadius = card.MiniSparklineStyle == MetricCardSparklineStyle.Line ? 5.2 : ChartVisualPrimitives.MiniSparklineCurrentRadius;
        return new VisualMiniSparkline(points, area, color, fillColor, strokeWidth, currentRadius);
    }

    public static IReadOnlyList<ChartPoint> SmoothMiniSparklinePoints(VisualMiniSparkline sparkline) {
        return ChartPathBuilder.FromPoints(sparkline.Points, ChartSeriesKind.Line, smooth: true).Flatten(5);
    }

    public static double CompositionTotal(CompositionStatusCard card) {
        var total = 0.0;
        foreach (var segment in card.Segments) total += segment.Value;
        return total;
    }

    public static double DistributionTotal(DistributionStripCard card) {
        var total = 0.0;
        foreach (var segment in card.Segments) total += segment.Value;
        return total;
    }

    public static double SegmentRatio(double value, double maximum) => maximum <= 0 ? 0 : Math.Max(0, Math.Min(1, value / maximum));

    public static int FilledSegments(SegmentedProgressRow row) => Math.Max(0, Math.Min(row.Segments, (int)Math.Round(row.Segments * SegmentRatio(row.Value, row.Maximum))));

    public static double WorkloadRatio(WorkloadListRow row) => SegmentRatio(row.Value, row.Maximum);

    public static string WorkloadDisplayValue(WorkloadListRow row) =>
        row.DisplayValue.Length > 0 ? row.DisplayValue : row.Value.ToString("0.##", CultureInfo.InvariantCulture) + "/" + row.Maximum.ToString("0.##", CultureInfo.InvariantCulture);

    public static int ScheduleLaneCount(ScheduleTimelineBlock block) {
        var lanes = 0;
        foreach (var item in block.Events) lanes = Math.Max(lanes, item.Lane + 1);
        return Math.Max(1, lanes);
    }

    public static double ScheduleRatio(ScheduleTimelineBlock block, double value) => SegmentRatio(value - block.Start, block.End - block.Start);

    public static IEnumerable<double> ScheduleTicks(ScheduleTimelineBlock block) {
        var limit = block.End + block.TickInterval * 0.25;
        var tick = block.Start;
        for (var i = 0; i < MaximumScheduleTicks && tick <= limit; i++) {
            yield return tick;
            var next = tick + block.TickInterval;
            if (next <= tick) yield break;
            tick = next;
        }
    }

    public static bool IsScheduleTimeInRange(ScheduleTimelineBlock block, double value) =>
        value >= block.Start && value <= block.End;

    public static bool ScheduleEventIntersects(ScheduleTimelineBlock block, ScheduleTimelineEvent item) =>
        item.End >= block.Start && item.Start <= block.End;

    public static string FormatScheduleHour(double value) {
        var whole = (int)Math.Floor(value);
        var minutes = (int)Math.Round((value - whole) * 60);
        if (minutes >= 60) { whole++; minutes -= 60; }
        var normalized = ((whole % 24) + 24) % 24;
        var suffix = normalized >= 12 ? "PM" : "AM";
        var hour = normalized % 12;
        if (hour == 0) hour = 12;
        return hour.ToString(CultureInfo.InvariantCulture) + (minutes == 0 ? ".00 " : "." + minutes.ToString("00", CultureInfo.InvariantCulture) + " ") + suffix;
    }

    public static (double ItemWidth, double Gap) FitRepeatedItems(int count, double width, double preferredGap, double minimumItemWidth) {
        if (count <= 0 || width <= 0) return (0, 0);
        if (count == 1) return (Math.Max(0, width), 0);
        var minWidth = Math.Max(0, minimumItemWidth);
        var gap = Math.Min(Math.Max(0, preferredGap), Math.Max(0, (width - minWidth * count) / (count - 1)));
        var itemWidth = Math.Max(0, (width - gap * (count - 1)) / count);
        if (itemWidth < minWidth && width < minWidth * count) {
            gap = 0;
            itemWidth = Math.Max(0, width / count);
        }

        return (itemWidth, gap);
    }

    public static double EffectiveStackGap(int count, double width, double preferredGap) {
        if (count <= 1 || width <= 0) return 0;
        return Math.Min(Math.Max(0, preferredGap), Math.Max(0, width * 0.35 / (count - 1)));
    }

    public static double EffectiveHeatmapGap(double plotWidth, double plotHeight, int columns, int rows, double desiredGap) {
        var gap = Math.Max(0, desiredGap);
        if (columns > 1) gap = Math.Min(gap, Math.Max(0, (plotWidth - columns) / (columns - 1)));
        if (rows > 1) gap = Math.Min(gap, Math.Max(0, (plotHeight - rows) / (rows - 1)));
        return gap;
    }

    public static (double Minimum, double Maximum) TableCellMicroVisualBounds(ChartTableCell cell) {
        var minimum = cell.MicroVisualMinimum ?? Minimum(cell.MicroVisualValues);
        var maximum = cell.MicroVisualMaximum ?? Maximum(cell.MicroVisualValues);
        if (Math.Abs(maximum - minimum) < double.Epsilon) maximum = minimum + 1;
        return (minimum, maximum);
    }

    public static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static void ValidateTableCellMicroVisual(ChartTableCell cell) {
        if (cell.BadgeText.Length > 24) throw new InvalidOperationException("Chart table cell badge text must be twenty-four characters or fewer.");
        if (cell.MicroVisualKind == ChartTableCellMicroVisualKind.None) return;
        if (cell.MicroVisualValues.Count == 0) throw new InvalidOperationException("Chart table cell microvisuals must contain values.");
        if (cell.MicroVisualValues.Count > MaximumTableMicroVisualPoints) throw new InvalidOperationException("Chart table cell microvisuals must contain no more than " + MaximumTableMicroVisualPoints.ToString(CultureInfo.InvariantCulture) + " values.");
        if (cell.MicroVisualKind == ChartTableCellMicroVisualKind.Sparkline && cell.MicroVisualValues.Count < 2) throw new InvalidOperationException("Chart table cell sparklines require at least two values.");
        foreach (var value in cell.MicroVisualValues) if (!IsFinite(value)) throw new InvalidOperationException("Chart table cell microvisual values must be finite.");
        if (cell.MicroVisualMinimum.HasValue && !IsFinite(cell.MicroVisualMinimum.Value)) throw new InvalidOperationException("Chart table cell microvisual minimum values must be finite.");
        if (cell.MicroVisualMaximum.HasValue && !IsFinite(cell.MicroVisualMaximum.Value)) throw new InvalidOperationException("Chart table cell microvisual maximum values must be finite.");
        if (cell.MicroVisualMinimum.HasValue && cell.MicroVisualMaximum.HasValue && cell.MicroVisualMaximum.Value <= cell.MicroVisualMinimum.Value) throw new InvalidOperationException("Chart table cell microvisual maximum must be greater than minimum.");
    }

    public static bool IsSafeActionUrl(string value) {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var text = value.Trim();
        if (text.StartsWith("//", StringComparison.Ordinal)) return false;
        if (Uri.TryCreate(text, UriKind.Absolute, out var uri)) {
            return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase);
        }

        return Uri.TryCreate(text, UriKind.Relative, out _);
    }

    private static int ScheduleTickCount(ScheduleTimelineBlock block) {
        var limit = block.End + block.TickInterval * 0.25;
        var range = limit - block.Start;
        if (range <= 0 || block.TickInterval <= 0) return 0;
        var count = Math.Floor(range / block.TickInterval) + 1;
        return count >= int.MaxValue ? int.MaxValue : (int)count;
    }

    private static bool EqualsAny(string text, params string[] values) {
        foreach (var value in values) if (string.Equals(text, value, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static double Minimum(IReadOnlyList<double> values) {
        var minimum = double.PositiveInfinity;
        foreach (var value in values) minimum = Math.Min(minimum, value);
        return minimum;
    }

    private static double Maximum(IReadOnlyList<double> values) {
        var maximum = double.NegativeInfinity;
        foreach (var value in values) maximum = Math.Max(maximum, value);
        return maximum;
    }

    private static (double Minimum, double Maximum) ValueBounds(IReadOnlyList<double> values, double? configuredMinimum, double? configuredMaximum, bool includeZero) {
        var minimum = configuredMinimum ?? (includeZero ? Math.Min(0, Minimum(values)) : Minimum(values));
        var maximum = configuredMaximum ?? (includeZero ? Math.Max(0, Maximum(values)) : Maximum(values));
        if (maximum <= minimum) maximum = minimum + 1;
        return (minimum, maximum);
    }

    private static void Add(ref uint hash, string value) {
        foreach (var ch in value) {
            hash ^= ch;
            hash *= 16777619u;
        }
    }
}
