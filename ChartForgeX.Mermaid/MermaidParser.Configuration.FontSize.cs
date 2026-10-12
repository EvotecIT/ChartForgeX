using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    private static double? ReadConfigurationFontSize(MermaidConfigurationSetting? setting, MermaidParseResult<MermaidDocument> result, out bool invalid) {
        invalid = false;
        if (setting == null) return null;
        var value = setting.Value?.Trim();
        if (setting.TryGetNumber(out var numeric) &&
            numeric >= 1 && numeric <= 256) return numeric;
        if (value != null && value.Length <= 32 &&
            Regex.IsMatch(value, @"^\+?(?:[0-9]*\.[0-9]+|[0-9]+)(?:[eE][+-]?[0-9]+)?(?:px)?$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase) &&
            double.TryParse(value.EndsWith("px", StringComparison.OrdinalIgnoreCase) ? value.Substring(0, value.Length - 2) : value,
                NumberStyles.Float, CultureInfo.InvariantCulture, out var size) && size >= 1 && size <= 256) return size;
        if (setting.IsString && value != null && value.Length <= 4096 &&
            Regex.IsMatch(value, @"^(?:\+?(?:[0-9]*\.[0-9]+|[0-9]+)(?:[eE][+-]?[0-9]+)?(?:r?(?:em|ex|cap|ch|ic|lh)|pt|pc|in|cm|mm|q|[sld]?v(?:h|w|i|b|min|max)|cq(?:h|w|i|b|min|max)|%)|(?:calc|var|min|max|clamp|env)\(.+\)|xx-small|x-small|small|medium|large|x-large|xx-large|xxx-large|smaller|larger|math|inherit|initial|unset|revert|revert-layer)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline) && HasBalancedFontSizeFunction(value)) return null;
        invalid = true;
        ConfigurationError(result, setting.Span, "Mermaid configuration 'themeVariables.fontSize' must be a pixel size from 1 through 256, such as 18 or '18px'.");
        return null;
    }

    // Functions remain unapplied metadata. Check their lexical boundary without evaluating CSS expressions.
    private static bool HasBalancedFontSizeFunction(string value) {
        var opening = value.IndexOf('(');
        if (opening < 0) return true;
        var depth = 0;
        var quote = '\0';
        for (var index = opening; index < value.Length; index++) {
            var character = value[index];
            if (character == '\\') {
                if (++index == value.Length) return false;
                continue;
            }
            if (quote != '\0') {
                if (character == quote) quote = '\0';
                else if (character == '\r' || character == '\n' || character == '\f') return false;
                continue;
            }
            if (character == '\'' || character == '"') {
                quote = character;
                continue;
            }
            if (character == '/' && index + 1 < value.Length && value[index + 1] == '*') {
                var end = value.IndexOf("*/", index + 2, StringComparison.Ordinal);
                if (end < 0) return false;
                index = end + 1;
                continue;
            }
            if (character == '(') depth++;
            else if (character == ')' && --depth == 0) return index == value.Length - 1;
        }
        return false;
    }
}
