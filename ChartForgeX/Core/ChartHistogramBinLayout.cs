using System;

namespace ChartForgeX.Core;

/// <summary>
/// Defines one reusable histogram binning scheme so multiple series can share identical bounds.
/// </summary>
public sealed class ChartHistogramBinLayout {
    private ChartHistogramBinLayout(double minimum, double maximum, int count, double width) {
        Minimum = minimum;
        Maximum = maximum;
        Count = count;
        Width = width;
    }

    /// <summary>Gets the inclusive minimum covered by the layout.</summary>
    public double Minimum { get; }

    /// <summary>Gets the inclusive maximum covered by the layout.</summary>
    public double Maximum { get; }

    /// <summary>Gets the number of bins.</summary>
    public int Count { get; }

    /// <summary>Gets the requested bin width. The final bin may be narrower when a remainder exists.</summary>
    public double Width { get; }

    /// <summary>Creates an equal-width layout with an exact bin count.</summary>
    public static ChartHistogramBinLayout FromCount(double minimum, double maximum, int binCount) {
        ValidateRange(minimum, maximum);
        if (binCount < 1) throw new ArgumentOutOfRangeException(nameof(binCount), binCount, "Histogram bin count must be at least one.");
        if (minimum == maximum) return new ChartHistogramBinLayout(minimum, maximum, 1, 0);

        return new ChartHistogramBinLayout(minimum, maximum, binCount, (maximum - minimum) / binCount);
    }

    /// <summary>Creates a layout that preserves the requested width and uses a shorter final bin for any remainder.</summary>
    public static ChartHistogramBinLayout FromWidth(double minimum, double maximum, double binWidth) {
        ValidateRange(minimum, maximum);
        ChartGuards.Finite(binWidth, nameof(binWidth));
        if (binWidth <= 0) throw new ArgumentOutOfRangeException(nameof(binWidth), binWidth, "Histogram bin width must be greater than zero.");
        if (minimum == maximum) return new ChartHistogramBinLayout(minimum, maximum, 1, binWidth);

        var quotient = (maximum - minimum) / binWidth;
        var nearestInteger = Math.Round(quotient);
        var tolerance = Math.Max(1d, Math.Abs(quotient)) * 1e-12;
        if (Math.Abs(quotient - nearestInteger) <= tolerance) quotient = nearestInteger;
        if (double.IsInfinity(quotient) || quotient > int.MaxValue) {
            throw new ArgumentOutOfRangeException(nameof(binWidth), binWidth, "Histogram bin width produces too many bins.");
        }

        var count = Math.Max(1, (int)Math.Ceiling(quotient));
        return new ChartHistogramBinLayout(minimum, maximum, count, binWidth);
    }

    /// <summary>Gets the inclusive lower bound for a bin.</summary>
    public double GetLowerBound(int index) {
        ValidateIndex(index);
        return Minimum + Width * index;
    }

    /// <summary>Gets the upper bound for a bin. The final bin includes this value.</summary>
    public double GetUpperBound(int index) {
        ValidateIndex(index);
        return index == Count - 1 ? Maximum : Minimum + Width * (index + 1);
    }

    /// <summary>Gets the midpoint used for the bin's chart coordinate.</summary>
    public double GetCenter(int index) {
        var lower = GetLowerBound(index);
        return lower + (GetUpperBound(index) - lower) / 2.0;
    }

    internal int GetIndex(double value) {
        ChartGuards.Finite(value, nameof(value));
        if (value < Minimum || value > Maximum) {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Histogram values must fall within the shared bin layout.");
        }

        if (Count == 1 || value >= Maximum) return Count - 1;
        return Math.Max(0, Math.Min(Count - 1, (int)Math.Floor((value - Minimum) / Width)));
    }

    private static void ValidateRange(double minimum, double maximum) {
        ChartGuards.Finite(minimum, nameof(minimum));
        ChartGuards.Finite(maximum, nameof(maximum));
        if (maximum < minimum) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Histogram maximum must be greater than or equal to its minimum.");
        if (double.IsInfinity(maximum - minimum)) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Histogram range must be finite.");
    }

    private void ValidateIndex(int index) {
        if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index), index, "Histogram bin index is outside the layout.");
    }
}
