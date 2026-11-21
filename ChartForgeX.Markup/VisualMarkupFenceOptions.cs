using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Markup;

/// <summary>
/// Provides shared parsing helpers for Markdown fence attributes and option values.
/// </summary>
public static class VisualMarkupFenceOptions {
    /// <summary>
    /// Finds a fence attribute using ChartForgeX markup's case-insensitive normalized key matching.
    /// </summary>
    /// <param name="block">The visual markup block carrying fence attributes.</param>
    /// <param name="key">The attribute key to find.</param>
    /// <param name="value">The attribute value when present.</param>
    /// <returns>True when the attribute is present.</returns>
    public static bool TryGetAttribute(VisualMarkupBlock block, string key, out string value) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (block.Attributes.TryGetValue(key, out var exact)) {
            value = exact;
            return true;
        }

        var normalized = NormalizeKey(key);
        foreach (var item in block.Attributes) {
            if (NormalizeKey(item.Key) == normalized) {
                value = item.Value;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    /// <summary>
    /// Parses a boolean option value.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <param name="optionName">The option name used in diagnostics.</param>
    /// <returns>The parsed boolean value.</returns>
    public static bool ParseBoolean(string value, string optionName) {
        switch (NormalizeKey(value)) {
            case "true":
            case "yes":
            case "on":
            case "1":
                return true;
            case "false":
            case "no":
            case "off":
            case "0":
                return false;
            default:
                throw new ArgumentException("Option '" + optionName + "' requires a boolean value.");
        }
    }

    /// <summary>
    /// Parses an integer option value.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <param name="optionName">The option name used in diagnostics.</param>
    /// <returns>The parsed integer value.</returns>
    public static int ParseInt32(string value, string optionName) {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)) return result;
        throw new ArgumentException("Option '" + optionName + "' requires an integer value.");
    }

    /// <summary>
    /// Parses a double-precision option value.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <param name="optionName">The option name used in diagnostics.</param>
    /// <returns>The parsed numeric value.</returns>
    public static double ParseDouble(string value, string optionName) {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)) return result;
        throw new ArgumentException("Option '" + optionName + "' requires a numeric value.");
    }

    /// <summary>
    /// Parses an enum option value using ChartForgeX markup's normalized key matching.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="value">The source value.</param>
    /// <param name="optionName">The option name used in diagnostics.</param>
    /// <returns>The parsed enum value.</returns>
    public static TEnum ParseEnum<TEnum>(string value, string optionName) where TEnum : struct, Enum {
        var normalized = NormalizeKey(value);
        foreach (var name in Enum.GetNames(typeof(TEnum))) {
            if (NormalizeKey(name) == normalized) return (TEnum)Enum.Parse(typeof(TEnum), name, false);
        }

        throw new ArgumentException("Option '" + optionName + "' has unknown value '" + value + "'.");
    }

    /// <summary>
    /// Normalizes a markup key by keeping only letters and digits and lowercasing them.
    /// </summary>
    /// <param name="value">The source key.</param>
    /// <returns>The normalized key.</returns>
    public static string NormalizeKey(string value) =>
        new((value ?? string.Empty).Trim().Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
