using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    public static SegmentedCapsuleLayout CapsuleRingLayout(VisualBlockOptions options, ChartRect content, double y, bool hasAction) {
        var footerHeight = hasAction ? Math.Min(42, Math.Max(32, options.Size.Height * 0.13)) : 0;
        var bottom = options.Size.Height - options.Padding.Bottom - footerHeight;
        var legendRight = content.Width >= 430;
        var legendWidth = legendRight ? Math.Min(230, Math.Max(150, content.Width * 0.34)) : content.Width;
        var availableRingWidth = legendRight ? Math.Max(160, content.Width - legendWidth - 30) : content.Width;
        var ringHeight = Math.Max(72, Math.Min(128, Math.Min(bottom - y - 18, availableRingWidth * 0.43)));
        var ringWidth = legendRight ? Math.Min(availableRingWidth, Math.Max(180, ringHeight * 3.75)) : availableRingWidth;
        var stroke = Math.Max(16, Math.Min(30, ringHeight * 0.235));
        var ringX = content.X + stroke / 2;
        var ringY = y + stroke / 2 + Math.Max(0, (bottom - y - ringHeight) * 0.25);
        var pathWidth = Math.Max(1, ringWidth - stroke);
        var pathHeight = Math.Max(1, ringHeight - stroke);
        var legendX = legendRight ? content.X + ringWidth + 30 : content.X;
        var legendY = legendRight ? ringY - stroke / 2 + 8 : ringY + pathHeight + stroke + 14;
        return new SegmentedCapsuleLayout(footerHeight, bottom, legendRight, legendWidth, ringWidth, ringHeight, stroke, ringX, ringY, pathWidth, pathHeight, legendX, legendY, Math.Max(0, bottom - legendY));
    }

    public static SegmentedHeaderLayout SegmentedHeaderLayout(SegmentedMetricBlock card, double x, double y, double width) {
        var theme = card.Options.Theme;
        var badgeSize = card.HeaderSymbol.Length > 0 ? 48.0 : 0.0;
        var textX = x + (badgeSize > 0 ? badgeSize + 18 : 0);
        var menuReserve = card.ShowMenu ? 42.0 : 0.0;
        var titleHeight = card.Title.Length > 0 ? theme.TitleFontSize + (card.Subtitle.Length > 0 ? theme.SubtitleFontSize + 13 : 8) : 0;
        var dividerY = y + Math.Max(badgeSize, titleHeight) + 18;
        return new SegmentedHeaderLayout(badgeSize, textX, Math.Max(1, width - (textX - x) - menuReserve), x + width - 22, y + 22, dividerY, dividerY + 24);
    }

    public static SegmentedFunnelLayout SegmentedFunnelLayout(SegmentedMetricBlock card, ChartRect content, double y, double footerHeight) {
        var theme = card.Options.Theme;
        var bottom = card.Options.Size.Height - card.Options.Padding.Bottom - footerHeight;
        var totalSegments = 0;
        foreach (var item in card.Items) totalSegments += Math.Max(1, item.Segments);
        var groupGap = Math.Min(16, Math.Max(8, content.Width * 0.018));
        var availableWidth = Math.Max(1, content.Width - groupGap * Math.Max(0, card.Items.Count - 1));
        var intraStageGaps = Math.Max(0, totalSegments - card.Items.Count);
        var metrics = FitRepeatedItems(totalSegments, availableWidth, intraStageGaps, 4, 3);
        var barWidth = Math.Max(2, metrics.ItemWidth);
        var barGap = metrics.Gap;
        var labelHeight = Math.Max(36, theme.SubtitleFontSize * 2.2);
        var availableBarHeight = Math.Max(1, bottom - y - labelHeight - 6);
        var barHeight = Math.Min(132, availableBarHeight);
        var barY = bottom - barHeight - 4;
        var stages = new List<SegmentedFunnelStageLayout>(card.Items.Count);
        var cursor = content.X;
        for (var stageIndex = 0; stageIndex < card.Items.Count; stageIndex++) {
            var item = card.Items[stageIndex];
            var segmentCount = Math.Max(1, item.Segments);
            var groupWidth = segmentCount * barWidth + Math.Max(0, segmentCount - 1) * barGap;
            stages.Add(new SegmentedFunnelStageLayout(item, stageIndex, segmentCount, cursor, groupWidth));
            cursor += groupWidth + groupGap;
        }

        return new SegmentedFunnelLayout(totalSegments, groupGap, barWidth, barGap, barHeight, barY, stages);
    }

    public static SegmentedProgressRowsLayout SegmentedProgressRowsLayout(SegmentedMetricBlock card, double y, bool hasAction) {
        var options = card.Options;
        var footerHeight = hasAction ? Math.Min(58, Math.Max(42, options.Size.Height * 0.16)) : 0;
        var bottom = options.Size.Height - options.Padding.Bottom - footerHeight;
        var rowHeight = Math.Max(48, Math.Min(72, (bottom - y) / Math.Max(1, card.Items.Count)));
        return new SegmentedProgressRowsLayout(footerHeight, bottom, rowHeight);
    }

    public static SegmentedProgressRowLayout SegmentedProgressRowLayout(SegmentedMetricBlock card, SegmentedMetricItem row, ChartRect content, double y, double rowHeight, ChartColor accent) {
        var theme = card.Options.Theme;
        var valueText = SegmentedProgressValueText(row);
        var deltaColor = SegmentedDeltaColor(theme, row, accent);
        var valueFontSize = Math.Max(13, theme.SubtitleFontSize + 1);
        var valueWidth = Math.Min(160, Math.Max(46, EstimateTextWidth(valueText, valueFontSize) + 8));
        var deltaWidth = row.Delta.Length == 0 ? 0 : Math.Min(92, EstimateTextWidth(row.Delta, theme.SubtitleFontSize) + 18);
        var valueX = content.X + content.Width - valueWidth;
        var deltaX = valueX - deltaWidth - 8;
        var labelWidth = Math.Max(40, (row.Delta.Length > 0 ? deltaX : valueX) - content.X - 10);
        var stripY = y + 30;
        var stripHeight = Math.Max(12, Math.Min(22, rowHeight * 0.28));
        return new SegmentedProgressRowLayout(valueText, deltaColor, valueFontSize, valueWidth, deltaWidth, valueX, deltaX, labelWidth, stripY, stripHeight);
    }

    public static IReadOnlyList<SegmentedProgressStripSegment> SegmentedProgressStripSegments(SegmentedMetricItem row, double x, double y, double width, double height) {
        var metrics = FitRepeatedItems(row.Segments, width, row.Segments > 50 ? 2.0 : 3.0, 2);
        var filled = FilledSegments(row);
        var segments = new List<SegmentedProgressStripSegment>(row.Segments);
        for (var i = 0; i < row.Segments; i++) {
            var segmentWidth = metrics.ItemWidth;
            var segmentX = x + i * (segmentWidth + metrics.Gap);
            segments.Add(new SegmentedProgressStripSegment(i, segmentX, y, segmentWidth, height, Math.Min(4, segmentWidth * 0.35), i < filled));
        }

        return segments;
    }

    public static SegmentedCompositionLayout SegmentedCompositionLayout(SegmentedMetricBlock card, ChartRect content, double y, bool hasAction) {
        var options = card.Options;
        var footerHeight = hasAction ? Math.Min(42, Math.Max(32, options.Size.Height * 0.16)) : 0;
        var metricSize = Math.Min(46, Math.Max(28, options.Size.Height * 0.16));
        var stripY = y + metricSize + 20;
        var stripHeight = Math.Max(20, Math.Min(34, options.Size.Height * 0.09));
        var legendY = stripY + stripHeight + 18;
        var legendBottom = options.Size.Height - options.Padding.Bottom - footerHeight;
        var rowHeight = Math.Min(38, Math.Max(14, Math.Max(1, legendBottom - legendY) / Math.Max(1, card.Items.Count)));
        return new SegmentedCompositionLayout(footerHeight, metricSize, stripY, stripHeight, legendY, legendBottom, rowHeight);
    }

    public static SegmentedDistributionLayout SegmentedDistributionLayout(SegmentedMetricBlock card, ChartRect content, double y, bool hasAction) {
        var theme = card.Options.Theme;
        var footerHeight = hasAction ? Math.Min(42, Math.Max(32, card.Options.Size.Height * 0.13)) : 0;
        var bottom = card.Options.Size.Height - card.Options.Padding.Bottom - footerHeight;
        var metricSize = Math.Min(38, Math.Max(24, card.Options.Size.Height * 0.12));
        var captionWidth = card.Caption.Length == 0 ? 0 : Math.Min(content.Width * 0.34, EstimateTextWidth(card.Caption, theme.SubtitleFontSize) + 12);
        var stripY = y + theme.SubtitleFontSize + metricSize + 30;
        var stripHeight = Math.Max(14, Math.Min(24, card.Options.Size.Height * 0.065));
        var legendY = stripY + stripHeight + 12;
        var legendChips = DistributionLegendChips(card, content.X, legendY, content.Width);
        var legendHeight = legendChips.Count == 0 ? 20 : legendChips[legendChips.Count - 1].Y - legendY + 20;
        var rowsY = legendY + legendHeight + 10;
        var rowCount = Math.Max(1, card.Items.Count);
        var rowHeight = Math.Max(27, Math.Min(36, (bottom - rowsY) / rowCount));
        return new SegmentedDistributionLayout(footerHeight, bottom, metricSize, captionWidth, stripY, stripHeight, legendY, legendHeight, rowsY, rowHeight, legendChips);
    }

    private static IReadOnlyList<SegmentedDistributionChipLayout> DistributionLegendChips(SegmentedMetricBlock card, double x, double y, double width) {
        var theme = card.Options.Theme;
        var rowHeight = 20.0;
        var cursorX = x;
        var cursorY = y;
        var total = SegmentedTotal(card);
        var fontSize = Math.Max(9, theme.SubtitleFontSize - 2);
        var chips = new List<SegmentedDistributionChipLayout>(card.Items.Count);
        for (var i = 0; i < card.Items.Count; i++) {
            var segment = card.Items[i];
            var label = ShortSegmentLabel(segment.Label) + " " + SegmentedShareText(segment, total, "0.##");
            var chipWidth = Math.Min(140, Math.Max(54, EstimateTextWidth(label, fontSize) + 18));
            if (cursorX + chipWidth > x + width && cursorX > x) {
                cursorX = x;
                cursorY += rowHeight;
            }

            chips.Add(new SegmentedDistributionChipLayout(segment, i, label, cursorX, cursorY, chipWidth, fontSize));
            cursorX += chipWidth + 10;
        }

        return chips;
    }

    public static SegmentedDistributionRowLayout SegmentedDistributionRowLayout(SegmentedMetricBlock card, SegmentedMetricItem segment, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        var total = SegmentedTotal(card);
        var rowY = y + height * 0.5;
        var badgeSize = Math.Min(22, Math.Max(16, height - 9));
        var ringRadius = Math.Min(8.5, Math.Max(6, height * 0.24));
        var percentWidth = 58.0;
        var displayValueWidth = segment.DisplayValue.Length == 0 ? 0 : Math.Min(96, EstimateTextWidth(segment.DisplayValue, theme.SubtitleFontSize) + 8);
        var labelWidth = Math.Max(1, width - badgeSize - 16 - percentWidth - displayValueWidth - 34);
        return new SegmentedDistributionRowLayout(
            rowY,
            badgeSize,
            ringRadius,
            percentWidth,
            displayValueWidth,
            labelWidth,
            SegmentedShare(segment, total),
            segment.Symbol.Length > 0 ? segment.Symbol : ShortSegmentLabel(segment.Label),
            SegmentedShareText(segment, total, "0.##"));
    }

    public static double CapsuleRingBoundaryGapRatio(double width, double height, double stroke) {
        var radius = Math.Max(1, Math.Min(height / 2, width / 2));
        var straight = Math.Max(0, width - radius * 2);
        var perimeter = Math.Max(1, straight * 2 + Math.PI * radius * 2);
        return Math.Min(0.003, Math.Max(0.0015, stroke * 0.10 / perimeter));
    }
}

