using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private IReadOnlyList<ChartBubble> _bubbles = Array.Empty<ChartBubble>();
    /// <summary>Gets immutable authored bubbles, one item per observation in input order.</summary>
    public IReadOnlyList<ChartBubble> Bubbles => _bubbles;

    /// <summary>Replaces the bubbles atomically and clears per-observation keys, labels and paint overrides. Series configuration is retained.</summary>
    /// <param name="values">The complete replacement observations. An empty sequence creates a no-data series.</param>
    /// <returns>The current series.</returns>
    public ChartSeries ReplaceBubbles(IEnumerable<ChartBubble> values) {
        RequireObservationKind(ChartSeriesKind.Bubble);
        ReplaceObservations(ref _bubbles, values, ValidateBubble);
        return this;
    }

    /// <summary>Replaces one bubble while preserving its ordinal, explicit key and presentation overrides.</summary>
    /// <param name="index">The existing zero-based observation index.</param>
    /// <param name="value">The complete replacement observation.</param>
    /// <returns>The current series.</returns>
    public ChartSeries UpdateBubble(int index, ChartBubble value) {
        RequireObservationKind(ChartSeriesKind.Bubble);
        UpdateObservation(ref _bubbles, index, value, ValidateBubble);
        return this;
    }

    private static ChartBubble ValidateBubble(ChartBubble value) => new ChartBubble(value.X, value.Y, value.Size).WithColorValue(value.ColorValue);

    private IReadOnlyList<ChartCandlestick> _financialValues = Array.Empty<ChartCandlestick>();
    /// <summary>Gets immutable authored financial values, one item per observation in input order.</summary>
    public IReadOnlyList<ChartCandlestick> FinancialValues => _financialValues;

    /// <summary>Replaces the financial values atomically and clears per-observation keys, labels and paint overrides. Series configuration is retained.</summary>
    /// <param name="values">The complete replacement observations. An empty sequence creates a no-data series.</param>
    /// <returns>The current series.</returns>
    public ChartSeries ReplaceFinancialValues(IEnumerable<ChartCandlestick> values) {
        RequireObservationKind(ChartSeriesKind.Candlestick, ChartSeriesKind.Ohlc);
        ReplaceObservations(ref _financialValues, values, ValidateFinancialValue);
        return this;
    }

    /// <summary>Replaces one financial value while preserving its ordinal, explicit key and presentation overrides.</summary>
    /// <param name="index">The existing zero-based observation index.</param>
    /// <param name="value">The complete replacement observation.</param>
    /// <returns>The current series.</returns>
    public ChartSeries UpdateFinancialValue(int index, ChartCandlestick value) {
        RequireObservationKind(ChartSeriesKind.Candlestick, ChartSeriesKind.Ohlc);
        UpdateObservation(ref _financialValues, index, value, ValidateFinancialValue);
        return this;
    }

    private static ChartCandlestick ValidateFinancialValue(ChartCandlestick value) => new ChartCandlestick(value.X, value.Open, value.High, value.Low, value.Close);

    private IReadOnlyList<ChartRangeBand> _ranges = Array.Empty<ChartRangeBand>();
    /// <summary>Gets immutable authored ranges, one item per observation in input order.</summary>
    public IReadOnlyList<ChartRangeBand> Ranges => _ranges;

    /// <summary>Replaces the ranges atomically and clears per-observation keys, labels and paint overrides. Series configuration is retained.</summary>
    /// <param name="values">The complete replacement observations. An empty sequence creates a no-data series.</param>
    /// <returns>The current series.</returns>
    public ChartSeries ReplaceRanges(IEnumerable<ChartRangeBand> values) {
        RequireObservationKind(ChartSeriesKind.RangeBand, ChartSeriesKind.RangeArea);
        ReplaceObservations(ref _ranges, values, ValidateRange);
        return this;
    }

    /// <summary>Replaces one range while preserving its ordinal, explicit key and presentation overrides.</summary>
    /// <param name="index">The existing zero-based observation index.</param>
    /// <param name="value">The complete replacement observation.</param>
    /// <returns>The current series.</returns>
    public ChartSeries UpdateRange(int index, ChartRangeBand value) {
        RequireObservationKind(ChartSeriesKind.RangeBand, ChartSeriesKind.RangeArea);
        UpdateObservation(ref _ranges, index, value, ValidateRange);
        return this;
    }

    private static ChartRangeBand ValidateRange(ChartRangeBand value) => new ChartRangeBand(value.X, value.Lower, value.Upper, value.BreakBefore);

    private IReadOnlyList<ChartInterval> _intervals = Array.Empty<ChartInterval>();
    /// <summary>Gets immutable authored intervals, one item per observation in input order.</summary>
    public IReadOnlyList<ChartInterval> Intervals => _intervals;

    /// <summary>Replaces the intervals atomically and clears per-observation keys, labels and paint overrides. Series configuration is retained.</summary>
    /// <param name="values">The complete replacement observations. An empty sequence creates a no-data series.</param>
    /// <returns>The current series.</returns>
    public ChartSeries ReplaceIntervals(IEnumerable<ChartInterval> values) {
        RequireObservationKind(ChartSeriesKind.RangeBar);
        ReplaceObservations(ref _intervals, values, ValidateInterval);
        return this;
    }

    /// <summary>Replaces one interval while preserving its ordinal, explicit key and presentation overrides.</summary>
    /// <param name="index">The existing zero-based observation index.</param>
    /// <param name="value">The complete replacement observation.</param>
    /// <returns>The current series.</returns>
    public ChartSeries UpdateInterval(int index, ChartInterval value) {
        RequireObservationKind(ChartSeriesKind.RangeBar);
        UpdateObservation(ref _intervals, index, value, ValidateInterval);
        return this;
    }

    private static ChartInterval ValidateInterval(ChartInterval value) => new ChartInterval(value.X, value.Start, value.End);

    private IReadOnlyList<ChartErrorBar> _errorBars = Array.Empty<ChartErrorBar>();
    /// <summary>Gets immutable authored error bars, one item per observation in input order.</summary>
    public IReadOnlyList<ChartErrorBar> ErrorBars => _errorBars;

    /// <summary>Replaces the error bars atomically and clears per-observation keys, labels and paint overrides. Series configuration is retained.</summary>
    /// <param name="values">The complete replacement observations. An empty sequence creates a no-data series.</param>
    /// <returns>The current series.</returns>
    public ChartSeries ReplaceErrorBars(IEnumerable<ChartErrorBar> values) {
        RequireObservationKind(ChartSeriesKind.ErrorBar);
        ReplaceObservations(ref _errorBars, values, ValidateErrorBar);
        return this;
    }

    /// <summary>Replaces one error bar while preserving its ordinal, explicit key and presentation overrides.</summary>
    /// <param name="index">The existing zero-based observation index.</param>
    /// <param name="value">The complete replacement observation.</param>
    /// <returns>The current series.</returns>
    public ChartSeries UpdateErrorBar(int index, ChartErrorBar value) {
        RequireObservationKind(ChartSeriesKind.ErrorBar);
        UpdateObservation(ref _errorBars, index, value, ValidateErrorBar);
        return this;
    }

    private static ChartErrorBar ValidateErrorBar(ChartErrorBar value) => new ChartErrorBar(value.X, value.Y, value.XBounds, value.YBounds);

    private IReadOnlyList<ChartBoxPlot> _boxPlots = Array.Empty<ChartBoxPlot>();
    /// <summary>Gets immutable authored box plots, one item per observation in input order.</summary>
    public IReadOnlyList<ChartBoxPlot> BoxPlots => _boxPlots;

    /// <summary>Replaces the box plots atomically and clears per-observation keys, labels and paint overrides. Series configuration is retained.</summary>
    /// <param name="values">The complete replacement observations. An empty sequence creates a no-data series.</param>
    /// <returns>The current series.</returns>
    public ChartSeries ReplaceBoxPlots(IEnumerable<ChartBoxPlot> values) {
        RequireObservationKind(ChartSeriesKind.BoxPlot);
        ReplaceObservations(ref _boxPlots, values, ValidateBoxPlot);
        return this;
    }

    /// <summary>Replaces one box plot while preserving its ordinal, explicit key and presentation overrides.</summary>
    /// <param name="index">The existing zero-based observation index.</param>
    /// <param name="value">The complete replacement observation.</param>
    /// <returns>The current series.</returns>
    public ChartSeries UpdateBoxPlot(int index, ChartBoxPlot value) {
        RequireObservationKind(ChartSeriesKind.BoxPlot);
        var wasRaw = _isRawBoxPlot;
        UpdateObservation(ref _boxPlots, index, value, ValidateBoxPlot);
        if (wasRaw) ClearObservationKeys();
        return this;
    }

    private static ChartBoxPlot ValidateBoxPlot(ChartBoxPlot value) => new ChartBoxPlot(value.X, value.Minimum, value.Q1, value.Median, value.Q3, value.Maximum);

    private IReadOnlyList<ChartDumbbell> _dumbbells = Array.Empty<ChartDumbbell>();
    /// <summary>Gets immutable authored dumbbells, one item per observation in input order.</summary>
    public IReadOnlyList<ChartDumbbell> Dumbbells => _dumbbells;

    /// <summary>Replaces the dumbbells atomically and clears per-observation keys, labels and paint overrides. Series configuration is retained.</summary>
    /// <param name="values">The complete replacement observations. An empty sequence creates a no-data series.</param>
    /// <returns>The current series.</returns>
    public ChartSeries ReplaceDumbbells(IEnumerable<ChartDumbbell> values) {
        RequireObservationKind(ChartSeriesKind.Dumbbell);
        ReplaceObservations(ref _dumbbells, values, ValidateDumbbell);
        return this;
    }

    /// <summary>Replaces one dumbbell while preserving its ordinal, explicit key and presentation overrides.</summary>
    /// <param name="index">The existing zero-based observation index.</param>
    /// <param name="value">The complete replacement observation.</param>
    /// <returns>The current series.</returns>
    public ChartSeries UpdateDumbbell(int index, ChartDumbbell value) {
        RequireObservationKind(ChartSeriesKind.Dumbbell);
        UpdateObservation(ref _dumbbells, index, value, ValidateDumbbell);
        return this;
    }

    private static ChartDumbbell ValidateDumbbell(ChartDumbbell value) => new ChartDumbbell(value.X, value.Start, value.End);

    private IReadOnlyList<ChartDifferencePoint> _differencePoints = Array.Empty<ChartDifferencePoint>();
    /// <summary>Gets immutable authored difference points, one item per observation in input order.</summary>
    public IReadOnlyList<ChartDifferencePoint> DifferencePoints => _differencePoints;

    /// <summary>Replaces the difference points atomically and clears per-observation keys, labels and paint overrides. Series configuration is retained.</summary>
    /// <param name="values">The complete replacement observations. An empty sequence creates a no-data series.</param>
    /// <returns>The current series.</returns>
    public ChartSeries ReplaceDifferencePoints(IEnumerable<ChartDifferencePoint> values) {
        RequireObservationKind(ChartSeriesKind.DifferenceArea);
        ReplaceObservations(ref _differencePoints, values, ValidateDifferencePoint);
        _differenceAlignment = null;
        return this;
    }

    /// <summary>Replaces one difference point while preserving its ordinal, explicit key and presentation overrides.</summary>
    /// <param name="index">The existing zero-based observation index.</param>
    /// <param name="value">The complete replacement observation.</param>
    /// <returns>The current series.</returns>
    public ChartSeries UpdateDifferencePoint(int index, ChartDifferencePoint value) {
        RequireObservationKind(ChartSeriesKind.DifferenceArea);
        RequireDirectDifferencePoints();
        UpdateObservation(ref _differencePoints, index, value, ValidateDifferencePoint);
        return this;
    }

    private static ChartDifferencePoint ValidateDifferencePoint(ChartDifferencePoint value) => new ChartDifferencePoint(value.X, value.First, value.Second, value.BreakBefore);

    private void RequireObservationKind(ChartSeriesKind expected, ChartSeriesKind? alternate = null) {
        if (Kind != expected && Kind != alternate)
            throw new InvalidOperationException("Series '" + Name + "' does not support this typed observation family.");
    }

    private void ReplaceObservations<T>(ref IReadOnlyList<T> target, IEnumerable<T> values, Func<T, T> validate) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        var snapshot = values.Select(validate).ToArray();
        target = Array.AsReadOnly(snapshot);
        ClearObservationMetadata();
        ObservationDataChanged(snapshot.Length);
    }

    private void ClearObservationMetadata() {
        _funnelConnectorStyles.Clear(); PointColors.Clear(); _pointStrokeColors.Clear(); PointFillPatterns.Clear(); PointLabels.Clear();
        _pointMarkers.Clear(); PointDataLabelStyles.Clear(); PointSliceOffsets.Clear(); _pointSliceRadiusRatios.Clear(); ClearObservationKeys();
        _financialStyles = Array.Empty<ChartFinancialItemOptions?>();
    }

    private void UpdateObservation<T>(ref IReadOnlyList<T> target, int index, T value, Func<T, T> validate) {
        if (index < 0 || index >= target.Count) throw new ArgumentOutOfRangeException(nameof(index));
        value = validate(value);
        var snapshot = target.ToArray(); snapshot[index] = value;
        target = Array.AsReadOnly(snapshot);
        ObservationDataChanged(snapshot.Length);
    }

    private void ObservationDataChanged(int count) {
        _renderPoints = null;
        SourcePointCount = count; SourcePointIndices = Array.Empty<int>(); DecimationMode = null;
        if (Kind == ChartSeriesKind.BoxPlot) ClearBoxPlotRaw();
    }
}
