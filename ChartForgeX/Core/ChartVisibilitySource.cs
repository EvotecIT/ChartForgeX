using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Accessibility;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Core;

/// <summary>One private detached finite source for both native visibility preparation and canonical source-key facts.</summary>
internal sealed partial class ChartVisibilitySource {
    private readonly VisualArtifactInterchangeWaterfallArtifact _presentation;
    private readonly ChartLineVisualStyle _line;
    private readonly ChartBoxPlotOptions _boxes;
    private readonly VisualAccessibility _accessibility;
    private readonly IReadOnlyList<Series> _series;
    private readonly IReadOnlyList<ChartVisibilityTarget> _targets;
    private readonly IReadOnlyDictionary<(ChartVisibilityTargetKind Kind, string Series, string? Observation), ChartVisibilityTarget> _targetIndex;
    private readonly IReadOnlyList<ChartAxisLabel> _categoryLabels;
    private readonly PiePolicy? _pie;
    private readonly ChartBubbleOptions? _bubble;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<int>> _radialOther;
    private readonly IReadOnlyDictionary<string, HashSet<int>> _radialOtherIndex;
    internal ChartVisibilitySource(Chart chart, VisualRenderContext context) : this(chart, context, false) { }
    internal static ChartVisibilitySource CaptureNativeCartesianValues(Chart chart, VisualRenderContext context) => new(chart, context, true);
    internal static ChartVisibilitySource CaptureNativeBubbleValues(Chart chart, VisualRenderContext context) => CaptureNativeComponentValues(chart, context);
    internal static ChartVisibilitySource CaptureNativeComponentValues(Chart chart, VisualRenderContext context) => new(chart, context, false, true);
    private ChartVisibilitySource(Chart chart, VisualRenderContext context, bool completeCartesianValues, bool componentValues = false) {
        Validate(chart, completeCartesianValues, componentValues);
        _bubble = componentValues && chart.Series.Any(series => series.Kind == ChartSeriesKind.Bubble) ? chart.Options.Bubble.Clone() : null;
        Context = context.WithNativeTextCapture(null); Title = chart.Title; Subtitle = chart.Subtitle;
        _presentation = VisualArtifactInterchangeMapping.CaptureVisibilityPresentation(chart);
        // Recompute the fixed category map from copied authored policy; never mutate the prepared source or retain a late mutable chart.
        if (componentValues && _presentation.XAxis.Scale == ChartScaleKind.Time) _presentation.XAxis.PreparedTimeDomain = null;
        _line = chart.Options.LineVisualStyle.Clone(); _boxes = chart.Options.BoxPlot.Clone(); _accessibility = chart.Accessibility.Clone();
        _series = Array.AsReadOnly(chart.Series.Select(series => series.CaptureVisibilitySource()).ToArray());
        _categoryLabels = Array.AsReadOnly(ChartCartesianOrientation.CategoryLabels(chart).ToArray());
        BarMode = chart.Options.BarMode; ShowStackTotals = chart.Options.ShowStackTotals; MaximumBarWidth = chart.Options.MaximumBarWidth;
        LineOrientation = chart.Options.LineOrientation; LineMarkers = chart.Options.LineMarkerMode;
        Width = chart.Options.Size.Width; Height = chart.Options.Size.Height;
        _hierarchy = chart.Series.Any(series => series.Kind is ChartSeriesKind.Treemap or ChartSeriesKind.Sunburst) ? new HierarchyPolicy(chart) : null;
        _pie = chart.Series.Any(series => ChartSeriesKindTraits.IsPieLikeKind(series.Kind)) ? new PiePolicy(chart.Options) : null;
        _radialOther = _pie == null ? new Dictionary<string, IReadOnlyList<int>>()
            : VisualRadialCompiler.CaptureOtherMembership(CreateNativeView(null), context.Theme.Resolve(context.ThemeMode));
        _radialOtherIndex = _radialOther.ToDictionary(pair => pair.Key, pair => new HashSet<int>(pair.Value), StringComparer.Ordinal);
        _targets = CaptureTargetCatalog(); _targetIndex = _targets.ToDictionary(target => (target.Kind, target.SeriesId.Value, target.Identity));
    }
    internal VisualRenderContext Context { get; }
    internal string Title { get; }
    internal string Subtitle { get; }
    internal int Width { get; }
    internal int Height { get; }
    internal ChartBarMode BarMode { get; }
    internal bool ShowStackTotals { get; }
    internal double? MaximumBarWidth { get; }
    internal ChartOrientation LineOrientation { get; }
    internal ChartLineMarkerMode? LineMarkers { get; }
    internal IReadOnlyList<Series> SeriesRows => _series;
    internal VisualArtifactInterchangeWaterfallArtifact CopyPresentation() => _presentation.Clone();
    internal ChartLineVisualStyle CopyLine() => _line.Clone();
    internal ChartBoxPlotOptions CopyBoxes() => _boxes.Clone();
    internal ChartBubbleOptions? CopyBubble() => _bubble?.Clone();
    internal VisualAccessibility CopyAccessibility() => _accessibility.Clone();
    internal IReadOnlyList<ChartAxisLabel> CategoryLabels => _categoryLabels;

