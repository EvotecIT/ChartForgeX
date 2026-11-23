using System;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets the shared direction-specific candlestick and OHLC configuration.</summary>
    public ChartFinancialOptions Financial {
        get => _financial;
        set => _financial = value ?? throw new ArgumentNullException(nameof(value));
    }
    private ChartFinancialOptions _financial = new();

    internal void ValidateFinancialOptions() {
        if (!Financial.IsConfigured) return;
        if (Kind != ChartSeriesKind.Candlestick && Kind != ChartSeriesKind.Ohlc)
            throw new InvalidOperationException("Series '" + Name + "' of kind " + Kind + " does not support financial options.");
        if (Kind == ChartSeriesKind.Ohlc && (Financial.Rising.HasFill || Financial.Falling.HasFill
            || Financial.Rising.Wick.IsConfigured || Financial.Falling.Wick.IsConfigured))
            throw new InvalidOperationException("OHLC series '" + Name + "' supports financial stroke options only; fill and wick options require Candlestick.");
    }
}
