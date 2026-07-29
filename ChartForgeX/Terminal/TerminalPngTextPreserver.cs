using System;
using System.Globalization;
using System.Text;
using ChartForgeX.Raster;

namespace ChartForgeX.Terminal;

internal static class TerminalPngTextPreserver {
    private const double ColumnWidthFactor = 0.61;
    internal const char EscapeStart = '\uFDD0';
    internal const char EscapeEnd = '\uFDD1';

    public static string Preserve(string value, TrueTypeFont? font) {
        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }
        StringBuilder? output = null;
        var copiedUntil = 0;
        for (var index = 0; index < value.Length;) {
            var scalarStart = index;
            var codePoint = ReadCodePoint(value, ref index);
            if (codePoint != EscapeStart && codePoint != EscapeEnd && CanRender(codePoint, font)) {
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
        foreach (var element in TerminalTextWidth.Elements(value)) {
            width += ContainsPreservedScalar(element)
                ? TerminalTextWidth.Measure(element) * fontSize * ColumnWidthFactor
                : canvas.MeasureTextWidth(element, fontSize);
        }
        return width;
    }

    public static void Draw(RgbaCanvas canvas, double x, double y, string value, ChartColor color, double fontSize) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        if (value == null) throw new ArgumentNullException(nameof(value));
        var cursor = x;
        foreach (var element in TerminalTextWidth.Elements(value)) {
            if (!ContainsPreservedScalar(element)) {
                canvas.DrawText(cursor, y, element, color, fontSize);
                cursor += canvas.MeasureTextWidth(element, fontSize);
                continue;
            }

            var width = TerminalTextWidth.Measure(element) * fontSize * ColumnWidthFactor;
            var label = ClusterFallbackLabel(element);
            if (width > 0 && label.Length > 0) {
                canvas.DrawTextFitted(cursor, y, label, color, fontSize, width);
            }
            cursor += width;
        }
    }

    private static bool CanRender(int codePoint, TrueTypeFont? font) {
        if (font != null) {
            return font.HasGlyph(codePoint);
        }
        return codePoint <= char.MaxValue && TinyFont.Supports((char)codePoint);
    }

    private static bool ContainsPreservedScalar(string value) {
        for (var index = 0; index < value.Length;) {
            if (TerminalTextWidth.TryPreservedScalar(value, index, out _, out _)) {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static string ClusterFallbackLabel(string value) {
        var plain = new StringBuilder(value.Length);
        var labels = new StringBuilder();
        var hasVisibleFallback = false;
        for (var index = 0; index < value.Length;) {
            if (!TerminalTextWidth.TryPreservedScalar(value, index, out var length, out var codePoint)) {
                var scalarStart = index;
                codePoint = ReadCodePoint(value, ref index);
                plain.Append(value, scalarStart, index - scalarStart);
                if (!TerminalTextWidth.IsZeroWidthScalar(codePoint)) {
                    AppendLabel(labels, codePoint);
                }
                continue;
            }

            if (!TerminalTextWidth.IsZeroWidthScalar(codePoint)) {
                AppendLabel(labels, codePoint);
                hasVisibleFallback = true;
            }
            index += length;
        }
        return hasVisibleFallback ? labels.ToString() : plain.ToString();
    }

    private static void AppendLabel(StringBuilder labels, int codePoint) {
        if (labels.Length > 0) {
            labels.Append(' ');
        }
        labels.Append(CompactLabel(codePoint));
    }

    private static string CompactLabel(int codePoint) {
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
