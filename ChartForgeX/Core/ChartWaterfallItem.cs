using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>An immutable change or calculated checkpoint at an explicit waterfall display coordinate.</summary>
public sealed class ChartWaterfallItem {
    private ChartWaterfallItem(double x, ChartWaterfallItemKind kind, double? deltaValue = null) {
        ChartGuards.Finite(x, nameof(x));
        if (deltaValue.HasValue) ChartGuards.Finite(deltaValue.Value, nameof(deltaValue));
        X = x; Kind = kind; DeltaValue = deltaValue;
    }

    /// <summary>Gets the display coordinate. Items accumulate in input order, independently of axis reversal.</summary>
    public double X { get; }

    /// <summary>Gets whether the item supplies a change or requests a calculated subtotal or total.</summary>
    public ChartWaterfallItemKind Kind { get; }

    /// <summary>Gets the supplied change, or null for a calculated checkpoint. Checkpoints carry no authored numeric value.</summary>
    public double? DeltaValue { get; }

    /// <summary>Creates a finite change to the running balance at the supplied display coordinate.</summary>
    public static ChartWaterfallItem Delta(double x, double value) {
        ChartGuards.Finite(value, nameof(value));
        return new ChartWaterfallItem(x, ChartWaterfallItemKind.Delta, value);
    }

    /// <summary>Creates a subtotal spanning the preceding checkpoint balance to the current balance, without changing that balance.</summary>
    public static ChartWaterfallItem Subtotal(double x) => new(x, ChartWaterfallItemKind.Subtotal);

    /// <summary>Creates a cumulative total drawn from zero, without changing the running balance.</summary>
    public static ChartWaterfallItem Total(double x) => new(x, ChartWaterfallItemKind.Total);

    /// <summary>Creates a change at a date/time coordinate, using the same UTC conversion as <see cref="ChartPoint"/>.</summary>
    public static ChartWaterfallItem Delta(DateTime x, double value) => Delta(ChartDateTime.ToOADate(x), value);

    /// <summary>Creates a subtotal at a date/time coordinate, using the same UTC conversion as <see cref="ChartPoint"/>.</summary>
    public static ChartWaterfallItem Subtotal(DateTime x) => Subtotal(ChartDateTime.ToOADate(x));

    /// <summary>Creates a cumulative total at a date/time coordinate, using the same UTC conversion as <see cref="ChartPoint"/>.</summary>
    public static ChartWaterfallItem Total(DateTime x) => Total(ChartDateTime.ToOADate(x));
}
