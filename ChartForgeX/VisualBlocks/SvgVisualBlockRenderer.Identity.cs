using System;
using System.Globalization;
using System.Text;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static string BuildProvisionalId(IVisualBlock block, string idScope) {
        var options = block.Options;
        return "cfx-visual-seed-" + VisualBlockRendering.StableHash(
            idScope,
            block.GetType().FullName ?? block.GetType().Name,
            block.AccessibleName,
            options.Size.Width.ToString(CultureInfo.InvariantCulture),
            options.Size.Height.ToString(CultureInfo.InvariantCulture));
    }

    private static string BindVisualIdentity(string svg, string provisionalId, string idScope) {
        var canonicalSvg = svg.Replace("\r\n", "\n");
        var finalId = "cfx-visual-" + VisualBlockRendering.StableHash(idScope, canonicalSvg);
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

        const string ariaPrefix = "aria-labelledby=\"";
        var ariaStart = svg.LastIndexOf(ariaPrefix, index, StringComparison.Ordinal);
        if (ariaStart < 0) return false;
        var ariaEnd = svg.IndexOf('"', ariaStart + ariaPrefix.Length);
        return ariaEnd > index;
    }

    private static bool HasPrefix(string value, int index, string prefix) {
        var prefixStart = index - prefix.Length;
        return prefixStart >= 0 && string.CompareOrdinal(value, prefixStart, prefix, 0, prefix.Length) == 0;
    }
}
