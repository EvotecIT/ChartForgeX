using System;
using System.Globalization;
using System.Text;
using ChartForgeX.Raster;

namespace ChartForgeX.Terminal;

internal static class TerminalPngTextPreserver {
    public static string Preserve(string value, TrueTypeFont? font) {
        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }
        StringBuilder? output = null;
        var copiedUntil = 0;
        for (var index = 0; index < value.Length;) {
            var scalarStart = index;
            var codePoint = ReadCodePoint(value, ref index);
            if (CanRender(codePoint, font)) {
                continue;
            }

            output ??= new StringBuilder(value.Length + 16);
            output.Append(value, copiedUntil, scalarStart - copiedUntil);
            output.Append("[U+");
            output.Append(codePoint.ToString(codePoint <= ushort.MaxValue ? "X4" : "X", CultureInfo.InvariantCulture));
            output.Append(']');
            copiedUntil = index;
        }

        if (output == null) {
            return value;
        }
        output.Append(value, copiedUntil, value.Length - copiedUntil);
        return output.ToString();
    }

    private static bool CanRender(int codePoint, TrueTypeFont? font) {
        if (font != null) {
            return font.HasGlyph(codePoint);
        }
        return codePoint <= char.MaxValue && TinyFont.Supports((char)codePoint);
    }

    private static int ReadCodePoint(string value, ref int index) {
        var first = value[index++];
        if (!char.IsHighSurrogate(first) || index >= value.Length || !char.IsLowSurrogate(value[index])) {
            return first;
        }
        return char.ConvertToUtf32(first, value[index++]);
    }
}
