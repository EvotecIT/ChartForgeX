using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Core;

internal sealed partial class ChartVisibilitySource {
    /// <summary>Copied authored rows and sparse policies; factories and canonical writing consume these same facts.</summary>
    internal sealed partial class Series {
        private readonly TextStyleOverride _labelStyle;
        private readonly ChartMarkerOptions _markers;
        private readonly TextStyleOverride?[] _pointStyles;
        private readonly IReadOnlyDictionary<int, ChartObservationId> _ids, _groups;
        private readonly ChartBoxPlotStatisticsOptions? _statistics;
        private readonly ChartFinancialOptions _financial;
        private readonly IReadOnlyList<ChartFinancialItemOptions?> _financialStyles;
        internal Series(ChartSeries source) {
            Id = source.Id; Name = source.Name; Kind = source.Kind; Color = source.Color; State = source.StateRole; Pattern = source.FillPattern;
            ValueAxis = source.ValueAxis; ShowInLegend = source.ShowInLegend; SemanticRole = source.SemanticRole; Labels = source.ShowDataLabels; Placement = source.DataLabelPlacement;
            StrokeWidth = source.HasExplicitStrokeWidth ? source.StrokeWidth : null; Interpolation = source.Interpolation; Step = source.StepPosition;
            StackGroup = source.StackGroup; NormalizedTo = source.NormalizedTo; StackedAreaMissingData = source.StackedAreaMissingData;
            _labelStyle = source.DataLabelStyle.Clone(); _markers = source.Markers.Clone();
            CaptureHierarchy(source);
            Points = Array.AsReadOnly(source.Points.ToArray()); Boxes = Array.AsReadOnly(source.BoxPlots.ToArray());
            Bubbles = Array.AsReadOnly(source.Bubbles.ToArray()); Ranges = Array.AsReadOnly(source.Ranges.ToArray());
            ErrorBars = Array.AsReadOnly(source.ErrorBars.ToArray());
            FinancialValues = Array.AsReadOnly(source.FinancialValues.ToArray());
            _financial = source.Financial.Clone(); _financialStyles = source.CaptureFinancialStyles();
            var errorOffset = 0; ErrorOffsets = Array.AsReadOnly(ErrorBars.Select(item => {
                var captured = errorOffset; errorOffset += ErrorValueCount(item); return captured;
            }).ToArray());
            RawGroups = Array.AsReadOnly(source.BoxPlotSourceGroups.Select(group => new ChartBoxPlotSampleGroup(group.X, group.Samples)).ToArray());
            RawSamples = Array.AsReadOnly(source.BoxPlotSourceSamples.ToArray());
            SliceOffsets = Array.AsReadOnly(source.PointSliceOffsets.ToArray()); SliceRadiusRatios = Array.AsReadOnly(source.PointSliceRadiusRatios.ToArray()); RadialBand = source.RadialBand;
            if (source.BoxPlotQuartileEstimator.HasValue) _statistics = new ChartBoxPlotStatisticsOptions { WhiskerPolicy = source.BoxPlotWhiskerPolicy!.Value, IqrMultiplier = source.BoxPlotIqrMultiplier!.Value };
            _ids = new System.Collections.ObjectModel.ReadOnlyDictionary<int, ChartObservationId>(source.ObservationIds.ToDictionary(pair => pair.Key, pair => pair.Value));
            _groups = new System.Collections.ObjectModel.ReadOnlyDictionary<int, ChartObservationId>(source.BoxPlotGroupIds.ToDictionary(pair => pair.Key, pair => pair.Value));
            PointColors = Array.AsReadOnly(source.PointColors.ToArray()); PointPatterns = Array.AsReadOnly(source.PointFillPatterns.ToArray()); PointLabels = Array.AsReadOnly(source.PointLabels.ToArray());
            _pointStyles = source.PointDataLabelStyles.Select(style => style?.Clone()).ToArray();
        }
        internal ChartSeriesId Id { get; }
        internal string Name { get; }
        internal ChartSeriesKind Kind { get; }
        internal ChartColor? Color { get; }
        internal ChartSeriesState State { get; }
        internal ChartFillPattern Pattern { get; }
        internal ChartAxisSide ValueAxis { get; }
        internal bool ShowInLegend { get; }
        internal string? SemanticRole { get; }
        internal bool? Labels { get; }
        internal ChartDataLabelPlacement? Placement { get; }
        internal double? StrokeWidth { get; }
        internal ChartInterpolation Interpolation { get; }
        internal ChartStepPosition Step { get; }
        internal string? StackGroup { get; }
        internal double? NormalizedTo { get; }
        internal ChartStackedAreaMissingDataPolicy StackedAreaMissingData { get; }
        internal IReadOnlyList<ChartPoint> Points { get; }
        internal IReadOnlyList<ChartBubble> Bubbles { get; }
        internal IReadOnlyList<ChartRangeBand> Ranges { get; }
        internal IReadOnlyList<ChartErrorBar> ErrorBars { get; }
        internal IReadOnlyList<ChartCandlestick> FinancialValues { get; }
        internal ChartFinancialOptions CopyFinancial() => _financial.Clone();
        internal IReadOnlyList<ChartFinancialItemOptions?> CopyFinancialStyles() => Array.AsReadOnly(_financialStyles.Select(style => style?.Clone()).ToArray());
        internal IReadOnlyList<int> ErrorOffsets { get; }
        private static int ErrorValueCount(ChartErrorBar item) => 1 + (item.XBounds.HasValue ? 2 : 0) + (item.YBounds.HasValue ? 2 : 0);
        internal int NativeValueCount => IsHierarchy ? HierarchyItems.Count(item => item.Value.HasValue) : Kind == ChartSeriesKind.Bubble ? Bubbles.Count * 2 : Kind is ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea ? Ranges.Count * 2 : Kind == ChartSeriesKind.ErrorBar ? ErrorBars.Sum(ErrorValueCount) : Kind is ChartSeriesKind.Candlestick or ChartSeriesKind.Ohlc ? FinancialValues.Count * 4 : Points.Count;
        internal IReadOnlyList<ChartBoxPlot> Boxes { get; }
        internal IReadOnlyList<ChartBoxPlotSampleGroup> RawGroups { get; }
        internal IReadOnlyList<double> RawSamples { get; }
        internal IReadOnlyList<double> SliceOffsets { get; }
        internal IReadOnlyList<double?> SliceRadiusRatios { get; }
        internal ChartRadialBand? RadialBand { get; }
        internal bool Raw => _statistics != null;
        internal IReadOnlyDictionary<int, ChartObservationId> ObservationIds => _ids;
        internal IReadOnlyDictionary<int, ChartObservationId> GroupIds => _groups;
        internal IReadOnlyList<ChartColor?> PointColors { get; }
        internal IReadOnlyList<ChartFillPattern?> PointPatterns { get; }
        internal IReadOnlyList<string?> PointLabels { get; }
        internal int PointStyleCount => _pointStyles.Length;
        internal TextStyleOverride? CopyPointStyle(int index) => index < _pointStyles.Length ? _pointStyles[index]?.Clone() : null;
        internal TextStyleOverride CopyLabelStyle() => _labelStyle.Clone();
        internal ChartMarkerOptions CopyMarkers() => _markers.Clone();
        internal ChartBoxPlotStatisticsOptions? CopyStatistics() => _statistics?.Clone();
        internal IReadOnlyList<int> VisibleIndices(ChartVisibilityView? view) {
            if (IsHierarchy) return Array.AsReadOnly(Enumerable.Range(0, HierarchyItems.Count).ToArray());
            if (view?.IsHidden(Id.Value) == true) return Array.Empty<int>();
            var ids = Raw ? GroupIds : ObservationIds; var kind = Raw ? ChartVisibilityTargetKind.BoxPlotGroup : ChartVisibilityTargetKind.Observation;
            return Array.AsReadOnly(Enumerable.Range(0, DisplayCount).Where(index => view == null || !ids.TryGetValue(index, out var id)
                || !view.IsHidden(kind, Id, id)).ToArray());
        }
        internal int SourceCount => Raw ? RawSamples.Count : Kind == ChartSeriesKind.BoxPlot ? Boxes.Count : Kind == ChartSeriesKind.Bubble ? Bubbles.Count : Kind is ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea ? Ranges.Count : Kind == ChartSeriesKind.ErrorBar ? ErrorBars.Count : Kind is ChartSeriesKind.Candlestick or ChartSeriesKind.Ohlc ? FinancialValues.Count : Points.Count;
        internal int DisplayCount => IsHierarchy ? HierarchyItems.Count : Raw ? RawGroups.Count : Kind == ChartSeriesKind.BoxPlot ? Boxes.Count : Kind == ChartSeriesKind.Bubble ? Bubbles.Count : Kind is ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea ? Ranges.Count : Kind == ChartSeriesKind.ErrorBar ? ErrorBars.Count : Kind is ChartSeriesKind.Candlestick or ChartSeriesKind.Ohlc ? FinancialValues.Count : Points.Count;
    }
}
