using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    private static double? ReadConfigurationFontSize(MermaidConfigurationSetting? setting, MermaidParseResult<MermaidDocument> result, out bool invalid) {
        invalid = false;
        if (setting == null) return null;
        var value = setting.Value?.Trim();
        if (setting.IsString && value != null && value.Length <= 4096) value = TrimFontSizeTrivia(value);
        if (setting.TryGetNumber(out var numeric) &&
            numeric >= 1 && numeric <= 256) return numeric;
        if (value != null && value.Length <= 32 &&
            Regex.IsMatch(value, @"^\+?(?:[0-9]*\.[0-9]+|[0-9]+)(?:[eE][+-]?[0-9]+)?(?:px)?$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase) &&
            double.TryParse(value.EndsWith("px", StringComparison.OrdinalIgnoreCase) ? value.Substring(0, value.Length - 2) : value,
                NumberStyles.Float, CultureInfo.InvariantCulture, out var size) && size >= 1 && size <= 256) return size;
        if (setting.IsString && value != null && value.Length <= 4096 &&
            Regex.IsMatch(value, @"^(?:\+?(?:[0-9]*\.[0-9]+|[0-9]+)(?:[eE][+-]?[0-9]+)?(?:r?(?:em|ex|cap|ch|ic|lh)|pt|pc|in|cm|mm|q|[sld]?v(?:h|w|i|b|min|max)|cq(?:h|w|i|b|min|max)|%)|(?:calc|var|min|max|clamp|env|round|mod|rem|hypot|abs|attr)\(.+\)|xx-small|x-small|small|medium|large|x-large|xx-large|xxx-large|smaller|larger|math|inherit|initial|unset|revert|revert-layer)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline) && HasBalancedFontSizeFunction(value)) return null;
        invalid = true;
        ConfigurationError(result, setting.Span, "Mermaid configuration 'themeVariables.fontSize' must be a pixel size from 1 through 256, such as 18 or '18px'.");
        return null;
    }

    // Discard only surrounding CSS whitespace/comments. Keep internal token boundaries,
    // quoted comment markers and the original setting value intact.
    private static string? TrimFontSizeTrivia(string value) {
        var start = -1;
        var end = 0;
        var quote = '\0';
        for (var index = 0; index < value.Length; index++) {
            var character = value[index];
            if (quote == '\0') {
                if (character == ' ' || character == '\t' || character == '\r' || character == '\n' || character == '\f') continue;
                if (character == '/' && index + 1 < value.Length && value[index + 1] == '*') {
                    var close = value.IndexOf("*/", index + 2, StringComparison.Ordinal);
                    if (close < 0) return null;
                    index = close + 1;
                    continue;
                }
            }
            if (start < 0) start = index;
            if (character == '\\' && index + 1 < value.Length) index++;
            else if (character == quote) quote = '\0';
            else if (quote == '\0' && (character == '\'' || character == '"')) quote = character;
            end = index + 1;
        }
        return start < 0 ? null : value.Substring(start, end - start);
    }

    // Functions remain unapplied metadata. Check their lexical boundary without evaluating CSS expressions.
    private static bool HasBalancedFontSizeFunction(string value) {
        var opening = value.IndexOf('(');
        if (opening < 0) return true;
        var blocks = new Stack<char>();
        var quote = '\0';
        for (var index = opening; index < value.Length; index++) {
            var character = value[index];
            if (character == '\\') {
                if (++index == value.Length) return false;
                if (quote == '\0' && (value[index] == '\r' || value[index] == '\n' || value[index] == '\f')) return false;
                if (Uri.IsHexDigit(value[index])) {
                    for (var digits = 1; digits < 6 && index + 1 < value.Length && Uri.IsHexDigit(value[index + 1]); digits++) index++;
                    if (index + 1 < value.Length && (value[index + 1] == ' ' || value[index + 1] == '\t' ||
                        value[index + 1] == '\r' || value[index + 1] == '\n' || value[index + 1] == '\f')) index++;
                }
                if (value[index] == '\r' && index + 1 < value.Length && value[index + 1] == '\n') index++;
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
            if (character == '(' || character == '[' || character == '{') blocks.Push(character);
            else if (character == ')' || character == ']' || character == '}') {
                if (blocks.Count == 0) return false;
                var expected = character == ')' ? '(' : character == ']' ? '[' : '{';
                if (blocks.Pop() != expected) return false;
                if (blocks.Count == 0) return index == value.Length - 1;
            }
        }
        return false;
    }
}
