namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    internal bool HasSourceData => AuthoredObservationCount > 0;

    internal int RenderedPointCount => ObservationCount;

    /// <summary>Gets the number of authored display observations, independent of their number of numeric fields or derived marks.</summary>
    public int ObservationCount => AuthoredObservationCount;

    // Point overrides and keys use logical observations, not encoded tuple members or derived extra marks.
    internal int AuthoredObservationCount {
        get {
            if (IsRelationshipKind(Kind)) return Nodes.Count;
            if (IsCategoricalHeatmapRow) return HeatmapCells.Count;
            if (WaterfallItems.Count > 0) return WaterfallItems.Count;
            return Kind switch {
                ChartSeriesKind.Bubble => Bubbles.Count,
                ChartSeriesKind.Candlestick or ChartSeriesKind.Ohlc => FinancialValues.Count,
                ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea => Ranges.Count,
                ChartSeriesKind.RangeBar => Intervals.Count,
                ChartSeriesKind.ErrorBar => ErrorBars.Count,
                ChartSeriesKind.BoxPlot => BoxPlots.Count,
                ChartSeriesKind.Dumbbell => Dumbbells.Count,
                ChartSeriesKind.DifferenceArea => DifferencePoints.Count,
                ChartSeriesKind.Gantt => GanttItems.Count,
                ChartSeriesKind.StateTimeline => StateTimelineSegments.Count,
                ChartSeriesKind.GanttLane => GanttLaneItems.Count,
                ChartSeriesKind.Gauge or ChartSeriesKind.Circle => GaugeValues.Count,
                ChartSeriesKind.Bullet => BulletValues.Count,
                ChartSeriesKind.LayeredRadial => RadialLayers.Count,
                _ => Points.Count
            };
        }
    }
}