internal readonly struct SegmentedMetricSlice {
    public SegmentedMetricSlice(SegmentedMetricItem item, int index, double start, double end, double share) {
        Item = item;
        Index = index;
        Start = start;
        End = end;
        Share = share;
    }

    public SegmentedMetricItem Item { get; }
    public int Index { get; }
    public double Start { get; }
    public double End { get; }
    public double Share { get; }
}

internal readonly struct SegmentedStackSegment {
    public SegmentedStackSegment(SegmentedMetricItem item, int index, double x, double y, double width, double height, double share) {
        Item = item;
        Index = index;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Share = share;
    }

    public SegmentedMetricItem Item { get; }
    public int Index { get; }
    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }
    public double Share { get; }
}

internal readonly struct SegmentedCapsuleLayout {
    public SegmentedCapsuleLayout(double footerHeight, double bottom, bool legendRight, double legendWidth, double ringWidth, double ringHeight, double stroke, double ringX, double ringY, double pathWidth, double pathHeight, double legendX, double legendY, double legendHeight) {
        FooterHeight = footerHeight;
        Bottom = bottom;
        LegendRight = legendRight;
        LegendWidth = legendWidth;
        RingWidth = ringWidth;
        RingHeight = ringHeight;
        Stroke = stroke;
        RingX = ringX;
        RingY = ringY;
        PathWidth = pathWidth;
        PathHeight = pathHeight;
        LegendX = legendX;
        LegendY = legendY;
        LegendHeight = legendHeight;
    }

    public double FooterHeight { get; }
    public double Bottom { get; }
    public bool LegendRight { get; }
    public double LegendWidth { get; }
    public double RingWidth { get; }
    public double RingHeight { get; }
    public double Stroke { get; }
    public double RingX { get; }
    public double RingY { get; }
    public double PathWidth { get; }
    public double PathHeight { get; }
    public double LegendX { get; }
    public double LegendY { get; }
    public double LegendHeight { get; }
}

