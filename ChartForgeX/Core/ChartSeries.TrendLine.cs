using System;
using System.Collections.Generic;
using System.Threading;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private ChartTrendLineOptions _trendLineOptions = null!;
    private ChartTrendLineFit? _trendLineFit;

    /// <summary>Gets a detached configuration snapshot, or null when this is not a retained-source trend line.</summary>
    public ChartTrendLineOptions? TrendLineConfiguration => _trendLineFit == null ? null : _trendLineOptions.Capture();
    /// <summary>Gets immutable native fit/ribbon samples; raw observations remain in TrendLineSourcePoints.</summary>
    public IReadOnlyList<ChartTrendLineSample> TrendLineSamples => _trendLineFit?.Samples ?? Array.Empty<ChartTrendLineSample>();
    internal bool HasAdvancedTrendLine => _trendLineFit != null && (_trendLineOptions.Model != ChartTrendLineModel.Linear
        || _trendLineOptions.Interval != ChartTrendLineIntervalKind.None);
    internal bool TrendLineLogResponse => _trendLineFit?.LogResponse == true;
    internal ChartTrendLineOptions CapturedTrendLineOptions => _trendLineOptions;

    /// <summary>Creates a configured regression directly, without fitting an intermediate default linear model.</summary>
    /// <param name="id">The stable series identity.</param>
    /// <param name="name">The display name.</param>
    /// <param name="observations">The complete original source observations.</param>
    /// <param name="color">The optional series color.</param>
    /// <param name="configure">The independently captured model and interval configuration.</param>
    /// <returns>The independently configurable retained-source series.</returns>
    public static ChartSeries CreateTrendLine(ChartSeriesId id, string name, IEnumerable<ChartPoint> observations,
        ChartColor? color, Action<ChartTrendLineOptions> configure) => TrendLine(
            new ChartSeries(id, name, ChartSeriesKind.TrendLine, Array.Empty<ChartPoint>()) { Color = color },
            observations, CaptureTrendLineConfiguration(configure));

    internal static ChartSeries CreateTrendLine(string name, IEnumerable<ChartPoint> observations,
        ChartColor? color, Action<ChartTrendLineOptions> configure) => TrendLine(
            new ChartSeries(name, ChartSeriesKind.TrendLine, Array.Empty<ChartPoint>()) { Color = color },
            observations, CaptureTrendLineConfiguration(configure));

    private static ChartTrendLineOptions CaptureTrendLineConfiguration(Action<ChartTrendLineOptions> configure) =>
        ChartOptionConfiguration.Configure(new ChartTrendLineOptions(), configure, value => value.Capture());

    /// <summary>Refits this trend line from its retained source with an independently captured configuration.</summary>
    /// <remarks>Callback, domain, numerical rank and cancellation failures leave source, identity, configuration and fit unchanged.
    /// Every retained source row participates in one regression; source segment breaks do not split it into independent fits.</remarks>
    public ChartSeries ConfigureTrendLine(Action<ChartTrendLineOptions> configure, CancellationToken cancellationToken = default) {
        RequireTrendLineSource();
        var options = ChartOptionConfiguration.Configure(_trendLineOptions, configure, value => value.Capture());
        var fit = ChartTrendLineFit.Create(TrendLineSourcePoints, options, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        InstallTrendLine(TrendLineSourcePoints, options, fit);
        return this;
    }

    /// <summary>Captures new source observations and atomically refits this same derived series.</summary>
    /// <remarks>Like other complete-source replacements, this clears raw observation IDs. Series paint and the two derived
    /// endpoint handles retain their presentation. At least the model's required distinct predictors remain necessary;
    /// invalid enumeration, domain, rank and cancellation leave the previous source and fit installed.</remarks>
    public ChartSeries ReplaceTrendLineSourcePoints(IEnumerable<ChartPoint> observations, CancellationToken cancellationToken = default) {
        RequireTrendLineSource();
        var source = CaptureTrendLineSource(observations, cancellationToken);
        var fit = ChartTrendLineFit.Create(source, _trendLineOptions, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        InstallTrendLine(source, _trendLineOptions, fit);
        ClearObservationKeys();
        return this;
    }

    private static ChartSeries TrendLine(ChartSeries series, IEnumerable<ChartPoint> observations) =>
        TrendLine(series, observations, new ChartTrendLineOptions());

    private static ChartSeries TrendLine(ChartSeries series, IEnumerable<ChartPoint> observations, ChartTrendLineOptions options) {
        var source = CaptureTrendLineSource(observations, default);
        series.InstallTrendLine(source, options, ChartTrendLineFit.Create(source, options, default));
        return series;
    }

    private void InstallTrendLine(IReadOnlyList<ChartPoint> source, ChartTrendLineOptions options, ChartTrendLineFit fit) {
        // Preparation has captured its scene independently. Only this model's complete new fit is installed.
        _points.Clear();
        _points.Add(new ChartPoint(fit.Samples[0].X, fit.Samples[0].Value));
        var last = fit.Samples[fit.Samples.Count - 1];
        _points.Add(new ChartPoint(last.X, last.Value));
        ObservationDataChanged(source.Count);
        SetTrendLineSourcePoints(source);
        _trendLineOptions = options;
        _trendLineFit = fit;
    }

    private void RequireTrendLineSource() {
        if (Kind != ChartSeriesKind.TrendLine || _trendLineFit == null)
            throw new InvalidOperationException("Use CreateTrendLine or AddTrendLine before configuring or replacing regression source observations.");
    }

    private static List<ChartPoint> CaptureTrendLineSource(IEnumerable<ChartPoint> observations, CancellationToken cancellationToken) {
        if (observations == null) throw new ArgumentNullException(nameof(observations));
        var source = new List<ChartPoint>();
        foreach (var point in observations) {
            cancellationToken.ThrowIfCancellationRequested();
            ChartGuards.Finite(point.X, nameof(observations)); ChartGuards.Finite(point.Y, nameof(observations));
            source.Add(point);
        }
        cancellationToken.ThrowIfCancellationRequested();
        return source;
    }
}
