using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    internal Type DatumType => IsCategoricalHeatmapRow ? typeof(ChartHeatmapCell?)
        : IsTypedWaterfall ? typeof(ChartWaterfallItem) : IsTypedProgress ? typeof(ChartProgressItem)
        : Kind switch {
            ChartSeriesKind.Sunburst or ChartSeriesKind.Treemap => typeof(ChartHierarchyItem),
            ChartSeriesKind.Tree or ChartSeriesKind.Sankey or ChartSeriesKind.Chord => typeof(ChartNode),
            ChartSeriesKind.DottedMap => typeof(ChartMapPoint),
            ChartSeriesKind.CalendarHeatmap => typeof(ChartCalendarHeatmapItem),
            ChartSeriesKind.RegionMap or ChartSeriesKind.TileMap => typeof(ChartRegionMapItem),
            ChartSeriesKind.Bubble => typeof(ChartBubble),
            ChartSeriesKind.Candlestick or ChartSeriesKind.Ohlc => typeof(ChartCandlestick),
            ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea => typeof(ChartRangeBand),
            ChartSeriesKind.RangeBar => typeof(ChartInterval),
            ChartSeriesKind.ErrorBar => typeof(ChartErrorBar),
            ChartSeriesKind.BoxPlot => typeof(ChartBoxPlot),
            ChartSeriesKind.Dumbbell => typeof(ChartDumbbell),
            ChartSeriesKind.DifferenceArea => typeof(ChartDifferencePoint),
            ChartSeriesKind.Gantt => typeof(ChartGanttItem),
            ChartSeriesKind.StateTimeline => typeof(ChartStateTimelineSegment),
            ChartSeriesKind.GanttLane => typeof(ChartGanttLaneItem),
            ChartSeriesKind.Gauge or ChartSeriesKind.Circle => typeof(ChartGaugeValue),
            ChartSeriesKind.Bullet => typeof(ChartBulletValue),
            ChartSeriesKind.LayeredRadial => typeof(ChartRadialLayer),
            _ => typeof(ChartPoint)
        };

    internal object? ObservationDatum(int index) {
        if (IsCategoricalHeatmapRow) return HeatmapCells[index];
        if (IsTypedWaterfall) return WaterfallItems[index];
        if (IsTypedProgress) return ProgressItems[index];
        return Kind switch {
            ChartSeriesKind.Sunburst or ChartSeriesKind.Treemap => HierarchyItems[index],
            ChartSeriesKind.Tree or ChartSeriesKind.Sankey or ChartSeriesKind.Chord => Nodes[index],
            ChartSeriesKind.DottedMap => MapPoints[index],
            ChartSeriesKind.CalendarHeatmap => CalendarItems[index],
            ChartSeriesKind.RegionMap or ChartSeriesKind.TileMap => RegionValues[index],
            ChartSeriesKind.Bubble => Bubbles[index],
            ChartSeriesKind.Candlestick or ChartSeriesKind.Ohlc => FinancialValues[index],
            ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea => Ranges[index],
            ChartSeriesKind.RangeBar => Intervals[index],
            ChartSeriesKind.ErrorBar => ErrorBars[index],
            ChartSeriesKind.BoxPlot => BoxPlots[index],
            ChartSeriesKind.Dumbbell => Dumbbells[index],
            ChartSeriesKind.DifferenceArea => DifferencePoints[index],
            ChartSeriesKind.Gantt => GanttItems[index],
            ChartSeriesKind.StateTimeline => StateTimelineSegments[index],
            ChartSeriesKind.GanttLane => GanttLaneItems[index],
            ChartSeriesKind.Gauge or ChartSeriesKind.Circle => GaugeValues[index],
            ChartSeriesKind.Bullet => BulletValues[index],
            ChartSeriesKind.LayeredRadial => CopyRadialLayer(RadialLayers[index]),
            _ => Points[index]
        };
    }
}
