using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

    /// <summary>Partitions a numeric measure into equal-width bins.</summary>
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

        if (Math.Abs(maximum - minimum) < 0.000000000001) {
            return ChartDataset<ChartDataBin<T>>.From(new[] { new ChartDataBin<T>(minimum, maximum, From(_rows)) });
        }

        var width = (maximum - minimum) / binCount;
        var buckets = new List<T>[binCount];
        for (var i = 0; i < binCount; i++) buckets[i] = new List<T>();
        for (var i = 0; i < _rows.Count; i++) {
            var index = values[i] >= maximum ? binCount - 1 : Math.Min(binCount - 1, (int)((values[i] - minimum) / width));
            buckets[index].Add(_rows[i]);
        }

        var bins = new ChartDataBin<T>[binCount];
        for (var i = 0; i < binCount; i++) {
            var lower = minimum + width * i;
            var upper = i == binCount - 1 ? maximum : minimum + width * (i + 1);
            bins[i] = new ChartDataBin<T>(lower, upper, From(buckets[i]));
        }

        return ChartDataset<ChartDataBin<T>>.From(bins);
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => _rows.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
