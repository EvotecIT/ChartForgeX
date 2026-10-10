using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>An immutable opening balance, change or calculated checkpoint at an explicit waterfall display coordinate.</summary>
public sealed class ChartWaterfallItem {
    private ChartWaterfallItem(double x, ChartWaterfallItemKind kind, double? value = null) {
        ChartGuards.Finite(x, nameof(x));
        if (value.HasValue) ChartGuards.Finite(value.Value, nameof(value));
        X = x; Kind = kind; Value = value;
    }

    /// <summary>Gets the display coordinate. Items accumulate in input order, independently of axis reversal.</summary>
    public double X { get; }

    /// <summary>Gets whether the item supplies an opening balance or change, or requests a calculated subtotal or total.</summary>
    public ChartWaterfallItemKind Kind { get; }

    /// <summary>Gets the supplied opening balance or change, or null for a calculated checkpoint.</summary>
    public double? Value { get; }

    /// <summary>Creates a finite opening balance drawn from zero. It must be the first item and may occur only once.</summary>
    public static ChartWaterfallItem OpeningBalance(double x, double value) {
        ChartGuards.Finite(value, nameof(value));
        return new ChartWaterfallItem(x, ChartWaterfallItemKind.OpeningBalance, value);
    }

    /// <summary>Creates a finite change to the running balance at the supplied display coordinate.</summary>
    public static ChartWaterfallItem Delta(double x, double value) {
        ChartGuards.Finite(value, nameof(value));
        return new ChartWaterfallItem(x, ChartWaterfallItemKind.Delta, value);
    }

    /// <summary>Creates a subtotal spanning the preceding checkpoint balance to the current balance, without changing that balance.</summary>
    public static ChartWaterfallItem Subtotal(double x) => new(x, ChartWaterfallItemKind.Subtotal);

    /// <summary>Creates a cumulative total drawn from zero, without changing the running balance.</summary>
    public static ChartWaterfallItem Total(double x) => new(x, ChartWaterfallItemKind.Total);

    /// <summary>Creates an opening balance at a date/time coordinate, using the same UTC conversion as <see cref="ChartPoint"/>.</summary>
    public static ChartWaterfallItem OpeningBalance(DateTime x, double value) => OpeningBalance(ChartDateTime.ToOADate(x), value);

    /// <summary>Creates a change at a date/time coordinate, using the same UTC conversion as <see cref="ChartPoint"/>.</summary>
    public static ChartWaterfallItem Delta(DateTime x, double value) => Delta(ChartDateTime.ToOADate(x), value);

    /// <summary>Creates a subtotal at a date/time coordinate, using the same UTC conversion as <see cref="ChartPoint"/>.</summary>
    public static ChartWaterfallItem Subtotal(DateTime x) => Subtotal(ChartDateTime.ToOADate(x));

    /// <summary>Creates a cumulative total at a date/time coordinate, using the same UTC conversion as <see cref="ChartPoint"/>.</summary>
    public static ChartWaterfallItem Total(DateTime x) => Total(ChartDateTime.ToOADate(x));
}