    internal Chart CreateNativeView(ChartVisibilityView? view) => CreateNativeView(view, null);
    internal Chart CreateNativeValueView(double[] values) {
        if (values.Length != _series.Sum(row => row.NativeValueCount)) throw new ArgumentException("Complete native value readings must match their captured source rows.", nameof(values));
        return CreateNativeView(null, values);
    }
    private Chart CreateNativeView(ChartVisibilityView? view, double[]? values) {
        var chart = Chart.Create().WithTitle(Title).WithSubtitle(Subtitle).WithSize(Width, Height);
        VisualArtifactInterchangeMapping.RestoreVisibilityPresentation(chart, _presentation);
        chart.Options.LineVisualStyle = _line.Clone(); chart.Options.BoxPlot = _boxes.Clone(); chart.Options.BarMode = BarMode;
        chart.Options.ShowStackTotals = ShowStackTotals; chart.Options.MaximumBarWidth = MaximumBarWidth;
        chart.Options.LineOrientation = LineOrientation; chart.Options.LineMarkerMode = LineMarkers;
        var accessibility = _accessibility;
        chart.ConfigureAccessibility(target => { target.Name = accessibility.Name; target.Description = accessibility.Description; target.Language = accessibility.Language; target.IsDecorative = accessibility.IsDecorative; });
        _pie?.Apply(chart.Options); _hierarchy?.Apply(chart.Options);
        if (_bubble != null) chart.Options.Bubble = _bubble.Clone();
        if (_hierarchy != null) chart.NativeHierarchyVisibility = view ?? (_hierarchy.Root == null ? null
            : new ChartVisibilityView(Array.Empty<ChartVisibilityTarget>(), ChartVisibilityLayout.Reflow, ChartVisibilityLabelLayering.PreserveOverlay, _hierarchy.Root));
        var offset = 0;
        foreach (var row in _series) {
            var count = row.NativeValueCount;
            // Single-source plans retain their incumbent array/default path; multiband readings are scoped to their captured series.
            var readings = values == null || _series.Count == 1 ? values : values.Skip(offset).Take(count).ToArray();
            chart.AddSeries(ChartSeries.FromVisibilitySource(row, view, VisibleIndices(row, view), readings));
            offset += count;
        }
        return chart;
    }

