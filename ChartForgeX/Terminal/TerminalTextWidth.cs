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

    public static int VisibleElementCount(string value) {
        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }
        var count = 0;
        for (var index = 0; index < value.Length;) {
            if (ElementWidth(NextElement(value, ref index)) > 0) {
                count++;
            }
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

    internal static IEnumerable<string> Elements(string value) {
        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }
        for (var index = 0; index < value.Length;) {
            yield return NextElement(value, ref index);
        }
    }

    internal static bool IsElementBoundary(string value, int offset) {
        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }
        if (offset < 0 || offset > value.Length) {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
        if (offset == 0 || offset == value.Length) {
            return true;
        }
        for (var index = 0; index < value.Length;) {
            NextElement(value, ref index);
            if (index == offset) {
                return true;
            }
            if (index > offset) {
                return false;
            }
        }
        return false;
    }

    internal static IEnumerable<string> VisibleElements(string value) {
        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }
        var units = new List<string>();
        var pending = new StringBuilder();
        foreach (var element in Elements(value)) {
            if (ElementWidth(element) == 0) {
                pending.Append(element);
                continue;
            }

            if (pending.Length == 0) {
                units.Add(element);
                continue;
            }

            pending.Append(element);
            units.Add(pending.ToString());
            pending.Clear();
        }

        if (pending.Length > 0) {
            if (units.Count == 0) {
                units.Add(pending.ToString());
            } else {
                units[units.Count - 1] += pending.ToString();
            }
        }

        foreach (var unit in units) {
            yield return unit;
        }
    }

    private static int ElementWidth(string element) {
        var width = 0;
        var emojiPresentation = false;
        for (var index = 0; index < element.Length;) {
            var codePoint = ReadScalar(element, ref index);
            if (codePoint == 0xFE0F || codePoint == 0x20E3) {
                emojiPresentation = true;
            }
            if (IsZeroWidthScalar(codePoint)) {
                continue;
            }

            width = Math.Max(width, IsWide(codePoint) ? 2 : 1);
        }

        return emojiPresentation ? Math.Max(2, width) : width;
    }

    private static string NextElement(string value, ref int index) {
        var start = index;
        var first = ReadScalar(value, ref index);
        if (first == '\r' && PeekScalar(value, index, out var next, out var nextLength) && next == '\n') {
            index += nextLength;
            return value.Substring(start, index - start);
        }

        if (IsRegionalIndicator(first) && PeekScalar(value, index, out next, out nextLength) && IsRegionalIndicator(next)) {
            index += nextLength;
            return value.Substring(start, index - start);
        }

        var previous = first;
        var hasExtendedPictographic = IsExtendedPictographic(first);
        var hasIndicConjunct = TerminalIndicConjunctBreak.IsConsonant(first);
        var hasIndicLinker = false;
        while (PeekScalar(value, index, out next, out nextLength)) {
            var category = UnicodeCategoryFor(next);
            var isIndicExtend = hasIndicConjunct && TerminalIndicConjunctBreak.IsExtend(next);
            var isIndicLinker = hasIndicConjunct && TerminalIndicConjunctBreak.IsLinker(next);
            if (IsExtend(next, category) || IsHangulContinuation(previous, next) || isIndicExtend) {
                index += nextLength;
                if (hasIndicConjunct) {
                    if (isIndicLinker) {
                        hasIndicLinker = true;
                    } else if (!isIndicExtend) {
                        hasIndicConjunct = false;
                        hasIndicLinker = false;
                    }
                }
                previous = next;
                continue;
            }
            if (hasIndicConjunct &&
                hasIndicLinker &&
                TerminalIndicConjunctBreak.IsConsonant(next)) {
                index += nextLength;
                previous = next;
                hasIndicLinker = false;
                continue;
            }
            if (next != 0x200D) {
                break;
            }

            var joinerLength = nextLength;
            if (!PeekScalar(value, index + joinerLength, out next, out nextLength) ||
                !hasExtendedPictographic ||
                !IsExtendedPictographic(next)) {
                break;
            }
            index += joinerLength;
            index += nextLength;
            previous = next;
            hasExtendedPictographic = true;
        }

        return value.Substring(start, index - start);
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

    private static int ReadScalar(string value, ref int index) {
        if (TryPreservedScalar(value, index, out var length, out var codePoint)) {
            index += length;
            return codePoint;
        }
        return ReadCodePoint(value, ref index);
    }

    private static bool PeekScalar(string value, int index, out int codePoint, out int length) {
        codePoint = 0;
        length = 0;
        if (index >= value.Length) {
            return false;
        }
        if (TryPreservedScalar(value, index, out length, out codePoint)) {
            return true;
        }
        var cursor = index;
        codePoint = ReadCodePoint(value, ref cursor);
        length = cursor - index;
        return true;
    }

    private static bool IsExtend(int codePoint, UnicodeCategory category) {
        return category == UnicodeCategory.NonSpacingMark ||
               category == UnicodeCategory.SpacingCombiningMark ||
               category == UnicodeCategory.EnclosingMark ||
               codePoint >= 0xFE00 && codePoint <= 0xFE0F ||
               codePoint >= 0xE0100 && codePoint <= 0xE01EF ||
               codePoint >= 0xE0020 && codePoint <= 0xE007F ||
               codePoint >= 0x1F3FB && codePoint <= 0x1F3FF;
    }

    internal static bool IsZeroWidthScalar(int codePoint) {
        return codePoint == 0x200D ||
               IsExtend(codePoint, UnicodeCategoryFor(codePoint)) ||
               IsDefaultIgnorableFormatScalar(codePoint);
    }

    private static bool IsDefaultIgnorableFormatScalar(int codePoint) {
        return codePoint == 0x00AD ||
               codePoint == 0x061C ||
               codePoint == 0x180E ||
               codePoint >= 0x200B && codePoint <= 0x200F ||
               codePoint >= 0x202A && codePoint <= 0x202E ||
               codePoint >= 0x2060 && codePoint <= 0x2064 ||
               codePoint >= 0x2066 && codePoint <= 0x206F ||
               codePoint == 0xFEFF ||
               codePoint >= 0x1BCA0 && codePoint <= 0x1BCA3 ||
               codePoint >= 0x1D173 && codePoint <= 0x1D17A ||
               codePoint == 0xE0001 ||
               codePoint >= 0xE0020 && codePoint <= 0xE007F;
    }

    private static bool IsRegionalIndicator(int codePoint) {
        return codePoint >= 0x1F1E6 && codePoint <= 0x1F1FF;
    }

    private static bool IsExtendedPictographic(int codePoint) {
        if (codePoint >= 0x1F000 && codePoint <= 0x1FAFF) {
            return true;
        }
        return codePoint == 0x00A9 ||
               codePoint == 0x00AE ||
               codePoint == 0x203C ||
               codePoint == 0x2049 ||
               codePoint == 0x2122 ||
               codePoint == 0x2139 ||
               codePoint >= 0x2194 && codePoint <= 0x2199 ||
               codePoint == 0x21A9 ||
               codePoint == 0x21AA ||
               codePoint == 0x231A ||
               codePoint == 0x231B ||
               codePoint == 0x2328 ||
               codePoint == 0x23CF ||
               codePoint >= 0x23E9 && codePoint <= 0x23F3 ||
               codePoint >= 0x23F8 && codePoint <= 0x23FA ||
               codePoint == 0x24C2 ||
               codePoint == 0x25AA ||
               codePoint == 0x25AB ||
               codePoint == 0x25B6 ||
               codePoint == 0x25C0 ||
               codePoint >= 0x25FB && codePoint <= 0x25FE ||
               codePoint >= 0x2600 && codePoint <= 0x27BF ||
               codePoint == 0x2934 ||
               codePoint == 0x2935 ||
               codePoint >= 0x2B05 && codePoint <= 0x2B07 ||
               codePoint == 0x2B1B ||
               codePoint == 0x2B1C ||
               codePoint == 0x2B50 ||
               codePoint == 0x2B55 ||
               codePoint == 0x3030 ||
               codePoint == 0x303D ||
               codePoint == 0x3297 ||
               codePoint == 0x3299;
    }

    private static bool IsHangulContinuation(int previous, int next) {
        var previousType = HangulType(previous);
        var nextType = HangulType(next);
        return previousType == 1 && (nextType == 1 || nextType == 2 || nextType == 4 || nextType == 5) ||
               (previousType == 2 || previousType == 4) && (nextType == 2 || nextType == 3) ||
               (previousType == 3 || previousType == 5) && nextType == 3;
    }

    private static int HangulType(int codePoint) {
        if (codePoint >= 0x1100 && codePoint <= 0x115F || codePoint >= 0xA960 && codePoint <= 0xA97C) {
            return 1;
        }
        if (codePoint >= 0x1160 && codePoint <= 0x11A7 || codePoint >= 0xD7B0 && codePoint <= 0xD7C6) {
            return 2;
        }
        if (codePoint >= 0x11A8 && codePoint <= 0x11FF || codePoint >= 0xD7CB && codePoint <= 0xD7FB) {
            return 3;
        }
        if (codePoint >= 0xAC00 && codePoint <= 0xD7A3) {
            return (codePoint - 0xAC00) % 28 == 0 ? 4 : 5;
        }
        return 0;
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
