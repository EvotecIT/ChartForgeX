using System;
using System.Globalization;
using System.Text;

namespace ChartForgeX.Svg;

internal static class SvgRenderedIdentity {
    public static string CreateProvisionalId(string prefix, string idScope, params string[] identityParts) {
        var values = new string[identityParts.Length + 1];
        values[0] = idScope ?? string.Empty;
        Array.Copy(identityParts, 0, values, 1, identityParts.Length);
        return prefix + "-seed-" + StableHash(values);
    }

    public static string Bind(string svg, string provisionalId, string finalPrefix, string idScope, string separator = "-") {
        var canonicalSvg = svg.Replace("\r\n", "\n");
        var finalId = finalPrefix + separator + StableHash(idScope ?? string.Empty, canonicalSvg);
        return RebindGeneratedId(svg, provisionalId, finalId);
    }

    private static string RebindGeneratedId(string svg, string oldId, string newId) {
        var match = svg.IndexOf(oldId, StringComparison.Ordinal);
        if (match < 0) return svg;

        var writer = new StringBuilder(svg.Length);
        var cursor = 0;
        while (match >= 0) {
            writer.Append(svg, cursor, match - cursor);
            writer.Append(IsGeneratedIdReference(svg, match, oldId.Length) ? newId : oldId);
            cursor = match + oldId.Length;
            match = svg.IndexOf(oldId, cursor, StringComparison.Ordinal);
        }

        writer.Append(svg, cursor, svg.Length - cursor);
        return writer.ToString();
    }

    private static bool IsGeneratedIdReference(string svg, int index, int idLength) {
        if (HasPrefix(svg, index, "id=\"") || HasPrefix(svg, index, "url(#") || HasPrefix(svg, index, "href=\"#")) return true;
        if (HasPrefix(svg, index, "#") && index + idLength < svg.Length && svg[index + idLength] == ' ') return true;

        var attributeStart = svg.LastIndexOf("aria-", index, StringComparison.Ordinal);
        if (attributeStart < 0) return false;
        var valueStart = svg.IndexOf("=\"", attributeStart, StringComparison.Ordinal);
        var valueEnd = valueStart < 0 ? -1 : svg.IndexOf('"', valueStart + 2);
        return valueStart >= 0 && valueStart < index && valueEnd > index;
    }

    private static bool HasPrefix(string value, int index, string prefix) {
        var prefixStart = index - prefix.Length;
        return prefixStart >= 0 && string.CompareOrdinal(value, prefixStart, prefix, 0, prefix.Length) == 0;
    }

    private static string StableHash(params string[] values) {
        unchecked {
            var hash = 2166136261u;
            foreach (var value in values) Add(ref hash, value);
            return hash.ToString("x8", CultureInfo.InvariantCulture);
        }
    }

    private static void Add(ref uint hash, string value) {
        AddRaw(ref hash, value.Length.ToString(CultureInfo.InvariantCulture));
        AddRaw(ref hash, ":");
        AddRaw(ref hash, value);
        AddRaw(ref hash, "|");
    }

    private static void AddRaw(ref uint hash, string value) {
        foreach (var ch in value) {
            hash ^= ch;
            hash *= 16777619u;
        }
    }
}
