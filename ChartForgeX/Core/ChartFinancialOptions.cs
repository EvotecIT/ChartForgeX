using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>Direction-specific candlestick body, wick and OHLC stroke options.</summary>
public sealed class ChartFinancialOptions {
    /// <summary>Gets options for observations whose close is greater than or equal to their open, including doji.</summary>
    public ChartFinancialItemOptions Rising { get; } = new();

    /// <summary>Gets options for observations whose close is less than their open.</summary>
    public ChartFinancialItemOptions Falling { get; } = new();

    internal bool IsConfigured => Rising.IsConfigured || Falling.IsConfigured;
}

/// <summary>Paint, width and dash overrides for one financial direction. Null values inherit the existing series or family default.</summary>
public sealed class ChartFinancialItemOptions {
    private double? _fillOpacity, _strokeOpacity, _strokeWidth;
    private IReadOnlyList<double>? _dashPattern;
    private double? _dashOffset;

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

    /// <summary>Gets or sets a copied pattern of positive finite dash and gap lengths in logical pixels. Null or empty is solid. Odd-length patterns repeat twice; the complete repeated period must be finite.</summary>
    public IReadOnlyList<double>? DashPattern { get => _dashPattern; set => _dashPattern = CopyPattern(value); }

    /// <summary>Gets or sets a finite signed dash phase in logical pixels. Null means zero. Each OHLC stem or tick and each candlestick outline starts its own phase.</summary>
    public double? DashOffset { get => _dashOffset; set { Offset(value); _dashOffset = value; } }

    /// <summary>Gets candlestick wick overrides. Unspecified wick values inherit this item's stroke values. OHLC does not support wick options.</summary>
    public ChartFinancialWickOptions Wick { get; } = new();

    internal bool HasFill => Fill.HasValue || FillOpacity.HasValue;
    internal bool IsConfigured => HasFill || Stroke.HasValue || StrokeOpacity.HasValue || StrokeWidth.HasValue
        || DashPattern != null || DashOffset.HasValue || Wick.IsConfigured;

    internal static IReadOnlyList<double>? CopyPattern(IReadOnlyList<double>? value) {
        if (value == null) return null;
        var copy = new double[value.Count];
        for (var i = 0; i < copy.Length; i++) {
            var length = value[i];
            ChartGuards.Finite(length, nameof(value));
            if (length <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Financial dash lengths must be positive.");
            copy[i] = length;
        }
        if (double.IsInfinity(ChartStrokeDashes.Period(copy)))
            throw new ArgumentOutOfRangeException(nameof(value), "The complete financial dash period must be finite.");
        return Array.AsReadOnly(copy);
    }

    internal static void Offset(double? value) {
        if (value.HasValue) ChartGuards.Finite(value.Value, nameof(value));
    }

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
    private IReadOnlyList<double>? _dashPattern;
    private double? _dashOffset;

    /// <summary>Gets or sets the wick color. Explicit observation colors take precedence.</summary>
    public ChartColor? Stroke { get; set; }

    /// <summary>Gets or sets the wick alpha multiplier, from zero to one. Null inherits the item stroke multiplier.</summary>
    public double? StrokeOpacity { get => _strokeOpacity; set { ChartFinancialItemOptions.Opacity(value); _strokeOpacity = value; } }

    /// <summary>Gets or sets the logical wick width. Null inherits the item stroke width; zero removes the wick.</summary>
    public double? StrokeWidth { get => _strokeWidth; set { ChartFinancialItemOptions.Width(value); _strokeWidth = value; } }

    /// <summary>Gets or sets a copied pattern of positive finite logical-pixel lengths. Null inherits the item pattern; empty makes the wick solid. Odd-length patterns repeat twice; the complete repeated period must be finite.</summary>
    public IReadOnlyList<double>? DashPattern { get => _dashPattern; set => _dashPattern = ChartFinancialItemOptions.CopyPattern(value); }

    /// <summary>Gets or sets a finite signed logical-pixel phase. Null independently inherits the item offset. Each emitted wick segment starts its own phase.</summary>
    public double? DashOffset { get => _dashOffset; set { ChartFinancialItemOptions.Offset(value); _dashOffset = value; } }

    internal bool IsConfigured => Stroke.HasValue || StrokeOpacity.HasValue || StrokeWidth.HasValue || DashPattern != null || DashOffset.HasValue;
}
