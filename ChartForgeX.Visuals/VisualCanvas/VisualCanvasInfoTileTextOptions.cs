using System;

namespace ChartForgeX.Composition;

/// <summary>Immutable text scale and per-role font sizes for an information tile.</summary>
public readonly struct VisualCanvasInfoTileTextOptions {
    /// <summary>Creates tile text sizing overrides. Null values inherit the canvas style or theme.</summary>
    public VisualCanvasInfoTileTextOptions(double? textScale = null, double? labelFontSize = null, double? valueFontSize = null, double? detailFontSize = null) {
        TextScale = ValidateOptionalPositive(textScale, nameof(textScale));
        LabelFontSize = ValidateOptionalPositive(labelFontSize, nameof(labelFontSize));
        ValueFontSize = ValidateOptionalPositive(valueFontSize, nameof(valueFontSize));
        DetailFontSize = ValidateOptionalPositive(detailFontSize, nameof(detailFontSize));
    }

    /// <summary>Scales all tile text and its supporting layout; null inherits the canvas style.</summary>
    public double? TextScale { get; }
    /// <summary>Overrides the theme label size before text scaling and fit adjustment.</summary>
    public double? LabelFontSize { get; }
    /// <summary>Overrides the theme value size before text scaling and fit adjustment.</summary>
    public double? ValueFontSize { get; }
    /// <summary>Overrides the theme detail size before text scaling and fit adjustment.</summary>
    public double? DetailFontSize { get; }

    private static double? ValidateOptionalPositive(double? value, string parameterName) {
        if (!value.HasValue) return null;
        if (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value <= 0) {
            throw new ArgumentOutOfRangeException(parameterName, value, "Font sizes and text scale must be finite and greater than zero.");
        }
        return value;
    }
}
