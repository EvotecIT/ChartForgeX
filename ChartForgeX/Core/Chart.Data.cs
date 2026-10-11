using System;
using System.Collections.Generic;
using ChartForgeX.Data;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds a cartesian series by mapping typed source rows to x/y values.</summary>
    public Chart AddSeries<T>(
        string name,
        ChartSeriesKind kind,
        IEnumerable<T> rows,
        Func<T, double> x,
        Func<T, double> y,
        ChartColor? color = null) {
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        if (x == null) throw new ArgumentNullException(nameof(x));
        if (y == null) throw new ArgumentNullException(nameof(y));
        if (!ChartSeriesKindTraits.SupportsPointSeriesMapping(kind)) throw new ArgumentException("Typed x/y mapping supports ordinary point-based cartesian series kinds only.", nameof(kind));

        var points = new List<ChartPoint>();
        foreach (var row in rows) points.Add(new ChartPoint(x(row), y(row)));
        return Add(name, kind, points, color);
    }

    /// <summary>Adds a line series from typed source rows.</summary>
    public Chart AddLine<T>(string name, IEnumerable<T> rows, Func<T, double> x, Func<T, double> y, ChartColor? color = null) =>
        AddSeries(name, ChartSeriesKind.Line, rows, x, y, color);

    /// <summary>Adds a bar series from typed source rows.</summary>
    public Chart AddBar<T>(string name, IEnumerable<T> rows, Func<T, double> x, Func<T, double> y, ChartColor? color = null) =>
        AddSeries(name, ChartSeriesKind.Bar, rows, x, y, color);

    /// <summary>Adds an area series from typed source rows.</summary>
    public Chart AddArea<T>(string name, IEnumerable<T> rows, Func<T, double> x, Func<T, double> y, ChartColor? color = null) =>
        AddSeries(name, ChartSeriesKind.Area, rows, x, y, color);

    /// <summary>Adds a scatter series from typed source rows.</summary>
    public Chart AddScatter<T>(string name, IEnumerable<T> rows, Func<T, double> x, Func<T, double> y, ChartColor? color = null) =>
        AddSeries(name, ChartSeriesKind.Scatter, rows, x, y, color);

    /// <summary>Adds one histogram count per interval, preserving the layout, empty bins and original source identities.</summary>
    public Chart AddHistogram<T>(string name, ChartDataset<ChartDataBin<T>> bins, ChartColor? color = null) =>
        AddHistogram(name, bins, ChartHistogramEncoding.Value, color);

    /// <summary>Adds a typed dataset's counts with value or full-interval density encoding.</summary>
    public Chart AddHistogram<T>(string name, ChartDataset<ChartDataBin<T>> bins, ChartHistogramEncoding encoding, ChartColor? color = null) {
        if (bins == null) throw new ArgumentNullException(nameof(bins));
        if (bins.Count == 0) throw new ArgumentException("Histogram bins require a layout; bin an empty dataset with an explicit layout to retain its intervals.", nameof(bins));
        var layout = bins[0].Layout;
        if (bins.Count != layout.Count) throw new ArgumentException("Histogram bins must retain the complete shared layout in interval order.", nameof(bins));
        var sourceCount = 0;
        for (var index = 0; index < bins.Count; index++) {
            if (!ReferenceEquals(bins[index].Layout, layout) || bins[index].Index != index || !ReferenceEquals(bins[index].SourceIdentity, bins[0].SourceIdentity))
                throw new ArgumentException("Histogram bins must retain one complete source partition and shared layout in interval order.", nameof(bins));
            sourceCount += bins[index].Count;
        }
        if (sourceCount != bins[0].SourceCount) throw new ArgumentException("Histogram bins must preserve every source observation exactly once.", nameof(bins));
        var observations = new ChartPoint[sourceCount];
        foreach (var bin in bins) {
            for (var index = 0; index < bin.Count; index++)
                observations[bin.SourceIndices[index]] = new ChartPoint(bin.Measurements[index], 1);
        }
        return AddHistogram(name, observations, layout, ChartHistogramAggregation.Count, encoding, color);
    }
}