internal readonly struct SegmentedHeaderLayout {
    public SegmentedHeaderLayout(double badgeSize, double textX, double textWidth, double menuDotStartX, double menuDotY, double dividerY, double nextY) {
        BadgeSize = badgeSize;
        TextX = textX;
        TextWidth = textWidth;
        MenuDotStartX = menuDotStartX;
        MenuDotY = menuDotY;
        DividerY = dividerY;
        NextY = nextY;
    }

    public double BadgeSize { get; }
    public double TextX { get; }
    public double TextWidth { get; }
    public double MenuDotStartX { get; }
    public double MenuDotY { get; }
    public double DividerY { get; }
    public double NextY { get; }
}

internal readonly struct SegmentedFunnelLayout {
    public SegmentedFunnelLayout(int totalSegments, double groupGap, double barWidth, double barGap, double barHeight, double barY, IReadOnlyList<SegmentedFunnelStageLayout> stages) {
        TotalSegments = totalSegments;
        GroupGap = groupGap;
        BarWidth = barWidth;
        BarGap = barGap;
        BarHeight = barHeight;
        BarY = barY;
        Stages = stages;
    }

    public int TotalSegments { get; }
    public double GroupGap { get; }
    public double BarWidth { get; }
    public double BarGap { get; }
    public double BarHeight { get; }
    public double BarY { get; }
    public IReadOnlyList<SegmentedFunnelStageLayout> Stages { get; }
}

