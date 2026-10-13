using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    private readonly List<ChartSeries> _series = new();
    private IReadOnlyList<ChartSeries>? _seriesView;
    private long _nextAutomaticSeriesId;

    /// <summary>Gets attached series in paint order. Retain the series itself to configure it independently of this order.</summary>
    public IReadOnlyList<ChartSeries> Series => _seriesView ??= new ReadOnlyCollection<ChartSeries>(_series);

    /// <summary>Attaches a configured series, preserving its stable identity and data ownership.</summary>
    /// <remarks>A series can belong to only one chart at a time. IDs must be unique within the chart.</remarks>
    public Chart AddSeries(ChartSeries series) {
        if (series == null) throw new ArgumentNullException(nameof(series));
        EnsureCanAddSeries();
        if (series.Owner != null) throw new InvalidOperationException("The series is already attached to a chart. Remove it before attaching it elsewhere.");
        if (series.AutomaticIdentityOwner != null && !ReferenceEquals(series.AutomaticIdentityOwner, this))
            throw new InvalidOperationException("An automatically identified series belongs to its original chart. Create a series with an explicit ID to move it between charts.");
        if (series.HasIdentity) {
            foreach (var attached in _series)
                if (attached.Id == series.Id) throw new ArgumentException("The chart already contains a series with ID '" + series.Id.Value + "'.", nameof(series));
        } else series.AssignAutomaticIdentity(ChartSeriesId.Automatic(checked(++_nextAutomaticSeriesId)), this);
        _series.Add(series);
        series.AttachTo(this);
        return this;
    }

    /// <summary>Removes an attached series without changing its identity or the identities of remaining series.</summary>
    public Chart RemoveSeries(ChartSeries series) {
        var index = OwnedSeriesIndex(series);
        EnsureCanAddSeries();
        _series.RemoveAt(index);
        series.DetachFrom(this);
        return this;
    }

    /// <summary>Moves an attached series to a zero-based paint position while retaining its identity.</summary>
    public Chart MoveSeries(ChartSeries series, int index) {
        var current = OwnedSeriesIndex(series);
        if (index < 0 || index >= _series.Count) throw new ArgumentOutOfRangeException(nameof(index), "Position must refer to the chart's current series collection.");
        EnsureCanAddSeries();
        if (index == current) return this;
        _series.RemoveAt(current);
        _series.Insert(index, series);
        return this;
    }

    /// <summary>Removes every series while keeping each detached handle and identity usable.</summary>
    public Chart ClearSeries() {
        EnsureCanAddSeries();
        foreach (var series in _series) series.DetachFrom(this);
        _series.Clear();
        return this;
    }

    internal int IndexOfSeries(ChartSeries series) => _series.IndexOf(series);

    private int OwnedSeriesIndex(ChartSeries series) {
        if (series == null) throw new ArgumentNullException(nameof(series));
        var index = _series.IndexOf(series);
        if (index < 0) throw new ArgumentException("The series is not attached to this chart.", nameof(series));
        return index;
    }
}
