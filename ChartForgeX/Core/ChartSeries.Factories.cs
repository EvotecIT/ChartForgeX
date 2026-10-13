using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Creates an independently configurable line series with an explicit stable ID and numeric observations.</summary>
    public static ChartSeries CreateLine(ChartSeriesId id, string name, IEnumerable<ChartPoint> points, ChartColor? color = null) =>
        new(id, name, ChartSeriesKind.Line, points) { Color = color };

    /// <summary>Creates an independently configurable bar series with an explicit stable ID and numeric observations.</summary>
    public static ChartSeries CreateBar(ChartSeriesId id, string name, IEnumerable<ChartPoint> points, ChartColor? color = null) =>
        new(id, name, ChartSeriesKind.Bar, points) { Color = color };

    /// <summary>Creates an independently configurable area series with an explicit stable ID and numeric observations.</summary>
    public static ChartSeries CreateArea(ChartSeriesId id, string name, IEnumerable<ChartPoint> points, ChartColor? color = null) =>
        new(id, name, ChartSeriesKind.Area, points) { Color = color };

    /// <summary>Creates an independently configurable scatter series with an explicit stable ID and numeric observations.</summary>
    public static ChartSeries CreateScatter(ChartSeriesId id, string name, IEnumerable<ChartPoint> points, ChartColor? color = null) =>
        new(id, name, ChartSeriesKind.Scatter, points) { Color = color };

    /// <summary>Creates an independently configurable bubble series with an explicit stable ID and one typed item per observation.</summary>
    public static ChartSeries CreateBubble(ChartSeriesId id, string name, IEnumerable<ChartBubble> values, ChartColor? color = null) {
        var series = new ChartSeries(id, name, ChartSeriesKind.Bubble, Array.Empty<ChartPoint>()) { Color = color };
        series.ReplaceBubbles(values);
        return series;
    }

    internal static ChartSeries CreateBubble(string name, IEnumerable<ChartBubble> values, ChartColor? color) {
        var series = new ChartSeries(name, ChartSeriesKind.Bubble, Array.Empty<ChartPoint>()) { Color = color };
        series.ReplaceBubbles(values);
        return series;
    }

    /// <summary>Creates an independently configurable candlestick series with an explicit stable ID and one typed item per observation.</summary>
    public static ChartSeries CreateCandlestick(ChartSeriesId id, string name, IEnumerable<ChartCandlestick> values, ChartColor? color = null) {
        var series = new ChartSeries(id, name, ChartSeriesKind.Candlestick, Array.Empty<ChartPoint>()) { Color = color };
        series.ReplaceFinancialValues(values);
        return series;
    }

    internal static ChartSeries CreateCandlestick(string name, IEnumerable<ChartCandlestick> values, ChartColor? color) {
        var series = new ChartSeries(name, ChartSeriesKind.Candlestick, Array.Empty<ChartPoint>()) { Color = color };
        series.ReplaceFinancialValues(values);
        return series;
    }

    /// <summary>Creates an independently configurable ohlc series with an explicit stable ID and one typed item per observation.</summary>
    public static ChartSeries CreateOhlc(ChartSeriesId id, string name, IEnumerable<ChartCandlestick> values, ChartColor? color = null) {
        var series = new ChartSeries(id, name, ChartSeriesKind.Ohlc, Array.Empty<ChartPoint>()) { Color = color };
        series.ReplaceFinancialValues(values);
        return series;
    }

    internal static ChartSeries CreateOhlc(string name, IEnumerable<ChartCandlestick> values, ChartColor? color) {
        var series = new ChartSeries(name, ChartSeriesKind.Ohlc, Array.Empty<ChartPoint>()) { Color = color };
        series.ReplaceFinancialValues(values);
        return series;
    }

    /// <summary>Creates an independently configurable rangeband series with an explicit stable ID and one typed item per observation.</summary>
    public static ChartSeries CreateRangeBand(ChartSeriesId id, string name, IEnumerable<ChartRangeBand> values, ChartColor? color = null) {
        var series = new ChartSeries(id, name, ChartSeriesKind.RangeBand, Array.Empty<ChartPoint>()) { Color = color };
        series.ReplaceRanges(values);
        return series;
    }

    internal static ChartSeries CreateRangeBand(string name, IEnumerable<ChartRangeBand> values, ChartColor? color) {
        var series = new ChartSeries(name, ChartSeriesKind.RangeBand, Array.Empty<ChartPoint>()) { Color = color };
        series.ReplaceRanges(values);
        return series;
    }

    /// <summary>Creates an independently configurable rangearea series with an explicit stable ID and one typed item per observation.</summary>
    public static ChartSeries CreateRangeArea(ChartSeriesId id, string name, IEnumerable<ChartRangeBand> values, ChartColor? color = null, bool smooth = true) {
        var series = new ChartSeries(id, name, ChartSeriesKind.RangeArea, Array.Empty<ChartPoint>()) { Color = color, Smooth = smooth };
        series.ReplaceRanges(values);
        return series;
    }

    internal static ChartSeries CreateRangeArea(string name, IEnumerable<ChartRangeBand> values, ChartColor? color, bool smooth) {
        var series = new ChartSeries(name, ChartSeriesKind.RangeArea, Array.Empty<ChartPoint>()) { Color = color, Smooth = smooth };
        series.ReplaceRanges(values);
        return series;
    }

    /// <summary>Creates an independently configurable rangebar series with an explicit stable ID and one typed item per observation.</summary>
    public static ChartSeries CreateRangeBar(ChartSeriesId id, string name, IEnumerable<ChartInterval> values, ChartColor? color = null) {
        var series = new ChartSeries(id, name, ChartSeriesKind.RangeBar, Array.Empty<ChartPoint>()) { Color = color };
        series.ReplaceIntervals(values);
        return series;
    }

    internal static ChartSeries CreateRangeBar(string name, IEnumerable<ChartInterval> values, ChartColor? color) {
        var series = new ChartSeries(name, ChartSeriesKind.RangeBar, Array.Empty<ChartPoint>()) { Color = color };
        series.ReplaceIntervals(values);
        return series;
    }

    /// <summary>Creates an independently configurable errorbar series with an explicit stable ID and one typed item per observation.</summary>
    public static ChartSeries CreateErrorBar(ChartSeriesId id, string name, IEnumerable<ChartErrorBar> values, ChartColor? color = null) {
        var series = new ChartSeries(id, name, ChartSeriesKind.ErrorBar, Array.Empty<ChartPoint>()) { Color = color };
        series.ReplaceErrorBars(values);
        return series;
    }

    internal static ChartSeries CreateErrorBar(string name, IEnumerable<ChartErrorBar> values, ChartColor? color) {
        var series = new ChartSeries(name, ChartSeriesKind.ErrorBar, Array.Empty<ChartPoint>()) { Color = color };
        series.ReplaceErrorBars(values);
        return series;
    }

    /// <summary>Creates an independently configurable boxplot series with an explicit stable ID and one typed item per observation.</summary>
    public static ChartSeries CreateBoxPlot(ChartSeriesId id, string name, IEnumerable<ChartBoxPlot> values, ChartColor? color = null) {
        var series = new ChartSeries(id, name, ChartSeriesKind.BoxPlot, Array.Empty<ChartPoint>()) { Color = color };
        series.ReplaceBoxPlots(values);
        return series;
    }

    internal static ChartSeries CreateBoxPlot(string name, IEnumerable<ChartBoxPlot> values, ChartColor? color) {
        var series = new ChartSeries(name, ChartSeriesKind.BoxPlot, Array.Empty<ChartPoint>()) { Color = color };
        series.ReplaceBoxPlots(values);
        return series;
    }

    /// <summary>Creates an independently configurable dumbbell series with an explicit stable ID and one typed item per observation.</summary>
    public static ChartSeries CreateDumbbell(ChartSeriesId id, string name, IEnumerable<ChartDumbbell> values, ChartColor? color = null) {
        var series = new ChartSeries(id, name, ChartSeriesKind.Dumbbell, Array.Empty<ChartPoint>()) { Color = color };
        series.ReplaceDumbbells(values);
        return series;
    }

    internal static ChartSeries CreateDumbbell(string name, IEnumerable<ChartDumbbell> values, ChartColor? color) {
        var series = new ChartSeries(name, ChartSeriesKind.Dumbbell, Array.Empty<ChartPoint>()) { Color = color };
        series.ReplaceDumbbells(values);
        return series;
    }

    /// <summary>Creates an independently configurable differencearea series with an explicit stable ID and one typed item per observation.</summary>
    public static ChartSeries CreateDifferenceArea(ChartSeriesId id, string name, IEnumerable<ChartDifferencePoint> values, ChartColor? color = null) {
        var series = new ChartSeries(id, name, ChartSeriesKind.DifferenceArea, Array.Empty<ChartPoint>()) { Color = color };
        series.ReplaceDifferencePoints(values);
        return series;
    }

    internal static ChartSeries CreateDifferenceArea(string name, IEnumerable<ChartDifferencePoint> values, ChartColor? color) {
        var series = new ChartSeries(name, ChartSeriesKind.DifferenceArea, Array.Empty<ChartPoint>()) { Color = color };
        series.ReplaceDifferencePoints(values);
        return series;
    }

}
