using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private IReadOnlyList<ChartFinancialItemOptions?> _financialStyles = Array.Empty<ChartFinancialItemOptions?>();

    /// <summary>Configures one financial observation atomically. Unspecified properties inherit its rising or falling style.</summary>
    /// <param name="index">The zero-based observation index in <see cref="FinancialValues"/>.</param>
    /// <param name="configure">Configures a detached copy of the observation overrides. Explicit point colors take precedence.</param>
    /// <returns>The current series.</returns>
    /// <remarks>The installed style is copied after the callback returns. A typed item update preserves it; complete replacement clears it.</remarks>
    public ChartSeries ConfigureFinancialValue(int index, Action<ChartFinancialItemOptions> configure) {
        RequireFinancialStyleIndex(index);
        var current = index < _financialStyles.Count ? _financialStyles[index] : null;
        var candidate = ChartOptionConfiguration.Configure(current ?? new ChartFinancialItemOptions(), configure, options => options.Clone());
        ValidateFinancialItemOptions(candidate);
        var styles = CopyFinancialStyles();
        styles[index] = candidate;
        _financialStyles = Array.AsReadOnly(styles);
        return this;
    }

    /// <summary>Configures every financial observation from its source values and ordinal, installing all detached styles atomically.</summary>
    /// <param name="configure">Receives the authored prices, zero-based observation index and a detached copy of its current overrides.</param>
    /// <returns>The current series.</returns>
    /// <remarks>The callback runs once per existing observation during this call and is not retained. A failed callback or invalid OHLC style
    /// leaves all previous styles unchanged. Item updates preserve styles; complete typed replacement clears them.</remarks>
    public ChartSeries ConfigureFinancialValues(Action<ChartCandlestick, int, ChartFinancialItemOptions> configure) {
        RequireObservationKind(ChartSeriesKind.Candlestick, ChartSeriesKind.Ohlc);
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        var styles = CopyFinancialStyles();
        for (var index = 0; index < styles.Length; index++) {
            var candidate = styles[index]?.Clone() ?? new ChartFinancialItemOptions();
            configure(FinancialValues[index], index, candidate);
            ValidateFinancialItemOptions(candidate);
            styles[index] = candidate.Clone();
        }
        _financialStyles = Array.AsReadOnly(styles);
        return this;
    }

    /// <summary>Removes one financial observation's overrides so it uses its rising or falling style.</summary>
    /// <param name="index">The zero-based observation index in <see cref="FinancialValues"/>.</param>
    /// <returns>The current series.</returns>
    public ChartSeries UseSeriesFinancialStyle(int index) {
        RequireFinancialStyleIndex(index);
        if (index >= _financialStyles.Count || _financialStyles[index] == null) return this;
        var styles = CopyFinancialStyles();
        styles[index] = null;
        _financialStyles = Array.AsReadOnly(styles);
        return this;
    }

    internal bool HasFinancialStyleOverrides {
        get {
            if (Financial.HasOverrides) return true;
            foreach (var style in _financialStyles) if (style?.HasOverrides == true) return true;
            return false;
        }
    }

    /// <summary>Resolves direction and detached observation overrides through the common financial mark owner.</summary>
    internal ChartFinancialItemOptions FinancialItemOptions(int index, bool rising) {
        var direction = rising ? Financial.Rising : Financial.Falling;
        var observation = index >= 0 && index < _financialStyles.Count ? _financialStyles[index] : null;
        return observation == null ? direction : direction.Overlay(observation);
    }

    private void RequireFinancialStyleIndex(int index) {
        RequireObservationKind(ChartSeriesKind.Candlestick, ChartSeriesKind.Ohlc);
        if (index < 0 || index >= FinancialValues.Count) throw new ArgumentOutOfRangeException(nameof(index));
    }

    private ChartFinancialItemOptions?[] CopyFinancialStyles() {
        var styles = new ChartFinancialItemOptions?[FinancialValues.Count];
        for (var index = 0; index < styles.Length && index < _financialStyles.Count; index++) styles[index] = _financialStyles[index];
        return styles;
    }

    private void ValidateFinancialItemOptions(ChartFinancialItemOptions item) {
        if (Kind == ChartSeriesKind.Ohlc && (item.HasFill || item.Wick.IsConfigured))
            throw new InvalidOperationException("OHLC series '" + Name + "' supports financial stroke options only; fill and wick options require Candlestick.");
    }
}
