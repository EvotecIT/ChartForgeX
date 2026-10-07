using System;
using System.Globalization;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

/// <summary>
/// Defines one reusable histogram binning scheme so multiple series can share identical bounds.
/// </summary>
public sealed class ChartHistogramBinLayout {
    private const int MaximumBoundaryValidationScanCount = 1_000_000;
    private readonly decimal? _decimalMinimum;
    private readonly decimal? _decimalWidth;

    private ChartHistogramBinLayout(double minimum, double maximum, int count, double width) {
        Minimum = minimum;
        Maximum = maximum;
        Count = count;
        Width = width;
        if (width > 0 && TryConvertRoundTripDecimal(minimum, out var decimalMinimum) &&
            TryConvertRoundTripDecimal(width, out var decimalWidth) && decimalWidth > 0) {
            _decimalMinimum = decimalMinimum;
            _decimalWidth = decimalWidth;
        }
    }

    /// <summary>Gets the inclusive minimum covered by the layout.</summary>
    public double Minimum { get; }

    /// <summary>Gets the inclusive maximum covered by the layout.</summary>
    public double Maximum { get; }

    /// <summary>Gets the number of bins.</summary>
    public int Count { get; }

    /// <summary>Gets the bin width. Only exact data-bounded layouts can have a narrower final bin.</summary>
    public double Width { get; }

    /// <summary>Creates the requested count of equal-width bins with boundaries aligned to a nice decimal step where possible.</summary>
    public static ChartHistogramBinLayout FromCount(double minimum, double maximum, int binCount) => FromCount(minimum, maximum, binCount, true);

    /// <summary>Creates bins with rounded, aligned bounds, or preserves exact data bounds when <paramref name="roundBounds"/> is false.</summary>
    /// <param name="minimum">The lowest data value.</param>
    /// <param name="maximum">The highest data value.</param>
    /// <param name="binCount">The requested count; constant data uses one bin.</param>
    /// <param name="roundBounds">Whether to align to multiples of a 1, 2, 2.5, 5 or 10 decimal step. A single bin crossing zero preserves the data bounds because no zero-aligned interval can cover both signs.</param>
    /// <returns>A reusable bin layout.</returns>
    public static ChartHistogramBinLayout FromCount(double minimum, double maximum, int binCount, bool roundBounds) {
        ValidateRange(minimum, maximum);
        if (binCount < 1) throw new ArgumentOutOfRangeException(nameof(binCount), binCount, "Histogram bin count must be at least one.");
        if (minimum == maximum) {
            if (!roundBounds) return new ChartHistogramBinLayout(minimum, maximum, 1, 0);
            return FromWidth(minimum, maximum, ChartNiceNumbers.Step(Math.Max(1, Math.Abs(minimum) * 0.1)), true);
        }

        var width = (maximum - minimum) / binCount;
        if (roundBounds && binCount == 1 && minimum < 0 && maximum > 0)
            return new ChartHistogramBinLayout(minimum, maximum, 1, width);
        if (roundBounds) {
            width = ChartNiceNumbers.Step(width);
            // Reconstructing an aligned decimal boundary as a double can move it inward by an ULP.
            // Preserve the actual source endpoint when that happens; GetIndex retains strict bounds.
            var alignedMinimum = Math.Min(minimum, Math.Floor(NormalizeNearInteger(minimum / width)) * width);
            // Alignment can add a partial interval below the data; increase the step until the requested count covers it.
            for (var attempt = 0; attempt < 8 && alignedMinimum + binCount * width < maximum; attempt++) {
                width = ChartNiceNumbers.Step(width * 1.01);
                alignedMinimum = Math.Min(minimum, Math.Floor(NormalizeNearInteger(minimum / width)) * width);
            }
            minimum = alignedMinimum;
            maximum = minimum + binCount * width;
            ValidateRange(minimum, maximum);
        }
        ValidateRepresentableBounds(minimum, maximum, binCount, width, nameof(binCount), binCount);
        return new ChartHistogramBinLayout(minimum, maximum, binCount, width);
    }

    /// <summary>Preserves the requested width and aligns all boundaries to its multiples, extending the domain to cover the data.</summary>
    public static ChartHistogramBinLayout FromWidth(double minimum, double maximum, double binWidth) => FromWidth(minimum, maximum, binWidth, true);

