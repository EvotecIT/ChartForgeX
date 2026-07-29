using System;
using System.Globalization;
using System.Text;
using ChartForgeX.Raster;

namespace ChartForgeX.Terminal;

internal static class TerminalPngTextPreserver {
    private const double ColumnWidthFactor = 0.61;
    internal const char EscapeStart = '\uE000';
    internal const char EscapeEnd = '\uE001';

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
            output.Append(EscapeStart);
            output.Append("[U+");
            output.Append(codePoint.ToString(codePoint <= ushort.MaxValue ? "X4" : "X", CultureInfo.InvariantCulture));
            output.Append(']');
            output.Append(EscapeEnd);
            copiedUntil = index;
        }

        if (output == null) {
            return value;
        }
        output.Append(value, copiedUntil, value.Length - copiedUntil);
        return output.ToString();
    }

    public static double Measure(string value, RgbaCanvas canvas, double fontSize) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        var width = 0.0;
        Visit(
            value,
            (text, preserved) => width += preserved
                ? TerminalTextWidth.Measure(text) * fontSize * ColumnWidthFactor
                : canvas.MeasureTextWidth(text, fontSize));
        return width;
    }

    public static void Draw(RgbaCanvas canvas, double x, double y, string value, ChartColor color, double fontSize) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        if (value == null) throw new ArgumentNullException(nameof(value));
        var cursor = x;
        Visit(value, (text, preserved) => {
            if (preserved) {
                var width = TerminalTextWidth.Measure(text) * fontSize * ColumnWidthFactor;
                canvas.DrawTextFitted(cursor, y, CompactLabel(text), color, fontSize, width);
                cursor += width;
            } else {
                canvas.DrawText(cursor, y, text, color, fontSize);
                cursor += canvas.MeasureTextWidth(text, fontSize);
            }
        });
    }

    private static bool CanRender(int codePoint, TrueTypeFont? font) {
        if (font != null) {
            return font.HasGlyph(codePoint);
        }
        return codePoint <= char.MaxValue && TinyFont.Supports((char)codePoint);
    }

    private static void Visit(string value, Action<string, bool> visitor) {
        var textStart = 0;
        for (var index = 0; index < value.Length;) {
            if (!TerminalTextWidth.TryPreservedScalar(value, index, out var length, out _)) {
                index++;
                continue;
            }

            if (index > textStart) {
                visitor(value.Substring(textStart, index - textStart), false);
            }
            visitor(value.Substring(index, length), true);
            index += length;
            textStart = index;
        }

        if (textStart < value.Length) {
            visitor(value.Substring(textStart), false);
        }
    }

    private static string CompactLabel(string preservedScalar) {
        if (!TerminalTextWidth.TryPreservedScalar(preservedScalar, 0, out _, out var codePoint)) {
            return preservedScalar;
        }
        return "U+" + codePoint.ToString("X", CultureInfo.InvariantCulture);
    }

    private static int ReadCodePoint(string value, ref int index) {
        var first = value[index++];
        if (!char.IsHighSurrogate(first) || index >= value.Length || !char.IsLowSurrogate(value[index])) {
            return first;
        }
        return char.ConvertToUtf32(first, value[index++]);
    }
}
