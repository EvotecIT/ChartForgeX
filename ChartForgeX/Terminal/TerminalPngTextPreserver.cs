using System;
using System.Globalization;
using System.Text;
using ChartForgeX.Raster;

namespace ChartForgeX.Terminal;

internal static class TerminalPngTextPreserver {
    private const double ColumnWidthFactor = 0.61;
    private const int MaximumFallbackLabels = 4;
    internal const char EscapeStart = '\uFDD0';
    internal const char EscapeEnd = '\uFDD1';

    public static string Preserve(string value, TrueTypeFont? font) {
        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }
        var output = new StringBuilder(value.Length + 16);
        var changed = false;
        foreach (var element in TerminalTextWidth.Elements(value)) {
            var requiresShaping = RequiresShaping(element);
            for (var index = 0; index < element.Length;) {
                var scalarStart = index;
                if (TerminalTextWidth.TryPreservedScalar(element, index, out var preservedLength, out _)) {
                    output.Append(element, index, preservedLength);
                    index += preservedLength;
                    continue;
                }
                var codePoint = ReadCodePoint(element, ref index);
                if (!requiresShaping &&
                    !TerminalTextWidth.IsZeroWidthScalar(codePoint) &&
                    codePoint != EscapeStart &&
                    codePoint != EscapeEnd &&
                    CanRender(codePoint, font)) {
                    output.Append(element, scalarStart, index - scalarStart);
                    continue;
                }

                AppendPreservedScalar(output, codePoint);
                changed = true;
            }
        }

        return changed ? output.ToString() : value;
    }

    public static double Measure(string value, RgbaCanvas canvas, double fontSize) {
        return MeasureCore(value, canvas, fontSize);
    }

    public static double MeasureEmphasized(string value, RgbaCanvas canvas, double fontSize) {
        var width = MeasureCore(value, canvas, fontSize);
        if (value.Length == 0) return width;
        var emphasisOffset = canvas.MeasureTextEmphasizedWidth("M", fontSize) - canvas.MeasureTextWidth("M", fontSize);
        return width + Math.Max(0, emphasisOffset);
    }

    private static double MeasureCore(string value, RgbaCanvas canvas, double fontSize) {
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
        Draw(canvas, x, y, value, color, fontSize, false);
    }

    public static void DrawEmphasized(RgbaCanvas canvas, double x, double y, string value, ChartColor color, double fontSize) {
        Draw(canvas, x, y, value, color, fontSize, true);
    }

    private static void Draw(RgbaCanvas canvas, double x, double y, string value, ChartColor color, double fontSize, bool emphasized) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        if (value == null) throw new ArgumentNullException(nameof(value));
        var cursor = x;
        foreach (var element in TerminalTextWidth.Elements(value)) {
            if (!ContainsPreservedScalar(element)) {
                if (emphasized) canvas.DrawTextEmphasized(cursor, y, element, color, fontSize);
                else canvas.DrawText(cursor, y, element, color, fontSize);
                cursor += canvas.MeasureTextWidth(element, fontSize);
                continue;
            }

            var width = TerminalTextWidth.Measure(element) * fontSize * ColumnWidthFactor;
            var label = ClusterFallbackLabel(element);
            if (width > 0 && label.Length > 0) {
                if (emphasized) canvas.DrawTextFittedEmphasized(cursor, y, label, color, fontSize, width);
                else canvas.DrawTextFitted(cursor, y, label, color, fontSize, width);
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

    private static bool RequiresShaping(string element) {
        var scalarCount = 0;
        for (var index = 0; index < element.Length;) {
            if (TerminalTextWidth.TryPreservedScalar(element, index, out var preservedLength, out _)) {
                index += preservedLength;
            } else {
                ReadCodePoint(element, ref index);
            }
            scalarCount++;
            if (scalarCount > 1) {
                return true;
            }
        }
        return false;
    }

    private static void AppendPreservedScalar(StringBuilder output, int codePoint) {
        output.Append(EscapeStart);
        output.Append("[U+");
        output.Append(codePoint.ToString(codePoint <= ushort.MaxValue ? "X4" : "X", CultureInfo.InvariantCulture));
        output.Append(']');
        output.Append(EscapeEnd);
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
        var labelCount = 0;
        var truncated = false;
        for (var index = 0; index < value.Length;) {
            if (!TerminalTextWidth.TryPreservedScalar(value, index, out var length, out var codePoint)) {
                var scalarStart = index;
                codePoint = ReadCodePoint(value, ref index);
                plain.Append(value, scalarStart, index - scalarStart);
                if (!TerminalTextWidth.IsZeroWidthScalar(codePoint)) {
                    AppendLabel(labels, codePoint, ref labelCount, ref truncated);
                }
                continue;
            }

            if (!TerminalTextWidth.IsZeroWidthScalar(codePoint)) {
                AppendLabel(labels, codePoint, ref labelCount, ref truncated);
                hasVisibleFallback = true;
            }
            index += length;
        }
        if (truncated) labels.Append(" …");
        return hasVisibleFallback ? labels.ToString() : plain.ToString();
    }

    private static void AppendLabel(StringBuilder labels, int codePoint, ref int labelCount, ref bool truncated) {
        if (labelCount >= MaximumFallbackLabels) {
            truncated = true;
            return;
        }
        if (labels.Length > 0) {
            labels.Append(' ');
        }
        labels.Append(CompactLabel(codePoint));
        labelCount++;
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
