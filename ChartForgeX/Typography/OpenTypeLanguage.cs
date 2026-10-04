using System;

namespace ChartForgeX.Typography;

/// <summary>Validates explicit OpenType language tags without guessing a culture-to-font mapping.</summary>
internal static class OpenTypeLanguage {
    internal static string? Normalize(string? value) {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var tag = value!.Trim();
        if (tag.Length > 4) throw new ArgumentException("OpenType language tags contain at most four ASCII letters or digits.", nameof(value));
        foreach (var ch in tag) if (!(ch >= 'A' && ch <= 'Z' || ch >= 'a' && ch <= 'z' || ch >= '0' && ch <= '9'))
            throw new ArgumentException("OpenType language tags contain only ASCII letters or digits.", nameof(value));
        return tag.PadRight(4);
    }

    /// <summary>Invalid CSS declarations are ignored; normal explicitly restores the default language system.</summary>
    internal static bool TryCss(string value, out string? tag) {
        tag = null;
        value = value.Trim();
        if (value.Equals("normal", StringComparison.OrdinalIgnoreCase)) return true;
        if (value.Length < 2 || value[0] != value[value.Length - 1] || value[0] != '\'' && value[0] != '"') return false;
        try { tag = Normalize(value.Substring(1, value.Length - 2)); return tag != null; }
        catch (ArgumentException) { return false; }
    }
}
