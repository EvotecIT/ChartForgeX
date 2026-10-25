using System;
using System.Globalization;
using System.Xml;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidConfigurationTypography {
    internal static bool TryFamily(string? text, out string? value) {
        value = null;
        if (string.IsNullOrWhiteSpace(text) || text!.Length > 4096 || text.IndexOfAny(new[] { ';', '{', '}', '<', '>', '\r', '\n' }) >= 0) return false;
        try { XmlConvert.VerifyXmlChars(text); } catch (XmlException) { return false; }
        value = text; return true;
    }
    internal static bool TrySize(string? text, out double value) {
        text = text?.Trim();
        if (text != null && text.EndsWith("px", StringComparison.OrdinalIgnoreCase)) text = text.Substring(0, text.Length - 2).TrimEnd();
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
            && !double.IsNaN(value) && !double.IsInfinity(value) && value > 0 && value <= 512;
    }
    internal static bool TryWeight(string? text, out string? value) {
        value = text?.Trim();
        return string.Equals(value, "normal", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "bold", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "bolder", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "lighter", StringComparison.OrdinalIgnoreCase)
            || int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var numeric) && numeric >= 1 && numeric <= 1000;
    }
}
