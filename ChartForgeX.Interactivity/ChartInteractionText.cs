using System;

namespace ChartForgeX.Interactivity;

internal static class ChartInteractionText {
    public static string RequiredText(string? value, string parameterName, string displayName) {
        if (value == null) throw new ArgumentNullException(parameterName);
        var trimmed = value.Trim();
        if (trimmed.Length == 0) throw new ArgumentException(displayName + " must not be empty.", parameterName);
        return trimmed;
    }

    public static string RequiredToken(string? value, string parameterName, string displayName) {
        var trimmed = RequiredText(value, parameterName, displayName);
        foreach (var ch in trimmed) {
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.') continue;
            throw new ArgumentException(displayName + " may contain only letters, digits, dots, underscores, and hyphens.", parameterName);
        }

        return trimmed;
    }

    public static string? OptionalText(string? value, string parameterName, string displayName) {
        if (value == null) return null;
        return RequiredText(value, parameterName, displayName);
    }

    public static string? OptionalToken(string? value, string parameterName, string displayName) {
        if (value == null) return null;
        return RequiredToken(value, parameterName, displayName);
    }
}
