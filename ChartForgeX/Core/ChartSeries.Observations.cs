namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    internal bool HasSourceData => AuthoredObservationCount > 0;

    // Encoded numeric families retain their existing footprint metric; typed input counts authored items.
    internal int RenderedPointCount => WaterfallItems.Count > 0 ? AuthoredObservationCount : Points.Count;

    // Point overrides and keys use logical observations, not encoded tuple members or derived extra marks.
    internal int AuthoredObservationCount {
        get {
            if (IsRelationshipKind(Kind)) return Nodes.Count;
            if (WaterfallItems.Count > 0) return WaterfallItems.Count;
            var tupleSize = Kind == ChartSeriesKind.Bubble || Kind == ChartSeriesKind.RangeBand || Kind == ChartSeriesKind.RangeArea
                || Kind == ChartSeriesKind.RangeBar || Kind == ChartSeriesKind.Dumbbell ? 2
                : Kind == ChartSeriesKind.ErrorBar ? 3
                : Kind == ChartSeriesKind.Candlestick || Kind == ChartSeriesKind.Ohlc ? 4
                : Kind == ChartSeriesKind.BoxPlot ? 5 : 1;
            return Points.Count / tupleSize;
        }
    }
}
