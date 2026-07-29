using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ChartForgeX.Terminal;

internal static class TerminalTextWidth {
    public static int Measure(string value) {
        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }
        var width = 0;
        for (var index = 0; index < value.Length;) {
            width += ElementWidth(NextElement(value, ref index));
        }
        return width;
    }

    public static int ElementCount(string value) {
        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }
        var count = 0;
        for (var index = 0; index < value.Length;) {
            NextElement(value, ref index);
            count++;
        }
        return count;
    }

    public static string Fit(string value, int maximum) {
        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }
        if (maximum <= 0) {
            return string.Empty;
        }
        if (Measure(value) <= maximum) {
            return value;
        }
        if (maximum == 1) {
            return "…";
        }

        var output = new StringBuilder(value.Length);
        var width = 0;
        for (var index = 0; index < value.Length;) {
            var element = NextElement(value, ref index);
            var elementWidth = ElementWidth(element);
            if (width + elementWidth + 1 > maximum) {
                break;
            }
            output.Append(element);
            width += elementWidth;
        }

        output.Append('…');
        return output.ToString();
    }

    public static string Fit(string value, double maximum, Func<string, double> measure) {
        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }
        if (measure == null) {
            throw new ArgumentNullException(nameof(measure));
        }
        if (maximum <= 0) {
            return string.Empty;
        }
        if (measure(value) <= maximum) {
            return value;
        }

        const string ellipsis = "…";
        if (measure(ellipsis) > maximum) {
            return string.Empty;
        }

        var output = new StringBuilder(value.Length);
        for (var index = 0; index < value.Length;) {
            var element = NextElement(value, ref index);
            output.Append(element);
            if (measure(output.ToString() + ellipsis) <= maximum) {
                continue;
            }
            output.Length -= element.Length;
            break;
        }

        output.Append(ellipsis);
        return output.ToString();
    }

    public static IEnumerable<string> Wrap(string value, int maximum) {
        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }
        if (maximum <= 0) {
            throw new ArgumentOutOfRangeException(nameof(maximum));
        }
        if (value.Length == 0) {
            yield return string.Empty;
            yield break;
        }

        var output = new StringBuilder(Math.Min(value.Length, maximum));
        var width = 0;
        for (var index = 0; index < value.Length;) {
            var element = NextElement(value, ref index);
            var elementWidth = ElementWidth(element);
            if (output.Length > 0 && width + elementWidth > maximum) {
                yield return output.ToString();
                output.Clear();
                width = 0;
            }

            output.Append(element);
            width += elementWidth;
        }

        if (output.Length > 0) {
            yield return output.ToString();
        }
    }

    private static int ElementWidth(string element) {
        if (TryPreservedScalar(element, 0, out var escapeLength, out var preservedCodePoint) && escapeLength == element.Length) {
            return IsWide(preservedCodePoint) ? 2 : 1;
        }

        var width = 0;
        for (var index = 0; index < element.Length;) {
            var codePoint = ReadCodePoint(element, ref index);
            var category = UnicodeCategoryFor(codePoint);
            if (category == UnicodeCategory.NonSpacingMark ||
                category == UnicodeCategory.SpacingCombiningMark ||
                category == UnicodeCategory.EnclosingMark ||
                codePoint == 0x200D ||
                codePoint >= 0xFE00 && codePoint <= 0xFE0F ||
                codePoint >= 0xE0100 && codePoint <= 0xE01EF) {
                continue;
            }

            width = Math.Max(width, IsWide(codePoint) ? 2 : 1);
        }

        return Math.Max(1, width);
    }

    private static string NextElement(string value, ref int index) {
        if (TryPreservedScalar(value, index, out var escapeLength, out _)) {
            var preserved = value.Substring(index, escapeLength);
            index += escapeLength;
            return preserved;
        }

        var element = StringInfo.GetNextTextElement(value, index);
        index += element.Length;
        return element;
    }

    internal static bool TryPreservedScalar(string value, int index, out int length, out int codePoint) {
        length = 0;
        codePoint = 0;
        if (index + 10 > value.Length ||
            value[index] != TerminalPngTextPreserver.EscapeStart ||
            value[index + 1] != '[' ||
            value[index + 2] != 'U' ||
            value[index + 3] != '+') {
            return false;
        }

        var digits = 0;
        var scalar = 0;
        for (var cursor = index + 4; cursor < value.Length; cursor++) {
            if (value[cursor] == ']') {
                if (cursor + 1 >= value.Length ||
                    value[cursor + 1] != TerminalPngTextPreserver.EscapeEnd ||
                    digits < 4 ||
                    scalar > 0x10FFFF ||
                    scalar >= 0xD800 && scalar <= 0xDFFF) {
                    return false;
                }
                length = cursor - index + 2;
                codePoint = scalar;
                return true;
            }
            if (digits >= 6 || !IsUpperHex(value[cursor])) {
                return false;
            }
            scalar = scalar * 16 + HexValue(value[cursor]);
            digits++;
        }
        return false;
    }

    private static bool IsUpperHex(char value) {
        return value >= '0' && value <= '9' || value >= 'A' && value <= 'F';
    }

    private static int HexValue(char value) {
        return value <= '9' ? value - '0' : value - 'A' + 10;
    }

    private static UnicodeCategory UnicodeCategoryFor(int codePoint) {
        if (codePoint <= char.MaxValue) {
            return char.GetUnicodeCategory((char)codePoint);
        }
        var scalar = char.ConvertFromUtf32(codePoint);
        return CharUnicodeInfo.GetUnicodeCategory(scalar, 0);
    }

    private static int ReadCodePoint(string value, ref int index) {
        var first = value[index++];
        if (!char.IsHighSurrogate(first) || index >= value.Length || !char.IsLowSurrogate(value[index])) {
            return first;
        }
        return char.ConvertToUtf32(first, value[index++]);
    }

    private static bool IsWide(int codePoint) {
        return codePoint >= 0x1100 && (
            codePoint <= 0x115F ||
            codePoint == 0x2329 ||
            codePoint == 0x232A ||
            codePoint >= 0x2E80 && codePoint <= 0xA4CF && codePoint != 0x303F ||
            codePoint >= 0xAC00 && codePoint <= 0xD7A3 ||
            codePoint >= 0xF900 && codePoint <= 0xFAFF ||
            codePoint >= 0xFE10 && codePoint <= 0xFE19 ||
            codePoint >= 0xFE30 && codePoint <= 0xFE6F ||
            codePoint >= 0xFF00 && codePoint <= 0xFF60 ||
            codePoint >= 0xFFE0 && codePoint <= 0xFFE6 ||
            codePoint >= 0x1F000 && codePoint <= 0x1FAFF ||
            codePoint >= 0x20000 && codePoint <= 0x3FFFD);
    }
}
