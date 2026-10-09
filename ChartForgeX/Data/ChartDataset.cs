using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Data;

/// <summary>
/// Represents an immutable, renderer-neutral collection of source rows that can be transformed before chart construction.
/// </summary>
/// <typeparam name="T">The source row type.</typeparam>
public sealed class ChartDataset<T> : IReadOnlyList<T> {
    private readonly List<T> _rows;
    private readonly IReadOnlyList<T> _readOnlyRows;

    private ChartDataset(IEnumerable<T> rows) {
        _rows = rows.ToList();
        _readOnlyRows = _rows.AsReadOnly();
    }

    /// <summary>Gets an empty dataset.</summary>
    public static ChartDataset<T> Empty { get; } = new(Array.Empty<T>());

    /// <summary>Gets the number of rows.</summary>
    public int Count => _rows.Count;

    /// <summary>Gets the row at the specified position.</summary>
    public T this[int index] => _rows[index];

    /// <summary>Gets the rows through a read-only collection.</summary>
    public IReadOnlyList<T> Rows => _readOnlyRows;

    /// <summary>Materializes a source sequence once into an immutable dataset.</summary>
    public static ChartDataset<T> From(IEnumerable<T> rows) {
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        return rows is ChartDataset<T> dataset ? dataset : new ChartDataset<T>(rows);
    }

    /// <summary>Returns rows accepted by the predicate without changing this dataset.</summary>
    public ChartDataset<T> Filter(Func<T, bool> predicate) {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        return new ChartDataset<T>(_rows.Where(predicate));
    }

    /// <summary>Returns rows ordered by the selected key without changing this dataset.</summary>
    public ChartDataset<T> SortBy<TKey>(Func<T, TKey> keySelector, IComparer<TKey>? comparer = null) {
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        return new ChartDataset<T>(_rows.OrderBy(keySelector, comparer ?? Comparer<TKey>.Default));
    }

    /// <summary>Returns rows ordered descending by the selected key without changing this dataset.</summary>
    public ChartDataset<T> SortByDescending<TKey>(Func<T, TKey> keySelector, IComparer<TKey>? comparer = null) {
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        return new ChartDataset<T>(_rows.OrderByDescending(keySelector, comparer ?? Comparer<TKey>.Default));
    }

    /// <summary>Projects every row into a new immutable dataset.</summary>
    public ChartDataset<TResult> Select<TResult>(Func<T, TResult> selector) {
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return ChartDataset<TResult>.From(_rows.Select(selector));
    }

    /// <summary>Groups rows by key while preserving first-key and source-row order.</summary>
    public IReadOnlyList<ChartDataGroup<TKey, T>> GroupBy<TKey>(Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer = null) {
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        return _rows
            .GroupBy(keySelector, comparer ?? EqualityComparer<TKey>.Default)
            .Select(group => new ChartDataGroup<TKey, T>(group.Key, From(group)))
            .ToArray();
    }

    /// <summary>Creates one summary row per group.</summary>
    public ChartDataset<TResult> Summarize<TKey, TResult>(
        Func<T, TKey> keySelector,
        Func<TKey, IReadOnlyList<T>, TResult> summarize,
        IEqualityComparer<TKey>? comparer = null) {
        if (summarize == null) throw new ArgumentNullException(nameof(summarize));
        return ChartDataset<TResult>.From(GroupBy(keySelector, comparer).Select(group => summarize(group.Key, group.Dataset.Rows)));
    }

    /// <summary>Partitions a numeric measure into the same rounded, equal-width layout used by scalar histogram ingestion.</summary>
    public ChartDataset<ChartDataBin<T>> Bin(Func<T, double> valueSelector, int binCount) {
        if (valueSelector == null) throw new ArgumentNullException(nameof(valueSelector));
        if (binCount <= 0) throw new ArgumentOutOfRangeException(nameof(binCount), binCount, "Bin count must be greater than zero.");
        if (_rows.Count == 0) return ChartDataset<ChartDataBin<T>>.Empty;

        var values = new double[_rows.Count];
        var minimum = double.MaxValue;
        var maximum = double.MinValue;
        for (var i = 0; i < _rows.Count; i++) {
            var value = valueSelector(_rows[i]);
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(valueSelector), value, "Binned values must be finite.");
            values[i] = value;
            minimum = Math.Min(minimum, value);
            maximum = Math.Max(maximum, value);
        }

        return Bin(values, ChartHistogramBinLayout.FromCount(minimum, maximum, binCount));
    }

    /// <summary>Partitions measurements through an explicit shared layout, preserving empty intervals and rejecting observations outside intervals or in gaps.</summary>
    public ChartDataset<ChartDataBin<T>> Bin(Func<T, double> valueSelector, ChartHistogramBinLayout layout) {
        if (valueSelector == null) throw new ArgumentNullException(nameof(valueSelector));
        if (layout == null) throw new ArgumentNullException(nameof(layout));
        return Bin(_rows.Select(valueSelector).ToArray(), layout);
    }

    private ChartDataset<ChartDataBin<T>> Bin(double[] values, ChartHistogramBinLayout layout) {
        var buckets = new List<T>[layout.Count];
        var measurements = new List<double>[layout.Count];
        var indices = new List<int>[layout.Count];
        for (var i = 0; i < layout.Count; i++) {
            buckets[i] = new List<T>(); measurements[i] = new List<double>(); indices[i] = new List<int>();
        }
        for (var i = 0; i < _rows.Count; i++) {
            var index = layout.GetIndex(values[i]);
            buckets[index].Add(_rows[i]);
            measurements[index].Add(values[i]);
            indices[index].Add(i);
        }

        var bins = new ChartDataBin<T>[layout.Count];
        var sourceIdentity = new object();
        for (var i = 0; i < layout.Count; i++)
            bins[i] = new ChartDataBin<T>(layout, i, From(buckets[i]), measurements[i].ToArray(), indices[i].ToArray(), sourceIdentity, _rows.Count);

        return ChartDataset<ChartDataBin<T>>.From(bins);
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => _rows.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
