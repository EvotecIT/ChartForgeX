using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    // Shared fitted text implementations live in SvgChartRenderer.TextHelpers.cs: DrawSvgTextCenteredX, DrawSvgTextLeft, DrawSvgXAxisTitle, DrawSvgYAxisTitle.
    private static void DrawLabelPill(StringBuilder sb, Chart chart, string label, double x, double y, ChartColor textColor, string anchor, ChartRect plot) {
        var t = chart.Options.Theme;
        var maxWidth = Math.Max(42, PlotLabelMaxWidth(plot));
        var fontSize = TextFontSizeForSvgWidth(label, Math.Max(24, maxWidth - 18), t.TickLabelFontSize);
        label = TrimSvgLabelToWidth(label, fontSize, Math.Max(24, maxWidth - 18));
        if (label.Length == 0) return;
        var width = Math.Min(maxWidth, Math.Max(36, EstimateTextWidth(label, fontSize) + 18));
        var placement = PlaceLabelPill(x, width, anchor, plot);
        var textX = placement.Anchor == "end" ? placement.X - 9 : placement.X + 9;
        var rectY = Clamp(y - 16, plot.Top + 5, plot.Bottom - 27);
        var writer = new SvgMarkupWriter(512);
        writer.StartElement("rect").Attribute("data-cfx-role", "annotation-label").Attribute("data-cfx-label", label).Attribute("x", placement.RectX).Attribute("y", rectY).Attribute("width", width).Attribute("height", "23").Attribute("rx", "5").Attribute("fill", t.CardBackground.ToCss()).Attribute("opacity", "0.92").Attribute("stroke", textColor.ToCss()).Attribute("stroke-opacity", "0.36").EndEmptyElement().Line();
        writer.StartElement("text").Attribute("data-cfx-role", "annotation-label-text").Attribute("data-cfx-label", label).Attribute("x", textX).Attribute("y", rectY + 16).Attribute("text-anchor", placement.Anchor).Attribute("fill", textColor.ToCss()).Attribute("font-family", SvgFontFamilyAttributeValue(t.FontFamily)).Attribute("font-size", fontSize).Attribute("font-weight", "750").Raw(Escape(label)).EndElement().Line();
        sb.Append(writer.Build());
    }

    private static void DrawDataLabel(StringBuilder sb, Chart chart, string label, double x, double y, ChartRect plot, string role = "data-label", ChartSeries? series = null, int pointIndex = -1) {
        var t = chart.Options.Theme;
        if (!TryFitSvgDataLabel(label, chart, plot, series, pointIndex, out var style, out label, out var fontSize)) return;

        var height = EstimateSvgStyledTextHeight(fontSize, style);
        var safeY = Clamp(y, plot.Top + ChartVisualPrimitives.DataLabelPlotInset + height / 2.0, plot.Bottom - ChartVisualPrimitives.DataLabelPlotInset - height / 2.0);
        var anchor = EdgeAwareAnchor(label, x, plot, fontSize);
        var safeX = EdgeAwareTextX(label, x, plot, fontSize);
        var writer = new SvgMarkupWriter(512);
        WriteSvgDataLabelText(writer, chart, style, role, label, safeX, safeY, anchor, t.Text, t.CardBackground, fontSize);
        sb.Append(writer.Build());
    }

    private static bool ShouldDrawDataLabels(Chart chart, ChartSeries series) => series.ShowDataLabels ?? chart.Options.ShowDataLabels;

    private static bool HasHorizontalBarDataLabels(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.HorizontalBar && ShouldDrawDataLabels(chart, series));

    private static ChartDataLabelPlacement DataLabelPlacement(Chart chart, ChartSeries? series) => series?.DataLabelPlacement ?? chart.Options.DataLabelPlacement;

    private static ChartColor DataLabelConnectorColor(Chart chart) => chart.Options.DataLabelConnectorColor ?? chart.Options.Theme.MutedText;

    private static TextStyleOverride SeriesDataLabelStyle(Chart chart, ChartSeries? series) => DataLabelStyle(chart, series);

    private static TextStyleOverride DataLabelStyle(Chart chart, ChartSeries? series, int pointIndex = -1) {
        if (series != null && pointIndex >= 0 && pointIndex < series.PointDataLabelStyles.Count) {
            var pointStyle = series.PointDataLabelStyles[pointIndex];
            if (pointStyle != null && pointStyle.HasOverrides) return pointStyle;
        }

        return series != null && series.DataLabelStyle.HasOverrides ? series.DataLabelStyle : chart.Options.DataLabelStyle;
    }

    private static void DrawHorizontalValueLabel(StringBuilder sb, Chart chart, string label, double x, double y, string anchor, ChartRect plot, ChartSeries? series = null, int pointIndex = -1) {
        var t = chart.Options.Theme;
        if (!TryFitSvgDataLabel(label, chart, plot, series, pointIndex, out var style, out label, out var fontSize)) return;

        var width = EstimateTextWidth(label, fontSize);
        var height = EstimateSvgStyledTextHeight(fontSize, style);
        var effectiveAnchor = anchor == "end" ? "end" : "start";
        var safeX = effectiveAnchor == "end"
            ? Clamp(x, plot.Left + width + ChartVisualPrimitives.DataLabelPlotInset, plot.Right - ChartVisualPrimitives.DataLabelPlotInset)
            : Clamp(x, plot.Left + ChartVisualPrimitives.DataLabelPlotInset, plot.Right - width - ChartVisualPrimitives.DataLabelPlotInset);
        if (safeX < plot.Left + ChartVisualPrimitives.DataLabelPlotInset) {
            effectiveAnchor = "start";
            safeX = plot.Left + ChartVisualPrimitives.DataLabelPlotInset;
        } else if (safeX > plot.Right - ChartVisualPrimitives.DataLabelPlotInset) {
            effectiveAnchor = "end";
            safeX = plot.Right - ChartVisualPrimitives.DataLabelPlotInset;
        }

        var safeY = Clamp(y, plot.Top + ChartVisualPrimitives.DataLabelPlotInset + height / 2.0, plot.Bottom - ChartVisualPrimitives.DataLabelPlotInset - height / 2.0);
        var writer = new SvgMarkupWriter(512);
        WriteSvgDataLabelText(writer, chart, style, "data-label", label, safeX, safeY, effectiveAnchor, t.Text, t.CardBackground, fontSize);
        sb.Append(writer.Build());
    }

    private static bool ReserveSvgHorizontalLabel(string label, double x, double y, string anchor, Chart chart, ChartRect plot, List<ChartLabelBounds> reserved, ChartSeries? series = null, int pointIndex = -1) {
        if (!TryFitSvgDataLabel(label, chart, plot, series, pointIndex, out var style, out label, out var fontSize)) return false;

        var width = EstimateTextWidth(label, fontSize) + 8;
        var height = EstimateSvgStyledTextHeight(fontSize, style) + 6;
        var effectiveAnchor = anchor == "end" ? "end" : "start";
        var safeX = effectiveAnchor == "end"
            ? Clamp(x, plot.Left + width + ChartVisualPrimitives.DataLabelPlotInset, plot.Right - ChartVisualPrimitives.DataLabelPlotInset)
            : Clamp(x, plot.Left + ChartVisualPrimitives.DataLabelPlotInset, plot.Right - width - ChartVisualPrimitives.DataLabelPlotInset);
        if (safeX < plot.Left + ChartVisualPrimitives.DataLabelPlotInset) {
            effectiveAnchor = "start";
            safeX = plot.Left + ChartVisualPrimitives.DataLabelPlotInset;
        } else if (safeX > plot.Right - ChartVisualPrimitives.DataLabelPlotInset) {
            effectiveAnchor = "end";
            safeX = plot.Right - ChartVisualPrimitives.DataLabelPlotInset;
        }

        var left = effectiveAnchor == "end" ? safeX - width : safeX;
        var safeY = Clamp(y, plot.Top + ChartVisualPrimitives.DataLabelPlotInset + height / 2.0, plot.Bottom - ChartVisualPrimitives.DataLabelPlotInset - height / 2.0);
        var bounds = new ChartLabelBounds(left, safeY - height / 2, width, height);
        foreach (var item in reserved) if (bounds.Intersects(item)) return false;
        reserved.Add(bounds);
        return true;
    }

    private static LabelPillPlacement PlaceLabelPill(double x, double width, string anchor, ChartRect plot) {
        var minX = plot.Left + 4;
        var maxX = plot.Right - 4;
        var effectiveAnchor = anchor == "end" ? "end" : "start";
        var effectiveX = Clamp(x, minX, maxX);
        var rectX = effectiveAnchor == "end" ? effectiveX - width : effectiveX;

        if (rectX < minX) {
            effectiveAnchor = "start";
            effectiveX = minX;
            rectX = effectiveX;
        }

        if (rectX + width > maxX) {
            effectiveAnchor = "end";
            effectiveX = maxX;
            rectX = effectiveX - width;
        }

        if (rectX < minX) rectX = minX;
        return new LabelPillPlacement(effectiveX, rectX, effectiveAnchor);
    }

    private static string EdgeAwareAnchor(string label, double x, ChartRect plot, double fontSize) {
        var halfWidth = EstimateTextWidth(label, fontSize) / 2;
        if (x - halfWidth < plot.Left + ChartVisualPrimitives.DataLabelPlotInset) return "start";
        if (x + halfWidth > plot.Right - ChartVisualPrimitives.DataLabelPlotInset) return "end";
        return "middle";
    }

    private static double EdgeAwareTextX(string label, double x, ChartRect plot, double fontSize) {
        var halfWidth = EstimateTextWidth(label, fontSize) / 2;
        if (x - halfWidth < plot.Left + ChartVisualPrimitives.DataLabelPlotInset) return plot.Left + ChartVisualPrimitives.DataLabelPlotInset;
        if (x + halfWidth > plot.Right - ChartVisualPrimitives.DataLabelPlotInset) return plot.Right - ChartVisualPrimitives.DataLabelPlotInset;
        return x;
    }

    private static string RotatedAnchor(string label, double x, ChartRect plot, double angle, double fontSize) {
        var projectedWidth = EstimateTextWidth(label, fontSize) * Math.Abs(Math.Cos(angle * Math.PI / 180));
        if (x - projectedWidth < plot.Left + ChartVisualPrimitives.DataLabelPlotInset) return "start";
        if (x + projectedWidth > plot.Right - ChartVisualPrimitives.DataLabelPlotInset) return "end";
        return angle < 0 ? "end" : "start";
    }

    private static string EdgeAwareStyledAnchor(Chart chart, string label, double x, ChartRect plot, double fontSize, TextStyleOverride style, bool emphasized = false) {
        var halfWidth = MeasureSvgStyledTextWidth(chart, label, fontSize, style, emphasized) / 2;
        if (x - halfWidth < plot.Left + ChartVisualPrimitives.DataLabelPlotInset) return "start";
        if (x + halfWidth > plot.Right - ChartVisualPrimitives.DataLabelPlotInset) return "end";
        return "middle";
    }

    private static double EdgeAwareStyledTextX(Chart chart, string label, double x, ChartRect plot, double fontSize, TextStyleOverride style, bool emphasized = false) {
        var halfWidth = MeasureSvgStyledTextWidth(chart, label, fontSize, style, emphasized) / 2;
        return Clamp(x, plot.Left + ChartVisualPrimitives.DataLabelPlotInset + halfWidth, plot.Right - ChartVisualPrimitives.DataLabelPlotInset - halfWidth);
    }

    private static string RotatedStyledAnchor(Chart chart, string label, double x, ChartRect plot, double angle, double fontSize, TextStyleOverride style, bool emphasized = false) {
        var projectedWidth = MeasureSvgStyledTextWidth(chart, label, fontSize, style, emphasized) * Math.Abs(Math.Cos(angle * Math.PI / 180));
        if (x - projectedWidth < plot.Left + ChartVisualPrimitives.DataLabelPlotInset) return "start";
        if (x + projectedWidth > plot.Right - ChartVisualPrimitives.DataLabelPlotInset) return "end";
        return angle < 0 ? "end" : "start";
    }

    private static double PlotLabelMaxWidth(ChartRect plot) =>
        Math.Max(8, plot.Width - ChartVisualPrimitives.DataLabelPlotInset * 2);

    private static double EstimateTextWidth(string text, double fontSize) {
        var width = 0.0;
        foreach (var ch in text) width += char.IsWhiteSpace(ch) ? fontSize * 0.34 : char.IsUpper(ch) ? fontSize * 0.62 : fontSize * 0.54;
        return width;
    }

    private static string TrimSvgLabelToWidth(string value, double fontSize, double maxWidth) {
        if (string.IsNullOrEmpty(value) || EstimateTextWidth(value, fontSize) <= maxWidth) return value;
        const string suffix = "...";
        if (EstimateTextWidth(suffix, fontSize) > maxWidth) return string.Empty;

        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = low + (high - low + 1) / 2;
            var candidate = value.Substring(0, mid).TrimEnd() + suffix;
            if (EstimateTextWidth(candidate, fontSize) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return low == 0 ? suffix : value.Substring(0, low).TrimEnd() + suffix;
    }

    private static double TextFontSizeForSvgWidth(string text, double maxWidth, double preferredFontSize, double minFontSize = 8) {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0) return preferredFontSize;
        var fontSize = preferredFontSize;
        while (fontSize > minFontSize && EstimateTextWidth(text, fontSize) > maxWidth) fontSize -= 0.5;
        return Math.Max(minFontSize, fontSize);
    }

    private static ChartColor StyleColor(TextStyleOverride? style, ChartColor fallback) => style?.Color ?? fallback;

    private static double StyleFontSize(TextStyleOverride? style, double fallback) {
        var size = style?.FontSize ?? fallback;
        return style?.Baseline is TextBaseline.Superscript or TextBaseline.Subscript ? size * 0.65 : size;
    }

    private static string StyleWeight(TextStyleOverride? style, string fallback) => style?.FontWeight ?? fallback;

    private static string StyleFontFamily(Chart chart, TextStyleOverride? style) => style?.FontFamily ?? chart.Options.Theme.FontFamily;

    private static ChartColor Color(Chart chart, int index) => chart.Series[index].Color ?? chart.Options.Theme.Palette[index % chart.Options.Theme.Palette.Length];

    private static ChartColor PointColor(Chart chart, ChartSeries series, int seriesIndex, int pointIndex) =>
        pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue
            ? series.PointColors[pointIndex]!.Value
            : Color(chart, seriesIndex);

    private static string BarFill(Chart chart, ChartSeries series, int seriesIndex, int pointIndex, string id) =>
        pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue
            ? $"url(#{id}-seriesFill{seriesIndex}-point{pointIndex})"
            : $"url(#{id}-seriesFill{seriesIndex})";

    private static ChartFillPattern FillPattern(ChartSeries series, int pointIndex) =>
        pointIndex >= 0 && pointIndex < series.PointFillPatterns.Count && series.PointFillPatterns[pointIndex].HasValue
            ? series.PointFillPatterns[pointIndex]!.Value
            : series.FillPattern;

    private static void AppendFillPatternDefinitions(StringBuilder sb, Chart chart, string id) {
        for (var seriesIndex = 0; seriesIndex < chart.Series.Count; seriesIndex++) {
            var series = chart.Series[seriesIndex];
            if (series.FillPattern != ChartFillPattern.None) AppendSvgFillPatternDefinition(sb, FillPatternId(id, seriesIndex, -1), series.FillPattern);
            for (var pointIndex = 0; pointIndex < series.PointFillPatterns.Count; pointIndex++) {
                if (!series.PointFillPatterns[pointIndex].HasValue || series.PointFillPatterns[pointIndex] == ChartFillPattern.None) continue;
                AppendSvgFillPatternDefinition(sb, FillPatternId(id, seriesIndex, pointIndex), series.PointFillPatterns[pointIndex]!.Value);
            }
        }
    }

    private static void AppendSvgFillPatternDefinition(StringBuilder sb, string patternId, ChartFillPattern pattern) {
        var forward = pattern == ChartFillPattern.DiagonalForward || pattern == ChartFillPattern.Crosshatch;
        var backward = pattern == ChartFillPattern.DiagonalBackward || pattern == ChartFillPattern.Crosshatch;
        var opacity = pattern == ChartFillPattern.Crosshatch ? 0.2 : 0.28;
        AppendSvg(sb, writer => {
            writer.StartElement("pattern").Attribute("id", patternId).Attribute("width", "8").Attribute("height", "8").Attribute("patternUnits", "userSpaceOnUse").EndStartElement().Line();
            if (forward) writer.StartElement("path").Attribute("d", "M -2 8 L 8 -2 M 0 10 L 10 0").Attribute("stroke", "#fff").Attribute("stroke-opacity", opacity).Attribute("stroke-width", "1.25").Attribute("stroke-linecap", "round").EndEmptyElement().Line();
            if (backward) writer.StartElement("path").Attribute("d", "M -2 0 L 8 10 M 0 -2 L 10 8").Attribute("stroke", "#fff").Attribute("stroke-opacity", opacity).Attribute("stroke-width", "1.25").Attribute("stroke-linecap", "round").EndEmptyElement().Line();
            writer.EndElement().Line();
        });
    }

    private static string FillPatternId(string id, int seriesIndex, int pointIndex) =>
        pointIndex >= 0 ? $"{id}-fillPattern{seriesIndex}Point{pointIndex}" : $"{id}-fillPattern{seriesIndex}";

    private static string? FillPatternReference(ChartSeries series, int seriesIndex, int pointIndex, string id) {
        var pattern = FillPattern(series, pointIndex);
        if (pattern == ChartFillPattern.None) return null;
        var hasPointPattern = pointIndex >= 0 && pointIndex < series.PointFillPatterns.Count && series.PointFillPatterns[pointIndex].HasValue && series.PointFillPatterns[pointIndex] != ChartFillPattern.None;
        return $"url(#{FillPatternId(id, seriesIndex, hasPointPattern ? pointIndex : -1)})";
    }

    private static void DrawSvgFillPatternOverlay(StringBuilder sb, ChartSeries series, int seriesIndex, int pointIndex, string id, double x, double y, double width, double height, double radius, string role) {
        var writer = new SvgMarkupWriter(512);
        WriteFillPatternOverlay(writer, series, seriesIndex, pointIndex, id, x, y, width, height, radius, role);
        sb.Append(writer.Build());
    }

    private static void WriteFillPatternOverlay(SvgMarkupWriter writer, ChartSeries series, int seriesIndex, int pointIndex, string id, double x, double y, double width, double height, double radius, string role) {
        if (width <= 0.5 || height <= 0.5) return;
        var fill = FillPatternReference(series, seriesIndex, pointIndex, id);
        if (fill == null) return;
        writer.StartElement("rect")
            .Attribute("data-cfx-role", role)
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("data-cfx-point", pointIndex)
            .Attribute("data-cfx-fill-pattern", FillPattern(series, pointIndex).ToString())
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("width", width)
            .Attribute("height", height)
            .Attribute("rx", radius)
            .Attribute("fill", fill)
            .Attribute("pointer-events", "none")
            .EndEmptyElement()
            .Line();
    }

    private static bool ShowXAxis(Chart chart) => !IsMapChart(chart) && chart.Options.ShowAxes && chart.Options.ShowXAxis;

    private static bool ShowYAxis(Chart chart) => !IsMapChart(chart) && chart.Options.ShowAxes && chart.Options.ShowYAxis;

    private static bool ShowXAxisLine(Chart chart) => ShowXAxis(chart) && chart.Options.XAxis.ShowLine;

    private static bool ShowYAxisLine(Chart chart) => ShowYAxis(chart) && chart.Options.YAxis.ShowLine;

    private static bool ShowSecondaryYAxis(Chart chart) => !IsMapChart(chart) && chart.Options.ShowAxes && chart.Options.SecondaryYAxis.Visible;

    private static bool ShowSecondaryYAxisLine(Chart chart) => ShowSecondaryYAxis(chart) && chart.Options.SecondaryYAxis.ShowLine;

    private static bool IsMapChart(Chart chart) {
        foreach (var series in chart.Series) if (ChartSeriesKindTraits.IsMapKind(series.Kind)) return true;
        return false;
    }

    private static bool IsSpatialMapChart(Chart chart) {
        foreach (var series in chart.Series) if (ChartSeriesKindTraits.IsSpatialMapKind(series.Kind)) return true;
        return false;
    }

    private static ChartRect SpatialMapPlotArea(Chart chart) {
        var o = chart.Options;
        var left = Math.Min(o.Padding.Left, 42);
        var right = Math.Min(o.Padding.Right, 42);
        var top = o.ShowHeader ? Math.Min(o.Padding.Top + 10, 88) : Math.Min(o.Padding.Top, 42);
        var bottom = Math.Min(o.Padding.Bottom, 42);
        return new ChartRect(left, top, Math.Max(1, o.Size.Width - left - right), Math.Max(1, o.Size.Height - top - bottom));
    }

    private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    private static string FormatNumber(double v) => ChartNumericFormatter.FormatCompact(v);

    private static string FormatValue(Chart chart, double value) {
        var formatter = chart.Options.ValueFormatter;
        if (formatter == null) return FormatNumber(value);
        return formatter(value) ?? string.Empty;
    }

    private static string FormatYAxisValue(Chart chart, double value) {
        return ChartAxisValueFormatter.Format(chart.Options.YAxis, value, chart.Options.ValueFormatter);
    }

    private static string FormatDataLabel(Chart chart, ChartSeries series, int pointIndex, double value) {
        if (pointIndex >= 0 && pointIndex < series.PointLabels.Count && series.PointLabels[pointIndex] != null) return series.PointLabels[pointIndex]!;
        return FormatValue(chart, value);
    }

    private static string SeriesSemanticRole(ChartSeries series, string fallback) =>
        string.IsNullOrWhiteSpace(series.SemanticRole) ? fallback : series.SemanticRole!;

    private static string FormatSecondaryValue(Chart chart, double value) {
        return ChartAxisValueFormatter.Format(chart.Options.SecondaryYAxis, value, chart.Options.ValueFormatter);
    }

    private static string FormatPercent(double v) => v.ToString("0.#%", CultureInfo.InvariantCulture);

    private static string SvgFontFamily(string value) => Escape(string.IsNullOrWhiteSpace(value) ? "system-ui, sans-serif" : value);

    private static string Escape(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));

    private static IReadOnlyList<double> GetXTicks(Chart chart, ChartRange range, ChartRect plot) {
        if (chart.Options.XAxisLabels.Count == 0) {
            var ticks = ChartTicks.GenerateInside(chart.Options.XAxis, range.MinX, range.MaxX);
            if (chart.Options.XAxisLabelDensity == ChartLabelDensity.All || ticks.Count < 3) return ticks;
            var generatedLabels = ticks.Select(tick => new ChartAxisLabel(tick, FormatXAxisValue(chart, tick))).ToArray();
            return SelectXAxisTickValues(chart, range, plot, generatedLabels);
        }

        var labels = chart.Options.XAxisLabels
            .Where(label => label.Value >= range.MinX && label.Value <= range.MaxX)
            .OrderBy(label => label.Value)
            .ToArray();
        return SelectXAxisTickValues(chart, range, plot, labels);
    }

    private static IReadOnlyList<double> SelectXAxisTickValues(Chart chart, ChartRange range, ChartRect plot, IReadOnlyList<ChartAxisLabel> labels) {
        if (chart.Options.XAxisLabelDensity == ChartLabelDensity.All || labels.Count < 3) return labels.Select(label => label.Value).ToArray();
        var style = chart.Options.TickLabelStyle;
        var fontSize = StyleFontSize(style, chart.Options.Theme.TickLabelFontSize);
        var widest = labels.Max(label => EstimateSvgStyledTextWidth(chart, label.Text, fontSize, style));
        var densityFactor = chart.Options.XAxisLabelDensity == ChartLabelDensity.Dense ? 0.72 : chart.Options.XAxisLabelDensity == ChartLabelDensity.Relaxed ? 1.35 : 1.0;
        var minSpacing = Math.Max(28, (widest + 18) * densityFactor);
        var maxCount = Math.Max(2, (int)Math.Floor(plot.Width / minSpacing) + 1);
        if (labels.Count <= maxCount && LabelsHaveMinimumLabelGap(chart, labels, range, plot, chart.Options.XAxis, fontSize, style, 6)) return labels.Select(label => label.Value).ToArray();

        var lastLabel = labels[labels.Count - 1];
        var step = Math.Max(1, (int)Math.Ceiling((labels.Count - 1) / (double)(maxCount - 1)));
        var selected = new List<ChartAxisLabel>();
        selected.Add(labels[0]);
        for (var i = step; i < labels.Count - 1; i += step) {
            if (LabelGap(chart, selected[selected.Count - 1], labels[i], range, plot, chart.Options.XAxis, fontSize, style) >= 6 &&
                LabelGap(chart, labels[i], lastLabel, range, plot, chart.Options.XAxis, fontSize, style) >= 6) selected.Add(labels[i]);
        }

        if (selected.Count > 1 && LabelGap(chart, selected[selected.Count - 1], lastLabel, range, plot, chart.Options.XAxis, fontSize, style) < 6) selected.RemoveAt(selected.Count - 1);
        selected.Add(lastLabel);
        return selected.Select(label => label.Value).ToArray();
    }

    private static bool LabelsHaveMinimumLabelGap(Chart chart, IReadOnlyList<ChartAxisLabel> labels, ChartRange range, ChartRect plot, ChartAxis axis, double fontSize, TextStyleOverride style, double minGap) {
        for (var i = 1; i < labels.Count; i++) {
            if (LabelGap(chart, labels[i - 1], labels[i], range, plot, axis, fontSize, style) < minGap) return false;
        }

        return true;
    }

    private static double LabelGap(Chart chart, ChartAxisLabel left, ChartAxisLabel right, ChartRange range, ChartRect plot, ChartAxis axis, double fontSize, TextStyleOverride style) {
        var leftWidth = EstimateSvgStyledTextWidth(chart, left.Text, fontSize, style);
        var rightWidth = EstimateSvgStyledTextWidth(chart, right.Text, fontSize, style);
        var leftX = Clamp(ProjectX(left.Value, range, plot, axis) - leftWidth / 2.0, plot.Left + 2, plot.Right - leftWidth - 2);
        var rightX = Clamp(ProjectX(right.Value, range, plot, axis) - rightWidth / 2.0, plot.Left + 2, plot.Right - rightWidth - 2);
        return rightX - (leftX + leftWidth);
    }

    private static IReadOnlyList<double> GetHorizontalCategoryTicks(Chart chart, ChartRange range) {
        var categories = new SortedSet<double>();
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.HorizontalBar) continue;
            foreach (var point in series.Points) {
                if (point.X >= range.MinY && point.X <= range.MaxY) categories.Add(point.X);
            }
        }

        if (categories.Count > 0) return categories.ToArray();
        return ChartTicks.GenerateInside(chart.Options.YAxis, range.MinY, range.MaxY);
    }

    private static double ProjectX(double value, ChartRange range, ChartRect plot, ChartAxis axis) {
        return plot.Left + ChartScaleTransform.Normalize(value, range.MinX, range.MaxX, axis) * plot.Width;
    }

    private static string FormatX(Chart chart, double value) {
        foreach (var label in chart.Options.XAxisLabels) {
            if (Math.Abs(label.Value - value) < 0.000001) return label.Text;
        }

        return FormatXAxisValue(chart, value);
    }

    private static string FormatXAxisValue(Chart chart, double value) {
        var formatter = chart.Options.XAxisValueFormatter;
        if (formatter == null && chart.Options.XAxis.Scale == ChartScaleKind.Time) return ChartTimeScale.Format(chart.Options.XAxis, value);
        if (formatter == null) return FormatNumber(value);
        return formatter(value) ?? string.Empty;
    }

    private static string BuildLinePath(IReadOnlyList<ChartPoint> points, bool smooth) {
        return BuildPath(ChartPathBuilder.FromPoints(points, ChartSeriesKind.Line, smooth));
    }

    private static string BuildStepLinePath(IReadOnlyList<ChartPoint> points) {
        return BuildPath(ChartPathBuilder.FromPoints(points, ChartSeriesKind.StepLine, false));
    }

    private static string BuildPath(ChartPath chartPath) {
        if (chartPath.Commands.Count == 0) return string.Empty;
        var sb = new StringBuilder();
        foreach (var command in chartPath.Commands) {
            if (command.Kind == ChartPathCommandKind.MoveTo) {
                sb.Append("M ").Append(F(command.X)).Append(' ').Append(F(command.Y));
            } else if (command.Kind == ChartPathCommandKind.LineTo) {
                sb.Append(" L ").Append(F(command.X)).Append(' ').Append(F(command.Y));
            } else if (command.Kind == ChartPathCommandKind.CubicTo) {
                sb.Append(" C ").Append(F(command.Control1X)).Append(' ').Append(F(command.Control1Y)).Append(' ')
                    .Append(F(command.Control2X)).Append(' ').Append(F(command.Control2Y)).Append(' ')
                    .Append(F(command.X)).Append(' ').Append(F(command.Y));
            }
        }

        return sb.ToString();
    }

    private static int VisualPointCount(ChartSeries series) {
        var tupleSize = VisualTupleSize(series.Kind);
        return tupleSize <= 1 ? series.Points.Count : series.Points.Count / tupleSize;
    }

    private static int VisualPointRawIndex(ChartSeries series, int pointIndex) {
        var tupleSize = VisualTupleSize(series.Kind);
        return tupleSize <= 1 ? pointIndex : pointIndex * tupleSize;
    }

    private static int VisualTupleSize(ChartSeriesKind kind) =>
        kind == ChartSeriesKind.Bubble || kind == ChartSeriesKind.RangeBand || kind == ChartSeriesKind.RangeArea || kind == ChartSeriesKind.RangeBar || kind == ChartSeriesKind.Dumbbell
            ? 2
            : kind == ChartSeriesKind.ErrorBar
                ? 3
                : kind == ChartSeriesKind.Candlestick || kind == ChartSeriesKind.Ohlc
                    ? 4
                    : kind == ChartSeriesKind.BoxPlot
                        ? 5
                        : 1;

    private static string LegendPointLabel(Chart chart, ChartPoint point, int index) {
        foreach (var label in chart.Options.XAxisLabels) {
            if (Math.Abs(label.Value - point.X) < 0.000001) return label.Text;
        }

        return "Item " + (index + 1).ToString(CultureInfo.InvariantCulture);
    }

    private readonly struct BarLayoutInfo {
        public BarLayoutInfo(double barWidth, double offset) {
            BarWidth = barWidth;
            Offset = offset;
        }

        public double BarWidth { get; }

        public double Offset { get; }
    }

    private readonly struct HorizontalBarLayoutInfo {
        public HorizontalBarLayoutInfo(double barHeight, double offset) {
            BarHeight = barHeight;
            Offset = offset;
        }

        public double BarHeight { get; }

        public double Offset { get; }
    }

    private readonly struct LabelPillPlacement {
        public LabelPillPlacement(double x, double rectX, string anchor) {
            X = x;
            RectX = rectX;
            Anchor = anchor;
        }

        public double X { get; }

        public double RectX { get; }

        public string Anchor { get; }
    }
}