    /// <summary>Preserves the width, using aligned equal-width bins or exact bounds with a possible final remainder bin.</summary>
    /// <param name="minimum">The lowest data value.</param>
    /// <param name="maximum">The highest data value.</param>
    /// <param name="binWidth">The caller's positive bin width.</param>
    /// <param name="roundBounds">Whether to align bounds to multiples of the width.</param>
    /// <returns>A reusable bin layout.</returns>
    public static ChartHistogramBinLayout FromWidth(double minimum, double maximum, double binWidth, bool roundBounds) {
        ValidateRange(minimum, maximum);
        ChartGuards.Finite(binWidth, nameof(binWidth));
        if (binWidth <= 0) throw new ArgumentOutOfRangeException(nameof(binWidth), binWidth, "Histogram bin width must be greater than zero.");
        if (roundBounds) {
            var lower = Math.Min(minimum, Math.Floor(NormalizeNearInteger(minimum / binWidth)) * binWidth);
            var upper = Math.Max(maximum, Math.Ceiling(NormalizeNearInteger(maximum / binWidth)) * binWidth);
            if (upper <= lower) upper = lower + binWidth;
            minimum = lower; maximum = upper;
            ValidateRange(minimum, maximum);
        } else if (minimum == maximum) return new ChartHistogramBinLayout(minimum, maximum, 1, binWidth);

        var quotient = NormalizeNearInteger((maximum - minimum) / binWidth);
        if (double.IsInfinity(quotient) || quotient > int.MaxValue) {
            throw new ArgumentOutOfRangeException(nameof(binWidth), binWidth, "Histogram bin width produces too many bins.");
        }

        var count = Math.Max(1, (int)Math.Ceiling(quotient));
        ValidateRepresentableBounds(minimum, maximum, count, binWidth, nameof(binWidth), binWidth);
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
        if (TryGetDecimalIndex(value, out var decimalIndex)) return Math.Max(0, Math.Min(Count - 1, decimalIndex));
        var quotient = (value - Minimum) / Width;
        return Math.Max(0, Math.Min(Count - 1, (int)Math.Floor(quotient)));
    }

    private bool TryGetDecimalIndex(double value, out int index) {
        index = 0;
        if (!_decimalMinimum.HasValue || !_decimalWidth.HasValue || _decimalWidth.Value <= 0 ||
            !TryConvertRoundTripDecimal(value, out var decimalValue)) return false;

        try {
            var quotient = (decimalValue - _decimalMinimum.Value) / _decimalWidth.Value;
            if (quotient < 0 || quotient > int.MaxValue) return false;
            index = decimal.ToInt32(decimal.Floor(quotient));
            return true;
        } catch (OverflowException) {
            return false;
        }
    }

    private static bool TryConvertRoundTripDecimal(double value, out decimal result) =>
        decimal.TryParse(value.ToString("R", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out result);

    private static void ValidateRange(double minimum, double maximum) {
        ChartGuards.Finite(minimum, nameof(minimum));
        ChartGuards.Finite(maximum, nameof(maximum));
        if (maximum < minimum) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Histogram maximum must be greater than or equal to its minimum.");
        if (double.IsInfinity(maximum - minimum)) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Histogram range must be finite.");
    }

    private static double NormalizeNearInteger(double value) {
        if (double.IsNaN(value) || double.IsInfinity(value)) return value;
        var nearestInteger = Math.Round(value);
        var magnitude = Math.Max(1d, Math.Abs(value));
        var bits = BitConverter.DoubleToInt64Bits(magnitude);
        var next = BitConverter.Int64BitsToDouble(bits + 1);
        var tolerance = (next - magnitude) * 4;
        return Math.Abs(value - nearestInteger) <= tolerance ? nearestInteger : value;
    }

    private static void ValidateRepresentableBounds(double minimum, double maximum, int count, double width, string parameterName, object actualValue) {
        if (count <= 1) return;
        var finalLowerBound = minimum + width * (count - 1);
        if (finalLowerBound >= maximum) {
            throw new ArgumentOutOfRangeException(parameterName, actualValue,
                "Histogram bins must have strictly increasing bounds representable by double values for the requested range.");
        }

        if (HasClearlySeparatedBounds(minimum, maximum, count, width)) return;
        if (count > MaximumBoundaryValidationScanCount) {
            throw new ArgumentOutOfRangeException(parameterName, actualValue,
                "Histogram bin precision requires validating too many internal bounds; use fewer bins or a wider numeric range.");
        }

        var previous = minimum;
        for (var index = 1; index < count; index++) {
            var current = minimum + width * index;
            if (current <= previous || current >= maximum) {
                throw new ArgumentOutOfRangeException(parameterName, actualValue,
                    "Histogram bins must have strictly increasing bounds representable by double values for the requested range.");
            }

            previous = current;
        }
    }

    private static bool HasClearlySeparatedBounds(double minimum, double maximum, int count, double width) {
        var lastOffset = width * (count - 1);
        var productUlp = GetUpperUlp(Math.Abs(lastOffset));
        var boundMagnitude = Math.Max(Math.Abs(minimum), Math.Abs(maximum));
        var boundUlp = GetUpperUlp(boundMagnitude);
        var errorBudget = productUlp + boundUlp;
        return !double.IsInfinity(errorBudget) && width > errorBudget;
    }

    private static double GetUpperUlp(double magnitude) {
        var bits = BitConverter.DoubleToInt64Bits(magnitude);
        var next = BitConverter.Int64BitsToDouble(bits + 1);
        return next - magnitude;
    }

    private void ValidateIndex(int index) {
        if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index), index, "Histogram bin index is outside the layout.");
    }
}
