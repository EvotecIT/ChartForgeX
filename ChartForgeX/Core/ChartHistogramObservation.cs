using System;

namespace ChartForgeX.Core;

/// <summary>One immutable histogram measurement, signed quantity and independent nonnegative averaging weight.</summary>
public readonly struct ChartHistogramObservation {
    /// <summary>Creates a finite observation. Zero weight retains source identity but contributes nothing to a weighted mean.</summary>
    /// <param name="measurement">The coordinate assigned to a histogram interval.</param>
    /// <param name="quantity">The signed value used by Sum, Mean and WeightedMean.</param>
    /// <param name="weight">The nonnegative finite weight used only by WeightedMean. Count still counts rows.</param>
    public ChartHistogramObservation(double measurement, double quantity, double weight = 1) {
        ChartGuards.Finite(measurement, nameof(measurement));
        ChartGuards.Finite(quantity, nameof(quantity));
        ChartGuards.Finite(weight, nameof(weight));
        if (weight < 0) throw new ArgumentOutOfRangeException(nameof(weight), weight, "Histogram weights must be nonnegative.");
        Measurement = measurement; Quantity = quantity; Weight = weight;
    }

    /// <summary>Gets the measurement assigned through the shared interval layout.</summary>
    public double Measurement { get; }
    /// <summary>Gets the signed source quantity, before aggregation.</summary>
    public double Quantity { get; }
    /// <summary>Gets the independent nonnegative averaging weight.</summary>
    public double Weight { get; }
}