internal readonly struct SegmentedFunnelStageLayout {
    public SegmentedFunnelStageLayout(SegmentedMetricItem item, int index, int segmentCount, double x, double width) {
        Item = item;
        Index = index;
        SegmentCount = segmentCount;
        X = x;
        Width = width;
    }

    public SegmentedMetricItem Item { get; }
    public int Index { get; }
    public int SegmentCount { get; }
    public double X { get; }
    public double Width { get; }
}

internal readonly struct SegmentedProgressRowsLayout {
    public SegmentedProgressRowsLayout(double footerHeight, double bottom, double rowHeight) {
        FooterHeight = footerHeight;
        Bottom = bottom;
        RowHeight = rowHeight;
    }

    public double FooterHeight { get; }
    public double Bottom { get; }
    public double RowHeight { get; }
}

internal readonly struct SegmentedProgressRowLayout {
    public SegmentedProgressRowLayout(string valueText, ChartColor deltaColor, double valueFontSize, double valueWidth, double deltaWidth, double valueX, double deltaX, double labelWidth, double stripY, double stripHeight) {
        ValueText = valueText;
        DeltaColor = deltaColor;
        ValueFontSize = valueFontSize;
        ValueWidth = valueWidth;
        DeltaWidth = deltaWidth;
        ValueX = valueX;
        DeltaX = deltaX;
        LabelWidth = labelWidth;
        StripY = stripY;
        StripHeight = stripHeight;
    }

    public string ValueText { get; }
    public ChartColor DeltaColor { get; }
    public double ValueFontSize { get; }
    public double ValueWidth { get; }
    public double DeltaWidth { get; }
    public double ValueX { get; }
    public double DeltaX { get; }
    public double LabelWidth { get; }
    public double StripY { get; }
    public double StripHeight { get; }
}

