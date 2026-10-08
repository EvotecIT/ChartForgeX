using System;
using System.Linq;
using ChartForgeX.Core;

public static partial class V2GalleryModels {
    private static Chart? Ranges(ChartSeriesKind kind, string variant) {
        var chart = Categories(); var count = variant == "sparse" ? 2 : 5;
        var candles = Enumerable.Range(1, count).Select(index => new ChartCandlestick(index, 30 + index * 3, 44 + index * 3, 20 + index * 3, index % 2 == 0 ? 26 + index * 3 : 38 + index * 3)).ToArray();
        var ranges = Enumerable.Range(1, count).Select(index => new ChartRangeBand(index, 20 + index * 2, 40 + index * 4)).ToArray();
        return kind switch {
            ChartSeriesKind.Bubble => chart.AddBubble("Requests", Enumerable.Range(1, count).Select(index => new ChartBubble(index, 20 + index * 5, index * index + 1))),
            ChartSeriesKind.ErrorBar => chart.AddErrorBar("Delivery time", Enumerable.Range(1, count).Select(index => new ChartErrorBar(index, 30 + index * 2, 20 + index, 42 + index * 3))),
            ChartSeriesKind.Candlestick => chart.AddCandlestick("Market price", candles),
            ChartSeriesKind.Ohlc => chart.AddOhlc("Market price", candles),
            ChartSeriesKind.RangeBand => chart.AddRangeBand("Expected range", ranges),
            ChartSeriesKind.RangeArea => chart.AddRangeArea("Expected range", ranges),
            ChartSeriesKind.Dumbbell => chart.AddDumbbell("Change", Enumerable.Range(1, count).Select(index => new ChartDumbbell(index, 20 + index * 2, 40 + index * 3))),
            ChartSeriesKind.RangeBar => chart.AddRangeBar("Range", Enumerable.Range(1, count).Select(index => new ChartInterval(index, 20 + index * 2, 40 + index * 4))),
            ChartSeriesKind.BoxPlot => chart.AddBoxPlot("Delivery time", Enumerable.Range(1, count).Select(index => new ChartBoxPlot(index, 10 + index, 20 + index, 30 + index, 38 + index, 50 + index))),
            _ => null
        };
    }
}
