using System;

namespace ChartForgeX.Core;

/// <summary>Immutable observation facts supplied to preparation-time label and tooltip formatters.</summary>
public sealed class ChartValueContext {
    private readonly object? _datum;
    internal ChartValueContext(ChartSeriesInfo series, int index, object? datum, double? value, string? category, double? percent) {
        Series = series; Index = index; _datum = datum; Value = value; Category = category; Percent = percent;
    }

    /// <summary>Gets detached series identity, display name, rendering kind and semantic state.</summary>
    public ChartSeriesInfo Series { get; }
    /// <summary>Gets the zero-based authored observation index, or the separate authored link ordinal for a relationship datum.</summary>
    public int Index { get; }
    /// <summary>Gets the scalar reading, or null for compound observations without a single measured value.</summary>
    public double? Value { get; }
    /// <summary>Gets the category caption, node name, date or state where the observation has one.</summary>
    public string? Category { get; }
    /// <summary>Gets a fraction of the declared scale or part-to-whole total, or null when no such total is defined. One means 100 percent.</summary>
    public double? Percent { get; }
    /// <summary>Gets the authored family datum. Mutable family data is copied before entering the context.</summary>
    public object? Datum => ChartSeries.CopyFormattingDatum(_datum);
    /// <summary>Returns the authored datum as its family type, such as ChartPoint, ChartBubble or ChartInterval.</summary>
    /// <exception cref="InvalidOperationException">The observation does not have the requested datum type.</exception>
    public TDatum GetDatum<TDatum>() => Datum is TDatum value ? value
        : throw new InvalidOperationException("This observation does not contain " + typeof(TDatum).Name + ".");
}
