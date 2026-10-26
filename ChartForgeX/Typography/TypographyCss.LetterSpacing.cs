using System;
using System.Globalization;

namespace ChartForgeX.Typography;

internal static partial class TypographyCss {
    /// <summary>Resolves the finite authored native length after the same element's font-size cascade.</summary>
    internal static bool TryLetterSpacing(string value, double fontSize, out double spacing, bool allowUserUnits = false) {
        spacing = 0; var text = value.Trim();
        if (text.Equals("normal", StringComparison.OrdinalIgnoreCase)) return true;
        var em = text.EndsWith("em", StringComparison.OrdinalIgnoreCase);
        var px = text.EndsWith("px", StringComparison.OrdinalIgnoreCase);
        if (em || px) text = text.Substring(0, text.Length - 2).Trim();
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            || double.IsNaN(parsed) || double.IsInfinity(parsed) || !em && !px && parsed != 0 && !allowUserUnits) return false;
        spacing = em ? parsed * fontSize : parsed;
        return !double.IsNaN(spacing) && !double.IsInfinity(spacing);
    }
}
