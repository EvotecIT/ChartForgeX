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

    /// <summary>Adds one bar per numeric bin using its midpoint and source-row count.</summary>
    public Chart AddHistogram<T>(string name, ChartDataset<ChartDataBin<T>> bins, ChartColor? color = null) {
        if (bins == null) throw new ArgumentNullException(nameof(bins));
        return AddBar(name, bins, bin => bin.Center, bin => bin.Count, color);
    }
}