internal readonly struct SegmentedProgressStripSegment {
    public SegmentedProgressStripSegment(int index, double x, double y, double width, double height, double radius, bool filled) {
        Index = index;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Radius = radius;
        Filled = filled;
    }

    public int Index { get; }
    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }
    public double Radius { get; }
    public bool Filled { get; }
}

internal readonly struct SegmentedCompositionLayout {
    public SegmentedCompositionLayout(double footerHeight, double metricSize, double stripY, double stripHeight, double legendY, double legendBottom, double rowHeight) {
        FooterHeight = footerHeight;
        MetricSize = metricSize;
        StripY = stripY;
        StripHeight = stripHeight;
        LegendY = legendY;
        LegendBottom = legendBottom;
        RowHeight = rowHeight;
    }

    public double FooterHeight { get; }
    public double MetricSize { get; }
    public double StripY { get; }
    public double StripHeight { get; }
    public double LegendY { get; }
    public double LegendBottom { get; }
    public double RowHeight { get; }
}

internal readonly struct SegmentedDistributionLayout {
    public SegmentedDistributionLayout(double footerHeight, double bottom, double metricSize, double captionWidth, double stripY, double stripHeight, double legendY, double legendHeight, double rowsY, double rowHeight, IReadOnlyList<SegmentedDistributionChipLayout> legendChips) {
        FooterHeight = footerHeight;
        Bottom = bottom;
        MetricSize = metricSize;
        CaptionWidth = captionWidth;
        StripY = stripY;
        StripHeight = stripHeight;
        LegendY = legendY;
        LegendHeight = legendHeight;
        RowsY = rowsY;
        RowHeight = rowHeight;
        LegendChips = legendChips;
    }

    public double FooterHeight { get; }
    public double Bottom { get; }
    public double MetricSize { get; }
    public double CaptionWidth { get; }
    public double StripY { get; }
    public double StripHeight { get; }
    public double LegendY { get; }
    public double LegendHeight { get; }
    public double RowsY { get; }
    public double RowHeight { get; }
    public IReadOnlyList<SegmentedDistributionChipLayout> LegendChips { get; }
}

internal readonly struct SegmentedDistributionChipLayout {
    public SegmentedDistributionChipLayout(SegmentedMetricItem item, int index, string label, double x, double y, double width, double fontSize) {
        Item = item;
        Index = index;
        Label = label;
        X = x;
        Y = y;
        Width = width;
        FontSize = fontSize;
    }

    public SegmentedMetricItem Item { get; }
    public int Index { get; }
    public string Label { get; }
    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double FontSize { get; }
}

internal readonly struct SegmentedDistributionRowLayout {
    public SegmentedDistributionRowLayout(double y, double badgeSize, double ringRadius, double percentWidth, double displayValueWidth, double labelWidth, double share, string symbolText, string percentText) {
        Y = y;
        BadgeSize = badgeSize;
        RingRadius = ringRadius;
        PercentWidth = percentWidth;
        DisplayValueWidth = displayValueWidth;
        LabelWidth = labelWidth;
        Share = share;
        SymbolText = symbolText;
        PercentText = percentText;
    }

    public double Y { get; }
    public double BadgeSize { get; }
    public double RingRadius { get; }
    public double PercentWidth { get; }
    public double DisplayValueWidth { get; }
    public double LabelWidth { get; }
    public double Share { get; }
    public string SymbolText { get; }
    public string PercentText { get; }
}
