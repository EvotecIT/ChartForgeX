using System;
using System.Collections.Generic;

namespace ChartForgeX.Markup;

public sealed partial class MarkupChartParser {
    private static Dictionary<string, string> Attributes(List<string> tokens, int start) {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = start; i < tokens.Count; i++) {
            var split = AttributeSplitIndex(tokens[i]);
            if (split <= 0) continue;
            attributes[NormalizeKey(tokens[i].Substring(0, split))] = tokens[i].Substring(split + 1);
        }

        return attributes;
    }

    private static bool IsAttribute(string token) => AttributeSplitIndex(token) > 0;

    private static bool TrySplitAttribute(string token, out string key, out string value) {
        var split = AttributeSplitIndex(token);
        if (split <= 0) {
            key = string.Empty;
            value = string.Empty;
            return false;
        }

        key = token.Substring(0, split);
        value = token.Substring(split + 1);
        return true;
    }

    private static int AttributeSplitIndex(string token) {
        var colon = token.IndexOf(':');
        var equals = token.IndexOf('=');
        if (colon <= 0) return equals;
        if (equals <= 0) return colon;
        return Math.Min(colon, equals);
    }

}
