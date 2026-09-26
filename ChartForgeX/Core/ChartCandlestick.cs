using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one open, high, low, and close value.
/// </summary>
public readonly struct ChartCandlestick {
    /// <summary>
    /// Gets the horizontal coordinate.
    /// </summary>
    public readonly double X;

    /// <summary>
    /// Gets the opening value.
    /// </summary>
    public readonly double Open;

    /// <summary>
    /// Gets the highest value.
    /// </summary>
    public readonly double High;

    /// <summary>
    /// Gets the lowest value.
    /// </summary>
    public readonly double Low;

    /// <summary>
    /// Gets the closing value.
    /// </summary>
    public readonly double Close;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartCandlestick"/> struct.
    /// </summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="open">The opening value.</param>
    /// <param name="high">The highest value.</param>
    /// <param name="low">The lowest value.</param>
    /// <param name="close">The closing value.</param>
    public ChartCandlestick(double x, double open, double high, double low, double close) {
        ChartPrimitiveGuards.Finite(x, nameof(x));
        ChartPrimitiveGuards.Finite(open, nameof(open));
        ChartPrimitiveGuards.Finite(high, nameof(high));
        ChartPrimitiveGuards.Finite(low, nameof(low));
        ChartPrimitiveGuards.Finite(close, nameof(close));
        if (high < open || high < close || high < low) throw new ArgumentOutOfRangeException(nameof(high), high, "Candlestick high must be greater than or equal to open, low, and close values.");
        if (low > open || low > close || low > high) throw new ArgumentOutOfRangeException(nameof(low), low, "Candlestick low must be less than or equal to open, high, and close values.");
        X = x;
        Open = open;
        High = high;
        Low = low;
        Close = close;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartCandlestick"/> struct from a date/time x value.
    /// </summary>
    /// <param name="x">The date/time x value.</param>
    /// <param name="open">The opening value.</param>
    /// <param name="high">The highest value.</param>
    /// <param name="low">The lowest value.</param>
    /// <param name="close">The closing value.</param>
    public ChartCandlestick(DateTime x, double open, double high, double low, double close) : this(ChartDateTime.ToOADate(x), open, high, low, close) {
    }
}
