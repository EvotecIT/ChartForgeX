using System;
using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Mermaid;

/// <summary>Translates a bounded documented source vocabulary into the existing immutable native number policy.</summary>
internal static class MermaidTreemapValueFormat {
    internal static bool TryRead(string source, out ChartValueFormat? format) {
        format = null;
        if (source.Length > 32) return false;
        var currency = source.Length > 0 && source[0] == '$';
        var token = source.Length == 0 ? "," : currency ? source.Substring(1) : source;
        // These documented currency aliases request grouping, independently of source magnitudes.
        if (currency && token == "0,0") token = ",";
        if (currency && token.Length == 0) {
            format = ChartValueFormat.Number("'$'0.###############", CultureInfo.InvariantCulture);
            return true;
        }
        var grouped = token.StartsWith(",", StringComparison.Ordinal);
        if (grouped) token = token.Substring(1);
        var number = grouped ? "#,0" : "0";
        if (token.Length == 0) {
            if (!grouped) return false;
            number += ".###############";
        } else {
            if (token.Length < 3 || token[0] != '.' || token[token.Length - 1] is not ('f' or '%')) return false;
            if (!int.TryParse(token.Substring(1, token.Length - 2), NumberStyles.None, CultureInfo.InvariantCulture, out var precision)
                || precision < 0 || precision > 15) return false;
            if (precision > 0) number += "." + new string('0', precision);
            if (token[token.Length - 1] == '%') {
                if (currency) return false;
                number += "%";
            }
        }
        format = ChartValueFormat.Number((currency ? "'$'" : "") + number, CultureInfo.InvariantCulture);
        return true;
    }
}