    private static void Validate(Chart chart, bool completeCartesianValues = false, bool componentValues = false) {
        if (chart.Series.Count == 0 || chart.Series.Any(series => (componentValues ? series.Kind is not (ChartSeriesKind.Bubble or ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea or ChartSeriesKind.ErrorBar or ChartSeriesKind.Candlestick or ChartSeriesKind.Ohlc) : completeCartesianValues
            ? !VisualCartesianCompiler.SupportsCompleteValueKind(series.Kind)
            : series.Kind is not (ChartSeriesKind.Line or ChartSeriesKind.Bar or ChartSeriesKind.BoxPlot or ChartSeriesKind.Pie or ChartSeriesKind.Donut or ChartSeriesKind.Treemap or ChartSeriesKind.Sunburst)) || series.Id.IsAutomatic))
            throw new NotSupportedException("Native Reflow requires authored Line/Bar, BoxPlot, native Pie/Donut bands, or authored Treemap/Sunburst sources.");
        ValidateHierarchySource(chart);
        if (chart.Series.Any(series => ChartSeriesKindTraits.IsPieLikeKind(series.Kind)
            && Enumerable.Range(0, series.Points.Count).Any(index => !series.ObservationIds.ContainsKey(index))))
            throw new NotSupportedException("Pie/Donut Reflow requires an authored observation identity for every retained source slice and legend row.");
        long values = 0;
        foreach (var series in chart.Series) values += (long)series.Points.Count + 4L * series.Bubbles.Count + 4L * series.Ranges.Count + 7L * series.ErrorBars.Count + 5L * series.FinancialValues.Count + (series.Kind is ChartSeriesKind.Candlestick or ChartSeriesKind.Ohlc ? series.FinancialSourceValueCount : 0) + series.HierarchyItems.Count + series.NodeStates.Count + series.PointStrokeColors.Count + series.BoxPlots.Count + series.BoxPlotSourceSamples.Count
            + series.ObservationIds.Count + series.BoxPlotGroupIds.Count + series.PointColors.Count + series.PointFillPatterns.Count
            + series.PointLabels.Count + series.PointDataLabelStyles.Count;
        if (values > VisualArtifactInterchangeEnvelope.MaximumJsonValues)
            throw new ArgumentOutOfRangeException(nameof(chart), "Native Reflow source exceeds the existing common interchange value budget.");
        var options = chart.Options;
        if (chart.Annotations.Count != 0 || chart.PlotRegions.Count != 0 || options.IsSparkline || options.IsPanel || options.XAxisLabelHighlights.Count != 0 || options.SvgColorVariables != null)
            throw new NotSupportedException("Native Reflow does not yet capture annotations, plot regions, compact-panel state, category highlights or chart SVG variable resolvers.");
        if (chart.Series.Any(series => series.DecimationMode.HasValue || series.HistogramBinLayout != null || series.ColorScale != null || series.ColorDimension.HasOverrides || series.HasPointMarkers))
            throw new NotSupportedException("Native Reflow does not yet capture derived decimation/histograms, numeric color dimensions or per-point marker policies.");
        if (options.Labels.AccessibleTextFormatter != null || options.Labels.DateFormatter != null || options.BoxPlot.StatisticLabelFormatter != null || options.PieSliceLabelFormatter != null)
            throw new NotSupportedException("Native Reflow requires static label words and captured built-in formats; custom callbacks remain available for ordinary rendering.");
        try { _ = options.ValueFormat.PortablePolicy();
        foreach (var axis in new[] { options.XAxis, options.YAxis, options.SecondaryXAxis, options.SecondaryYAxis }) {
            var nativeFixedTimeX = componentValues && ReferenceEquals(axis, options.XAxis) && axis.Scale == ChartScaleKind.Time;
            if (axis.TimeTickLabelFormatter != null || axis.PreparedTimeDomain != null && !nativeFixedTimeX || axis.PreparedViewportWindow != null)
                throw new NotSupportedException("Native Reflow requires authored axis policy, without custom/prepared axis closures.");
            if (axis.ValueFormat != null) _ = axis.ValueFormat.PortablePolicy();
        }
        } catch (NotSupportedException exception) {
            throw new NotSupportedException("Native Reflow source capture requires built-in formatting with unchanged named cultures and authored axis policy; ordinary callbacks remain available.", exception);
        }
    }
}
