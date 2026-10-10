using System;

namespace ChartForgeX.Core;

/// <summary>Direction-specific candlestick body, wick and OHLC stroke options.</summary>
public sealed class ChartFinancialOptions {
    /// <summary>Gets options for observations whose close is greater than or equal to their open, including doji.</summary>
    public ChartFinancialItemOptions Rising { get; } = new();

    /// <summary>Gets options for observations whose close is less than their open.</summary>
    public ChartFinancialItemOptions Falling { get; } = new();

    internal bool IsConfigured => Rising.IsConfigured || Falling.IsConfigured;
}

/// <summary>Paint and width overrides for one financial direction. Null values inherit the existing series or family default.</summary>
public sealed class ChartFinancialItemOptions {
    private double? _fillOpacity, _strokeOpacity, _strokeWidth;

    /// <summary>Gets or sets the candlestick body fill. Explicit observation colors take precedence. OHLC does not support fill.</summary>
    public ChartColor? Fill { get; set; }

    /// <summary>Gets or sets the candlestick fill alpha multiplier, from zero to one. Zero produces a hollow body without a pattern or interior wick.</summary>
    public double? FillOpacity { get => _fillOpacity; set { Opacity(value); _fillOpacity = value; } }

    /// <summary>Gets or sets the body outline or OHLC stroke. Explicit observation colors take precedence.</summary>
    public ChartColor? Stroke { get; set; }

    /// <summary>Gets or sets the stroke alpha multiplier, from zero to one. The default multiplier is one.</summary>
    public double? StrokeOpacity { get => _strokeOpacity; set { Opacity(value); _strokeOpacity = value; } }

    /// <summary>Gets or sets the logical stroke width. Null inherits an explicit series width or the family default; zero removes the stroke.</summary>
    public double? StrokeWidth { get => _strokeWidth; set { Width(value); _strokeWidth = value; } }

    /// <summary>Gets candlestick wick overrides. Unspecified wick values inherit this item's stroke values. OHLC does not support wick options.</summary>
    public ChartFinancialWickOptions Wick { get; } = new();

    internal bool HasFill => Fill.HasValue || FillOpacity.HasValue;
    internal bool IsConfigured => HasFill || Stroke.HasValue || StrokeOpacity.HasValue || StrokeWidth.HasValue || Wick.IsConfigured;

    internal static void Opacity(double? value) {
        if (!value.HasValue) return;
        ChartGuards.Finite(value.Value, nameof(value));
        if (value.Value < 0 || value.Value > 1) throw new ArgumentOutOfRangeException(nameof(value), "Financial opacity must be between zero and one.");
    }

    internal static void Width(double? value) {
        if (!value.HasValue) return;
        ChartGuards.Finite(value.Value, nameof(value));
        if (value.Value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Financial stroke widths cannot be negative.");
    }
}

/// <summary>Candlestick wick stroke overrides, inherited from the direction's body outline when null.</summary>
public sealed class ChartFinancialWickOptions {
    private double? _strokeOpacity, _strokeWidth;

    /// <summary>Gets or sets the wick color. Explicit observation colors take precedence.</summary>
    public ChartColor? Stroke { get; set; }

    /// <summary>Gets or sets the wick alpha multiplier, from zero to one. Null inherits the item stroke multiplier.</summary>
    public double? StrokeOpacity { get => _strokeOpacity; set { ChartFinancialItemOptions.Opacity(value); _strokeOpacity = value; } }

    /// <summary>Gets or sets the logical wick width. Null inherits the item stroke width; zero removes the wick.</summary>
    public double? StrokeWidth { get => _strokeWidth; set { ChartFinancialItemOptions.Width(value); _strokeWidth = value; } }

    internal bool IsConfigured => Stroke.HasValue || StrokeOpacity.HasValue || StrokeWidth.HasValue;
}
