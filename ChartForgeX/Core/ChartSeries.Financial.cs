using System;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets the shared direction-specific candlestick and OHLC configuration.</summary>
    public ChartFinancialOptions Financial { get; } = new();

    /// <summary>Configures financial body, wick and OHLC paint without changing the source observations.</summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The current series.</returns>
    public ChartSeries ConfigureFinancial(Action<ChartFinancialOptions> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(Financial); return this;
    }

    internal void ValidateFinancialOptions() {
        if (!Financial.IsConfigured) return;
        if (Kind != ChartSeriesKind.Candlestick && Kind != ChartSeriesKind.Ohlc)
            throw new InvalidOperationException("Series '" + Name + "' of kind " + Kind + " does not support financial options.");
        if (Kind == ChartSeriesKind.Ohlc && (Financial.Rising.HasFill || Financial.Falling.HasFill
            || Financial.Rising.Wick.IsConfigured || Financial.Falling.Wick.IsConfigured))
            throw new InvalidOperationException("OHLC series '" + Name + "' supports financial stroke options only; fill and wick options require Candlestick.");
    }
}
